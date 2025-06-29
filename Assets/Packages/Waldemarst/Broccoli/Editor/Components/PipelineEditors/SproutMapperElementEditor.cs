﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Component;
using Broccoli.Manager;
using Broccoli.Factory;
using Broccoli.BroccoEditor;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Sprout mapper element editor.
	/// </summary>
	[CustomEditor(typeof(SproutMapperElement))]
	public class SproutMapperElementEditor : PipelineElementEditor {
		#region Vars
		/// <summary>
		/// The sprout mapper element.
		/// </summary>
		public SproutMapperElement sproutMapperElement;
		/// <summary>
		/// Texture canvas component.
		/// </summary>
		TextureCanvas textureCanvas;
		bool _centerTexture = false;
		/// <summary>
		/// The maps list.
		/// </summary>
		ReorderableList mapsList;
		/// <summary>
		/// The areas list.
		/// </summary>
		ReorderableList areasList;
		/// <summary>
		/// The property sprout maps.
		/// </summary>
		SerializedProperty propSproutMaps;
		/// <summary>
		/// The property sprout areas.
		/// </summary>
		SerializedProperty propSproutAreas;
		/// <summary>
		/// Current variance mode to apply to sprout meshes.
		/// </summary>
		SerializedProperty propColorVarianceMode;
		/// <summary>
		/// The changes are to be applied on the pipeline.
		/// </summary>
		private bool changesForPipeline = false;
		/// <summary>
		/// The changes are for only materials.
		/// </summary>
		private bool changesForMaterials = false;
		/// <summary>
		/// The changes are for the UV indexes.
		/// </summary>
		private bool changesForUVs = false;
		/// <summary>
		/// The changes are for meshes.
		/// </summary>
		private bool changesForMeshes = false;
		/// <summary>
		/// The area canvas.
		/// </summary>
		private SproutAreaCanvasEditor areaCanvas = new SproutAreaCanvasEditor ();
		/// <summary>
		/// The index of the current map.
		/// </summary>
		int currentMapIndex = -1;
		/// <summary>
		/// The current sprout map.
		/// </summary>
		SproutMap currentSproutMap = null;
		#endregion

		#region Messages
		private const string MSG_OVERRIDE_IS_ON = "The tree factory overrides the custom material's shader with Unity's tree creator one. You " +
			"can turn this off on 'Advanced Preferences'.";
		private const string MSG_MATERIAL_OVERRIDE = "\"Material Overrive\" clones the assigned material to override its properties (like textures and normal maps). If your assigned material does not support double side rendering, please select \"TreeCreator\" as your preferred shader. To reflect changes made to the overrided material " +
			" please use the \"Update from Base Material\" button.";
		private const string MSG_USES_BRANCH_COLLECTION = "This Sprout Group uses a Branch Collection Scriptable Object to define its meshes. " +
			"Textures will be taken from the collection as well.";
		private const string MSG_DELETE_SPROUT_AREA_TITLE = "Remove Sprout Map Area";
		private const string MSG_DELETE_SPROUT_AREA_MESSAGE = "Do you want to remove this Mapping Area? (Sprouts will take mapping from the rest of the Mapping Areas available).";
		private const string MSG_DELETE_SPROUT_AREA_OK = "Proceed";
		private const string MSG_DELETE_SPROUT_AREA_CANCEL = "Cancel";
		#endregion

		#region Events
		/// <summary>
		/// Creates the UI Elements to be displayed in this inspector.
		/// </summary>
		/// <returns>UI elements to be displayed.</returns>
		public override VisualElement CreateInspectorGUI () {
			var container = new VisualElement();
 
			container.Add(new IMGUIContainer(OnInspectorGUI));
			if (textureCanvas == null) {
				textureCanvas = new TextureCanvas ();
				textureCanvas.Init (Vector2.zero, 1f);
				textureCanvas.SetupZoom (0.1f, 4f);
				container.Add (textureCanvas);
				textureCanvas.style.position = UnityEngine.UIElements.Position.Absolute;
				textureCanvas.StretchToParentSize ();
				BindTextureCanvasEvents ();
			}
			textureCanvas.Hide ();
			textureCanvas.RegisterArea (1, 0.2f, 0.2f, 0.5f, 0.5f, 0.5f, 0.5f);
		
			return container;
		}
		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		void OnDestroy () {
			if (textureCanvas != null) textureCanvas.parent.Remove (textureCanvas);
		}
		/// <summary>
		/// Actions to perform on the concrete class when the enable event is raised.
		/// </summary>
		protected override void OnEnableSpecific () {
			sproutMapperElement = target as SproutMapperElement;

			propSproutMaps = GetSerializedProperty ("sproutMaps");
			mapsList = new ReorderableList (serializedObject, propSproutMaps, false, true, true, true);
			mapsList.draggable = false;
			mapsList.drawHeaderCallback += DrawSproutMapHeader;
			mapsList.drawElementCallback += DrawSproutMapElement;
			mapsList.onSelectCallback += OnSelectMapItem;
			mapsList.onAddCallback += OnAddSproutMapItem;
			mapsList.onRemoveCallback += OnRemoveSproutMapItem;
		}
		/// <summary>
		/// Raises the disable specific event.
		/// </summary>
		protected override void OnDisableSpecific () {
			currentMapIndex = -1;
			currentSproutMap = null;
		}
		/// <summary>
		/// Raises the inspector GUI event.
		/// </summary>
		protected override void OnInspectorGUISpecific () {
			CheckUndoRequest ();

			UpdateSerialized ();

			// Log box.
			DrawLogBox ();

			EditorGUILayout.Space ();
			
			changesForPipeline = false;
			changesForMaterials = false;
			changesForUVs = false;
			changesForMeshes = false;

			// maps
			if (sproutMapperElement.selectedMapIndex != mapsList.index &&
			    sproutMapperElement.selectedMapIndex < mapsList.count) {
				mapsList.index = sproutMapperElement.selectedMapIndex;
			}
			mapsList.DoLayoutList ();

			if (changesForPipeline) {
				ApplySerialized ();
				UpdatePipeline (GlobalSettings.processingDelayVeryHigh, true);
				sproutMapperElement.Validate ();
				SetUndoControlCounter ();
			} else if (changesForMaterials) {
				ApplySerialized ();
				UpdateComponent ((int)SproutMapperComponent.ComponentCommand.BuildMaterials, 
					GlobalSettings.processingDelayVeryLow);
				sproutMapperElement.Validate ();
				SetUndoControlCounter ();
			} else if (changesForUVs) {
				ApplySerialized ();
				UpdateComponent ((int)SproutMapperComponent.ComponentCommand.SetUVs, 
					GlobalSettings.processingDelayVeryLow);
				sproutMapperElement.Validate ();
				SetUndoControlCounter ();
			} else if (changesForMeshes) {
				ApplySerialized ();
				UpdatePipelineUpstream (PipelineElement.ClassType.SproutMeshGenerator, 
					GlobalSettings.processingDelayVeryHigh);
				sproutMapperElement.Validate ();
				SetUndoControlCounter ();
			}

			// SEED OPTIONS
			DrawSeedOptions ();
			// HELP OPTIONS
			DrawFieldHelpOptions ();
			// KEYNAME OPTIONS
			DrawKeyNameOptions ();
		}
		/// <summary>
		/// Event listener for Undo.
		/// </summary>
		protected override void OnUndo () {
			textureCanvas.ClearTexture ();
			_centerTexture = true;
		}
		#endregion

		#region Sprout Maps
		/// <summary>
		/// Draws the list item header.
		/// </summary>
		/// <param name="rect">Rect.</param>
		private void DrawSproutMapHeader(Rect rect)
		{
			GUI.Label(rect, "Texture Maps");
		}
		/// <summary>
		/// Draws the sprout map element.
		/// </summary>
		/// <param name="rect">Rect.</param>
		/// <param name="index">Index.</param>
		/// <param name="isActive">If set to <c>true</c> is active.</param>
		/// <param name="isFocused">If set to <c>true</c> is focused.</param>
		private void DrawSproutMapElement (Rect rect, int index, bool isActive, bool isFocused) {
			var sproutMapProp = mapsList.serializedProperty.GetArrayElementAtIndex (index);

			int sproutGroupId = sproutMapProp.FindPropertyRelative ("groupId").intValue;
			if (sproutGroupId > 0) {
				rect.y += 2;
				SproutGroups sproutGroups = sproutMapperElement.pipeline.sproutGroups;
				EditorGUI.DrawRect (new Rect (rect.x, rect.y, 
					EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight), 
					sproutGroups.GetSproutGroupColor(sproutGroupId));
				rect.x += 22;
				EditorGUI.LabelField (new Rect (rect.x, rect.y, 
					150, EditorGUIUtility.singleLineHeight), "Assigned to group " + sproutGroupId);
			} else {
				rect.y += 2;
				EditorGUI.DrawRect (new Rect (rect.x, rect.y, 
					EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight), 
					Color.black);
				rect.x += 22;
				EditorGUI.LabelField (new Rect (rect.x, rect.y, 
					150, EditorGUIUtility.singleLineHeight), "Unassigned map");
			}

			if (isActive) {
				SproutGroups sproutGroups = sproutMapperElement.pipeline.sproutGroups;
				SproutGroups.SproutGroup sproutGroup = sproutGroups.GetSproutGroup (sproutGroupId);
				if (index != sproutMapperElement.selectedMapIndex) {
					sproutMapperElement.selectedMapIndex = index;
				}
				EditorGUILayout.Space ();

				// Sprout group.
				EditorGUI.BeginChangeCheck ();
				int sproutGroupIndex = EditorGUILayout.Popup ("Sprout Group",
					sproutMapperElement.pipeline.sproutGroups.GetSproutGroupIndex (sproutGroupId, true),
					sproutMapperElement.pipeline.sproutGroups.GetPopupOptions (true));
				int selectedSproutGroupId = 
					sproutMapperElement.pipeline.sproutGroups.GetSproutGroupId (sproutGroupIndex);
				if (EditorGUI.EndChangeCheck() && sproutGroupId != selectedSproutGroupId) {
					if (sproutMapperElement.GetSproutGroupsAssigned ().Contains (selectedSproutGroupId)) {
						Debug.LogWarning ("The sprout group has already been assigned to a material.");
					} else {
						sproutMapProp.FindPropertyRelative ("groupId").intValue = selectedSproutGroupId;
						changesForPipeline = true;
					}
				}
				EditorGUI.BeginChangeCheck ();
				if (sproutGroup != null && sproutGroup.branchCollection != null) {
					EditorGUILayout.HelpBox (MSG_USES_BRANCH_COLLECTION, MessageType.Info);
					if (ExtensionManager.isHDRP) {
						DrawSproutMapDiffusionProfileField (sproutMapProp);
					}
					EditorGUILayout.Slider (sproutMapProp.FindPropertyRelative ("alphaCutoff"), 0f, 1f);
					EditorGUILayout.PropertyField (sproutMapProp.FindPropertyRelative ("subsurfaceColor"));
					EditorGUILayout.Slider (sproutMapProp.FindPropertyRelative ("subsurfaceValue"), 0f, 1f);
					EditorGUI.BeginDisabledGroup (true);
					BranchDescriptorCollection collection = ((BranchDescriptorCollectionSO)sproutGroup.branchCollection).branchDescriptorCollection;
					EditorGUILayout.ObjectField ("Albedo Texture", collection.atlasAlbedoTexture, typeof(Texture2D), true);
					EditorGUILayout.ObjectField ("Normals Texture", collection.atlasNormalsTexture, typeof(Texture2D), true);
					EditorGUILayout.ObjectField ("Extras Texture", collection.atlasExtrasTexture, typeof(Texture2D), true);
					EditorGUILayout.ObjectField ("Subsurface Atlas", collection.atlasSubsurfaceTexture, typeof(Texture2D), true);
					EditorGUI.EndDisabledGroup ();

					if (EditorGUI.EndChangeCheck ()) {
						changesForMaterials = true;
					}
				} else {
					// Mode.
					SproutMap.Mode sproutMapMode = (SproutMap.Mode)sproutMapProp.FindPropertyRelative ("mode").enumValueIndex;
					EditorGUILayout.PropertyField(sproutMapProp.FindPropertyRelative ("mode"));
					if ((SproutMap.Mode)sproutMapProp.FindPropertyRelative ("mode").enumValueIndex == SproutMap.Mode.MaterialOverride) {
						// Changes for mode.
						if (EditorGUI.EndChangeCheck () ||
							sproutMapMode != (SproutMap.Mode)sproutMapProp.FindPropertyRelative ("mode").enumValueIndex) {
							changesForPipeline = true;
						} else {
							DrawSproutMapElementMaterialOverrideMode (sproutMapProp, index);
						}
					} else if ((SproutMap.Mode)sproutMapProp.FindPropertyRelative ("mode").enumValueIndex == SproutMap.Mode.Material) {
						// Changes for mode.
						if (EditorGUI.EndChangeCheck () ||
							sproutMapMode != (SproutMap.Mode)sproutMapProp.FindPropertyRelative ("mode").enumValueIndex) {
							changesForPipeline = true;
						} else {
							DrawSproutMapElementMaterialMode (sproutMapProp);
						}
					} else if ((SproutMap.Mode)sproutMapProp.FindPropertyRelative ("mode").enumValueIndex == SproutMap.Mode.Texture) {
						// Changes for mode.
						if (EditorGUI.EndChangeCheck () ||
							sproutMapMode != (SproutMap.Mode)sproutMapProp.FindPropertyRelative ("mode").enumValueIndex) {
							changesForPipeline = true;
						} else {
							DrawSproutMapElementTextureMode (sproutMapProp, index);
						}
					}
				}
				EditorGUILayout.Space ();
			}
		}
		/// <summary>
		/// Adds a list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnAddSproutMapItem(ReorderableList list)
		{
			if (sproutMapperElement.CanAddSproutMap ()) {
				SproutMap newSproutMap;
				if (sproutMapperElement.selectedMapIndex >= 0 && 
					sproutMapperElement.selectedMapIndex < sproutMapperElement.sproutMaps.Count)
				{
					newSproutMap = sproutMapperElement.sproutMaps [sproutMapperElement.selectedMapIndex].Clone ();
					newSproutMap.groupId = -1;
				} else {
					newSproutMap = new SproutMap ();
				}
				Undo.RecordObject (sproutMapperElement, "Sprout Map added");
				sproutMapperElement.AddSproutMap (newSproutMap);
				changesForMeshes = true;
			}
		}
		/// <summary>
		/// Event called when a map is selected from the list.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnSelectMapItem (ReorderableList list)
		{
			Undo.RecordObject (sproutMapperElement, "Sprout Map selected");
			sproutMapperElement.selectedMapIndex = list.index;
		}
		/// <summary>
		/// Removes a list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnRemoveSproutMapItem(ReorderableList list)
		{
			int undoGroup = Undo.GetCurrentGroup ();
			Undo.SetCurrentGroupName ("Sprout Map removed");
			Undo.RecordObject (sproutMapperElement, "Sprout Map removed");
			sproutMapperElement.sproutMaps.RemoveAt (list.index);
			Undo.RecordObject (sproutMapperElement, "Sprout Map removed");
			sproutMapperElement.selectedMapIndex = -1;
			Undo.CollapseUndoOperations (undoGroup);
			changesForMeshes = true;
		}
		/// <summary>
		/// Draws the sprout map element texture mode.
		/// </summary>
		/// <param name="sproutMapProp">Sprout map property.</param>
		private void DrawSproutMapElementTextureMode (SerializedProperty sproutMapProp, int index) {
			EditorGUI.BeginChangeCheck ();
			// Color
			EditorGUILayout.PropertyField (sproutMapProp.FindPropertyRelative ("color"));
			// Color Variance.
			EditorGUI.BeginChangeCheck ();
			propColorVarianceMode = sproutMapProp.FindPropertyRelative ("colorVarianceMode");
			EditorGUILayout.PropertyField (propColorVarianceMode, new GUIContent ("Color Variance"));
			if (propColorVarianceMode.enumValueIndex == (int)SproutMap.ColorVarianceMode.Shades) {
				FloatRangePropertyField (sproutMapProp.FindPropertyRelative ("minColorShade"), 
					sproutMapProp.FindPropertyRelative ("maxColorShade"), 0.65f, 1f, "Shade Range");
			}
			if (EditorGUI.EndChangeCheck ()) {
				changesForPipeline = true;
			}
			// Alpha cutoff
			EditorGUILayout.Slider (sproutMapProp.FindPropertyRelative ("alphaCutoff"), 0f, 1f);
			if (MaterialManager.leavesShaderType == MaterialManager.LeavesShaderType.SpeedTree8OrSimilar) {
				EditorGUILayout.PropertyField (sproutMapProp.FindPropertyRelative ("subsurfaceColor"));
				EditorGUILayout.Slider (sproutMapProp.FindPropertyRelative ("subsurfaceValue"), 0f, 1f);
				EditorGUILayout.Slider (sproutMapProp.FindPropertyRelative ("glossiness"), 0f, 1f);
				EditorGUILayout.Slider (sproutMapProp.FindPropertyRelative ("metallic"), 0f, 1f);
			}
			if (ExtensionManager.isHDRP) {
				DrawSproutMapDiffusionProfileField (sproutMapProp);
			}

			if (EditorGUI.EndChangeCheck ()) {
				changesForMaterials = true;
			}

			SproutMap sproutMap = sproutMapperElement.sproutMaps [index];
			EditorGUILayout.Space ();

			//Draw Areas
			DrawAreas (index, sproutMap, sproutMapProp);
		}
		/// <summary>
		/// Draws the diffussion profile property field of a sprout map element.
		/// </summary>
		/// <param name="sproutMapProp">Sprout map property.</param>
		private void DrawSproutMapDiffusionProfileField (SerializedProperty sproutMapProp) {
			SerializedProperty propDiffusionProfileSettings = sproutMapProp.FindPropertyRelative ("diffusionProfileSettings");
			ScriptableObject former = (ScriptableObject)propDiffusionProfileSettings.objectReferenceValue;
			former = 
				(ScriptableObject)EditorGUILayout.ObjectField (
					"Diffusion Profile", 
					former, 
					System.Type.GetType ("UnityEngine.Rendering.HighDefinition.DiffusionProfileSettings, Unity.RenderPipelines.HighDefinition.Runtime"), 
					false);
			if (former != (ScriptableObject)propDiffusionProfileSettings.objectReferenceValue) {
				propDiffusionProfileSettings.objectReferenceValue = former;
				changesForMaterials = true;
			}
		}
		/// <summary>
		/// Draws the sprout map element material mode.
		/// </summary>
		/// <param name="sproutMapProp">Sprout map property.</param>
		private void DrawSproutMapElementMaterialMode (SerializedProperty sproutMapProp) {
			// Changes for custom material.
			EditorGUI.BeginChangeCheck ();
			EditorGUILayout.PropertyField (sproutMapProp.FindPropertyRelative ("customMaterial"));
			if (TreeFactory.GetActiveInstance ().treeFactoryPreferences.overrideMaterialShaderEnabled) {
				EditorGUILayout.HelpBox (MSG_OVERRIDE_IS_ON, MessageType.Info);
			}
			if (EditorGUI.EndChangeCheck ()) {
				changesForMaterials = true;
			}
		}
		/// <summary>
		/// Draws the sprout map element material override mode.
		/// </summary>
		/// <param name="sproutMapProp">Sprout map property.</param>
		private void DrawSproutMapElementMaterialOverrideMode (SerializedProperty sproutMapProp, int index) {
			// Changes for custom material.
			EditorGUI.BeginChangeCheck ();
			EditorGUILayout.PropertyField (sproutMapProp.FindPropertyRelative ("customMaterial"));
			if (TreeFactory.GetActiveInstance ().treeFactoryPreferences.overrideMaterialShaderEnabled) {
				EditorGUILayout.HelpBox (MSG_OVERRIDE_IS_ON, MessageType.Info);
			}
			EditorGUILayout.HelpBox (MSG_MATERIAL_OVERRIDE, MessageType.Info);
			if (GUILayout.Button ("Update from Base Material")) {
				changesForMaterials = true;
			}
			EditorGUILayout.Space ();
			// Color
			EditorGUILayout.PropertyField (sproutMapProp.FindPropertyRelative ("color"));
			// Alpha cutoff
			EditorGUILayout.Slider (sproutMapProp.FindPropertyRelative ("alphaCutoff"), 0f, 1f);

			if (EditorGUI.EndChangeCheck ()) {
				changesForMaterials = true;
			}

			SproutMap sproutMap = sproutMapperElement.sproutMaps [index];
			EditorGUILayout.Space ();

			//Draw Areas
			DrawAreas (index, sproutMap, sproutMapProp);
		}
		#endregion

		#region Sprout Areas
		/// <summary>
		/// Draws the areas for a sprout map.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="sproutMap">Sprout map.</param>
		/// <param name="sproutMapProp">Sprout map property.</param>
		void DrawAreas (int index, SproutMap sproutMap, SerializedProperty sproutMapProp) {
			if (index != currentMapIndex) {
				currentMapIndex = index;
				currentSproutMap = sproutMap;
				propSproutAreas = sproutMapProp.FindPropertyRelative ("sproutAreas");
				areasList = new ReorderableList (serializedObject, propSproutAreas, false, true, true, true);
				areasList.draggable = false;
				areasList.drawHeaderCallback += DrawSproutAreaHeader;
				areasList.drawElementCallback += DrawSproutAreaElement;
				areasList.onSelectCallback += OnSelectSproutAreaItem;
				areasList.onAddCallback += OnAddSproutAreaItem;
				areasList.onRemoveCallback += OnRemoveSproutAreaItem;
			}
			if (areasList != null) {
				if (currentSproutMap.selectedAreaIndex != areasList.index &&
					currentSproutMap.selectedAreaIndex < mapsList.count) {
					areasList.index = currentSproutMap.selectedAreaIndex;
				}
				areasList.DoLayoutList ();
			}
		}
		/// <summary>
		/// Draws the sprout area header.
		/// </summary>
		/// <param name="rect">Rect.</param>
		private void DrawSproutAreaHeader(Rect rect) {
			GUI.Label(rect, "Textures");
		}
		/// <summary>
		/// Draws the sprout area element.
		/// </summary>
		/// <param name="rect">Rect.</param>
		/// <param name="index">Index.</param>
		/// <param name="isActive">If set to <c>true</c> is active.</param>
		/// <param name="isFocused">If set to <c>true</c> is focused.</param>
		private void DrawSproutAreaElement (Rect rect, int index, bool isActive, bool isFocused) {
			var sproutAreaProp = areasList.serializedProperty.GetArrayElementAtIndex (index);
			EditorGUI.LabelField (new Rect (rect.x, rect.y, 
					150, EditorGUIUtility.singleLineHeight), "Texture Area " + index);
			if (isActive) {
				if (index != currentSproutMap.selectedAreaIndex) {
					currentSproutMap.selectedAreaIndex = index;
				}
				EditorGUILayout.Space ();

				// Enabled
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (sproutAreaProp.FindPropertyRelative ("enabled"));
				if (EditorGUI.EndChangeCheck ()) {
					changesForMeshes = true;
				}

				Texture2D texture = null;
				SproutMap.SproutMapArea sproutArea = currentSproutMap.sproutAreas [index];

				if (currentSproutMap.textureMode == SproutMap.TextureMode.PerArea) {
					// Texture.
					EditorGUI.BeginChangeCheck ();
					SerializedProperty albedoTextureProp = sproutAreaProp.FindPropertyRelative ("texture");
					SerializedProperty normalTextureProp = sproutAreaProp.FindPropertyRelative ("normalMap");
					SerializedProperty extraTextureProp = sproutAreaProp.FindPropertyRelative ("extraMap");
					SerializedProperty subsurfaceTextureProp = sproutAreaProp.FindPropertyRelative ("subsurfaceMap");

					EditorGUILayout.PropertyField (albedoTextureProp, new GUIContent ("Texture"));
					if (EditorGUI.EndChangeCheck ()) {
						serializedObject.ApplyModifiedProperties();
						// Get the newly assigned texture
						Texture2D newAlbedo = albedoTextureProp.objectReferenceValue as Texture2D;
						// Run the logic to find related textures and prompt the user
						CheckAndPromptForRelatedTextures (newAlbedo, normalTextureProp, extraTextureProp, subsurfaceTextureProp);
						// Update again to potentially reflect changes made by the prompt logic
						changesForMaterials = true;
					}
					texture = sproutArea.texture;

					// Normal map.
					EditorGUI.BeginChangeCheck ();
					EditorGUILayout.PropertyField (normalTextureProp, new GUIContent ("Normap Map"));
					if (EditorGUI.EndChangeCheck ()) {
						changesForMaterials = true;
					}

					if (MaterialManager.leavesShaderType == MaterialManager.LeavesShaderType.SpeedTree8OrSimilar) {
						// Extra map.
						EditorGUI.BeginChangeCheck ();
						EditorGUILayout.PropertyField (extraTextureProp, new GUIContent ("Extra Map"));
						if (EditorGUI.EndChangeCheck ()) {
							changesForMaterials = true;
						}

						// Subsurface map.
						EditorGUI.BeginChangeCheck ();
						EditorGUILayout.PropertyField (subsurfaceTextureProp, new GUIContent ("Subsurface Map"));
						if (EditorGUI.EndChangeCheck ()) {
							changesForMaterials = true;
						}
					}
				} else {
					// Texture Mode is Per Group
					texture = currentSproutMap.texture;
				}

				if (texture != null) {
					// x, y, width, height, pivot x, pivot y
					EditorGUI.BeginChangeCheck ();
					EditorGUILayout.Slider (sproutAreaProp.FindPropertyRelative ("x"), 0f, 1f, "Area X");
					EditorGUILayout.Slider (sproutAreaProp.FindPropertyRelative ("y"), 0f, 1f, "Area Y");
					EditorGUILayout.Slider (sproutAreaProp.FindPropertyRelative ("width"), 0f, 1f, "Area Width");
					EditorGUILayout.Slider (sproutAreaProp.FindPropertyRelative ("height"), 0f, 1f, "Area Height");
					EditorGUILayout.Slider (sproutAreaProp.FindPropertyRelative ("pivotX"), 0f, 1f, "Pivot X");
					EditorGUILayout.Slider (sproutAreaProp.FindPropertyRelative ("pivotY"), 0f, 1f, "Pivot Y");
					if (EditorGUI.EndChangeCheck ()) {
						ApplySerialized ();
						changesForMeshes = true;
						currentSproutMap.sproutAreas [index].Validate ();
						textureCanvas.SetAreaRectAndPivot (1, currentSproutMap.sproutAreas [index].rect, currentSproutMap.sproutAreas [index].pivot);
					}

					EditorGUILayout.Space ();
					float canvasSize = (1f / EditorGUIUtility.pixelsPerPoint) * Screen.width - 40;
					GUILayout.Box ("", GUIStyle.none, 
						GUILayout.ExpandWidth (true), 
						GUILayout.Height (canvasSize));
					Rect canvasRect = GUILayoutUtility.GetLastRect ();
					if (_centerTexture) {
						textureCanvas.style.marginTop = canvasRect.y;
						textureCanvas.style.height = canvasRect.height;
						textureCanvas.style.width = canvasRect.width - 4;
					}

					if (textureCanvas.SetTexture (texture)) {
						textureCanvas.Show ();
						textureCanvas.SetAreaRectAndPivot (1, currentSproutMap.sproutAreas [index].rect, currentSproutMap.sproutAreas [index].pivot);
						_centerTexture = true;
					}
					if (_centerTexture && Event.current.type == EventType.Repaint) {
						textureCanvas.guiRect = canvasRect;
						textureCanvas.CenterTexture ();
						_centerTexture = false;
					}
				} else {
					textureCanvas.Hide ();
				}
			}
		}
		/// <summary>
		/// Raises the select sprout area item event.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnSelectSproutAreaItem (ReorderableList list)
		{
			currentSproutMap.selectedAreaIndex = list.index;
			textureCanvas.SetAreaRectAndPivot (1, currentSproutMap.sproutAreas [list.index].rect, currentSproutMap.sproutAreas [list.index].pivot);
		}
		/// <summary>
		/// Adds a list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnAddSproutAreaItem(ReorderableList list)
		{
			Undo.RecordObject (sproutMapperElement, "Sprout Map added");
			SproutMap.SproutMapArea newSproutMapArea;
			int totalSproutMapAreas = currentSproutMap.sproutAreas.Count;
			if (totalSproutMapAreas > 0) {
				newSproutMapArea = currentSproutMap.sproutAreas[totalSproutMapAreas - 1].Clone ();
			} else {
				newSproutMapArea = new SproutMap.SproutMapArea ();
			}
			currentSproutMap.sproutAreas.Add (newSproutMapArea);
			changesForPipeline = true;
		}
		/// <summary>
		/// Removes a list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnRemoveSproutAreaItem(ReorderableList list)
		{
			if (EditorUtility.DisplayDialog (
				MSG_DELETE_SPROUT_AREA_TITLE, 
				MSG_DELETE_SPROUT_AREA_MESSAGE, 
				MSG_DELETE_SPROUT_AREA_OK, 
				MSG_DELETE_SPROUT_AREA_CANCEL))
			{
				int undoGroup = Undo.GetCurrentGroup ();
				Undo.SetCurrentGroupName ("Sprout Map Area removed");
				Undo.RecordObject (sproutMapperElement, "Sprout Map Area removed");
				currentSproutMap.sproutAreas.RemoveAt (list.index);
				currentSproutMap.selectedAreaIndex = -1;
				Undo.CollapseUndoOperations (undoGroup);
				changesForPipeline = true;
			}
		}
		private const string AlbedoSuffix1 = "_Albedo";
		private const string AlbedoSuffix2 = "_Color";
		private const string NormalSuffix1 = "_Normal";
		private const string NormalSuffix2 = "_Normals";
		private const string ExtraSuffix1 = "_Extra";
		private const string ExtraSuffix2 = "_Extras";
		private const string SubsurfaceSuffix = "_Subsurface";

		/// <summary>
		/// Checks for related textures based on the assigned Albedo texture's path and name.
		/// If related textures are found, prompts the user to auto-assign them.
		/// </summary>
		/// <param name="albedo">The newly assigned Albedo texture.</param>
		private bool CheckAndPromptForRelatedTextures(Texture2D albedo, SerializedProperty normalMapProp, SerializedProperty extraMapProp, SerializedProperty subsurfaceMapProp)
		{
			if (albedo == null) return false; // Nothing assigned, do nothing

			// Get the asset path of the assigned Albedo texture
			string albedoPath = AssetDatabase.GetAssetPath(albedo);
			if (string.IsNullOrEmpty(albedoPath)) return false; // Not a project asset

			// Extract directory, filename without extension, and extension
			string directory = Path.GetDirectoryName(albedoPath);
			string filename = Path.GetFileNameWithoutExtension(albedoPath);
			string extension = Path.GetExtension(albedoPath); // e.g., ".png"

			// Check if the filename ends with the expected Albedo suffix
			if (!filename.EndsWith(AlbedoSuffix1, System.StringComparison.OrdinalIgnoreCase) && !filename.EndsWith(AlbedoSuffix2, System.StringComparison.OrdinalIgnoreCase))
			{
				// Optional: Warn user if name doesn't match convention
				// Debug.LogWarning($"Assigned texture '{filename}' does not end with '{AlbedoSuffix}'. Cannot automatically find related maps.");
				return false;
			}

			// Get the base name by removing the suffix
			string albedoSuffix = AlbedoSuffix1;
			if (filename.EndsWith(AlbedoSuffix2, System.StringComparison.OrdinalIgnoreCase)) {
				albedoSuffix = AlbedoSuffix2;
			}

			string baseName = filename.Substring(0, filename.Length - albedoSuffix.Length);

			// --- Find Potential Related Textures ---
			Dictionary<string, Texture2D> foundTextures = new Dictionary<string, Texture2D>();

			// Look for Normal Map (_Normals)
			Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(directory, baseName + NormalSuffix1 + extension));
			if (normal == null) // If _Normal not found, try _Normals
			{
				normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(directory, baseName + NormalSuffix2 + extension));
			}
			if (normal != null) foundTextures.Add("Normal", normal);

			// Look for Extra Map (_Extra or _Extras)
			Texture2D extra = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(directory, baseName + ExtraSuffix1 + extension));
			if (extra == null) // If _Extra not found, try _Extras
			{
				extra = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(directory, baseName + ExtraSuffix2 + extension));
			}
			if (extra != null) foundTextures.Add("Extra", extra);

			// Look for Subsurface Map (_Subsurface)
			Texture2D subsurface = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(directory, baseName + SubsurfaceSuffix + extension));
			if (subsurface != null) foundTextures.Add("Subsurface", subsurface);

			// --- Prompt User if Any Maps Found ---
			if (foundTextures.Any()) // Using System.Linq
			{
				// Build the confirmation message
				string foundMapsList = string.Join(", ", foundTextures.Keys); // e.g., "Normal, Extra, Subsurface"
				string dialogMessage = $"Found related texture(s): {foundMapsList}.\n\nAssign them automatically to the corresponding slots?";
				string dialogTitle = "Assign Related Textures?";

				// Show dialog and proceed if user clicks "Yes"
				if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "No"))
				{
					// Assign found textures to the corresponding serialized properties
					if (foundTextures.TryGetValue("Normal", out Texture2D foundNormal))
					{
						normalMapProp.objectReferenceValue = foundNormal;
					}
					if (foundTextures.TryGetValue("Extra", out Texture2D foundExtra))
					{
						extraMapProp.objectReferenceValue = foundExtra;
					}
					if (foundTextures.TryGetValue("Subsurface", out Texture2D foundSubsurface))
					{
						subsurfaceMapProp.objectReferenceValue = foundSubsurface;
					}
					// ApplyModifiedProperties will be called at the end of OnInspectorGUI
					//Debug.Log($"Auto-assigned related textures based on '{albedo.name}'.");
					return true;
				}
			}
			return false;
		}
		#endregion

		#region Structure Graph Events
		private void BindTextureCanvasEvents () {
			textureCanvas.onZoomDone -= OnCanvasZoomDone;
			textureCanvas.onZoomDone += OnCanvasZoomDone;
			textureCanvas.onPanDone -= OnCanvasPanDone;
			textureCanvas.onPanDone += OnCanvasPanDone;
			textureCanvas.onBeforeEditArea -= OnBeforeEditArea;
			textureCanvas.onBeforeEditArea += OnBeforeEditArea;
			textureCanvas.onEditArea -= OnEditArea;
			textureCanvas.onEditArea += OnEditArea;
		}
		void OnCanvasZoomDone (float currentZoom, float previousZoom) {}
		void OnCanvasPanDone (Vector2 currentOffset, Vector2 previousOffset) {}
		void OnBeforeEditArea (TextureCanvas.Area area) {
			if (currentSproutMap != null && currentSproutMap.selectedAreaIndex >= 0) {
				Undo.RecordObject (sproutMapperElement, "Edit Texture area.");
			}
		}
		void OnEditArea (TextureCanvas.Area area) {
			// Update area rect and anchor.
			if (currentSproutMap != null && currentSproutMap.selectedAreaIndex >= 0) {
				SproutMap.SproutMapArea sproutMapArea = currentSproutMap.sproutAreas [currentSproutMap.selectedAreaIndex];
				sproutMapArea.x = area.rect.x;
				sproutMapArea.y = area.rect.y;
				sproutMapArea.width = area.rect.width;
				sproutMapArea.height = area.rect.height;
				sproutMapArea.pivotX = area.pivot.x;
				sproutMapArea.pivotY = area.pivot.y;
				ApplySerialized ();
				sproutMapperElement.Validate ();
				UpdatePipelineUpstream (PipelineElement.ClassType.SproutMeshGenerator, 
					GlobalSettings.processingDelayVeryHigh);
				SetUndoControlCounter ();
			}
		}
		#endregion
	}
}