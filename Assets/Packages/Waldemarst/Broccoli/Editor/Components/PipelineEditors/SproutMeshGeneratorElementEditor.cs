﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.Factory;
using Broccoli.Utils;
using Broccoli.BroccoEditor;

namespace Broccoli.TreeNodeEditor
{
	using MeshPreview = Broccoli.BroccoEditor.MeshPreview;
	/// <summary>
	/// Sprout mesh generator node editor.
	/// </summary>
	[CustomEditor(typeof(SproutMeshGeneratorElement))]
	public class SproutMeshGeneratorElementEditor : PipelineElementEditor {
		#region Vars
		/// <summary>
		/// The sprout mesh generator node.
		/// </summary>
		public SproutMeshGeneratorElement sproutMeshGeneratorElement;
		/// <summary>
		/// Options to show on the toolbar.
		/// </summary>
		static string[] toolbarOptions = new string[] {"Sprout Groups", "Mesh Data"};
		static int OPTION_GROUPS = 0;
		static int OPTION_DATA = 1;
		/// <summary>
		/// The meshes list.
		/// </summary>
		ReorderableList meshesList;
		/// <summary>
		/// Normal mode for the sprout mesh.
		/// </summary>
		SerializedProperty propNormalMode;
		/// <summary>
		/// Normal mode lerp  from original sprout mesh normals.
		/// </summary>
		SerializedProperty propNormalModeStrength;
		/// <summary>
		/// Mesh preview utility.
		/// </summary>
		MeshPreview meshPreview = null;
		//bool meshPreviewEnabled = true;
		Dictionary<int, Mesh> previewMeshes = new Dictionary<int, Mesh> ();
		Dictionary<int, Material> previewMaterials = new Dictionary<int, Material> ();
		private static Rect scaleCurveRange = new Rect (0f, 0f, 1f, 1f);
		private static Rect gravityBendingCurveRange = new Rect (0f, 0f, 1f, 1f);
		private static Rect noiseResolutionCurveRange = new Rect (0f, 0f, 1f, 1f);
		private static Rect noiseStrengthCurveRange = new Rect (0f, 0f, 1f, 1f);
		SerializedProperty propSproutMeshes;
		GUIStyle pivotLabelStyle = new GUIStyle ();
		GUIStyle gravityVectorLabelStyle = new GUIStyle ();
        int selectedToolbarOption = 0;
        bool showSectionScale = false;
		bool showSectionNoise = false;
        bool showSectionHorizontalAlign = false;
		bool showSectionWindPattern = false;
        bool showSectionSize = false;
        bool showSectionMesh = false;
        bool showSectionResolution = false;
        bool showSectionGravityBending = false;
		#endregion

		#region GUI
		private static GUIContent scaleSectionLabel = new GUIContent ("Scale", "Control the scale applied to sprout meshes according to their position in the tree hierarchy.");
		private static GUIContent scaleModeLabel = new GUIContent ("Distribution", "Mode to calculate the scale according to the position of the sprout.");
		private static GUIContent scaleAtBaseLabel = new GUIContent ("At Base", "Scale factor to apply to sprouts at the base positions.");
		private static GUIContent scaleAtTopLabel = new GUIContent ("At Top", "Scale factor to apply to sprouts at the top positions.");
		private static GUIContent scaleVarianceLabel = new GUIContent ("Variance", "Adds variance to the scale factor within the specified At Base and At Top range.");
		private static GUIContent scaleCurveLabel = new GUIContent ("Curve", "Curve to adjust the scale factor for the sprout position.");
		private static GUIContent windPatternLabel = new GUIContent ("Wind Pattern Type", "Type of wind pattern to apply to these sprouts.");
		private static GUIContent noisePatternLabel = new GUIContent ("Pattern", "Pattern to distribute displacement noise on the sprout plane.");
		private static GUIContent noiseDistributionLabel = new GUIContent ("Distribution", "Mode to calculate the noise according to the position of the sprout.");
		private static GUIContent noiseResolutionAtBaseLabel = new GUIContent ("Resolution At Base", "Noise resolution factor to apply to sprouts at the base positions.");
		private static GUIContent noiseResolutionAtTopLabel = new GUIContent ("Resolution At Top", "Noise resolution factor to apply to sprouts at the top positions.");
		private static GUIContent noiseResolutionVarianceLabel = new GUIContent ("Resolution Variance", "Adds variance to the noise resolution factor within the specified At Base and At Top range.");
		private static GUIContent noiseResolutionCurveLabel = new GUIContent ("Resolution Curve", "Curve to adjust the noise resolution factor for the sprout position.");
		private static GUIContent noiseStrengthAtBaseLabel = new GUIContent ("Strength At Base", "Noise strength factor to apply to sprouts at the base positions.");
		private static GUIContent noiseStrengthAtTopLabel = new GUIContent ("Strength At Top", "Noise strength factor to apply to sprouts at the top positions.");
		private static GUIContent noiseStrengthVarianceLabel = new GUIContent ("Strength Variance", "Adds variance to the noise strength factor within the specified At Base and At Top range.");
		private static GUIContent noiseStrengthCurveLabel = new GUIContent ("Strength Curve", "Curve to adjust the noise strength factor for the sprout position.");
		#endregion

		#region Messages
		private static string MSG_SPROUT_GROUP = "Sprout group this mesh group belongs to.";
		private static string MSG_MODE = "Mode used to generate the sprouts.";
		private static string MSG_SHAPE_MODE = "Mesh shape for the sprouts.";
		private static string MSG_DEPTH = "Depth of the sprout mesh on its center.";
		private static string MSG_WIDTH = "Width for the mesh plane.";
		private static string MSG_HEIGHT = "Height for the mesh plane.";
		private static string MSG_PIVOT_X = "The x coordinate for the sprout point of origin on the mesh.";
		private static string MSG_PIVOT_Y = "The y coordinate for the sprout point of origin on the mesh.";
		private static string MSG_OVERRIDE_HEIGHT_WITH_TEXTURE = "Check this to let the height be set " +
			"keeping the aspect radio of the texture assigned to the sprout group.";
		private static string MSG_INCLUDE_SCALE_FROM_ATLAS = "Check this to apply an automatic scaling from the area the mapping " +
			"for this sprout takes on an texture atlas. This helps you get uniformed scaled sprout meshes when they come from a single texture atlas.";
		private static string MSG_SCALE_MODE = "Mode to calculate the scale factor according to the sprout position:\n" +
			"1. Hierarchy: the global hierarchy of the sprout is taken to calculate the scale factor.\n" +
			"2. Branch: the branch position of the sprout is taken to calculate the scale factor.\n" +
			"3. Range: the branch position of the sprout is normalized to the range used to create it to calculate the scale factor.";
		private static string MSG_SCALE_VARIANCE = "Adds randomness to the scale factor within the given parameters (At Top and At Base).";
		private static string MSG_SCALE_AT_BASE = "Scale of the sprouts at the base of the branch.";
		private static string MSG_SCALE_AT_TOP = "Scale of the sprouts at the top of the branch.";
		private static string MSG_SCALE_CURVE = "Curve used to transition between scale at base and at top values.";
		private static string MSG_HORIZONTAL_ALIGN_AT_BASE = "Horizontal plane alignment of the plane sprouts at the base of the branch.";
		private static string MSG_HORIZONTAL_ALIGN_AT_TOP = "Horizontal plane alignment of the plane sprouts at the top of the branch.";
		private static string MSG_HORIZONTAL_ALIGN_CURVE = "Curve used to transition between horizontal align at base and at top values.";
		private static string MSG_WIND_PATTERN = "Sprouts can have different parameters to apply wind to them. These parameter are available at the wind node.";
		private static string MSG_MESH = "Custom mesh to use as model to create the sprout meshes.";
		private static string MSG_MULTI_LOD = "Use custom meshes for additional LOD levels.";
		private static string MSG_MESH_LOD1 = "Custom mesh to use as model to create the sprout meshes for LOD 1.";
		private static string MSG_MESH_LOD2 = "Custom mesh to use as model to create the sprout meshes for LOD 2.";
		private static string MSG_MESH_SCALE = "Scale value for the custom mesh.";
		private static string MSG_MESH_ROTATION = "Rotation used on the custom mesh center.";
		private static string MSG_MESH_OFFSET = "Offset used on the custom mesh center.";
		private static string MSG_RESOLUTION_WIDTH = "Number of divisions for the plane on the width side of it.";
		private static string MSG_RESOLUTION_HEIGHT = "Number of divisions for the plane on the height side of it.";
		private static string MSG_GRAVITY_BENDING_AT_BASE = "How much the sprouts bend against gravity at the base of the branches.";
		private static string MSG_GRAVITY_BENDING_AT_TOP = "How much the sprouts bend agains gravity at the top of the branches.";
		private static string MSG_GRAVITY_BENDING_CURVE = "Curve to distribute gravity bending along the hierarchy of the tree.";
		private static string MSG_NORMAL_MODE = " Mode to calculate normals on the sprout mesh:\n" +
			"1. PerSprout: default normals and relative to the each sprout position.\n" +
			"2. TreeOrigin: normals relative to the origin of the whole tree.\n" +
			"3. SproutsCenter: normals calculated from the center of the whole sprout mesh bounds.\n" +
			"4. SproutsBase: normals calculated from the bottom of the whole sprout mesh bounds.";
		private static string MSG_NORMAL_MODE_STRENGTH = "How much the normals transition from default to the selected mode.";
		private static string MSG_SIDE_GRAVITY_BENDING_AT_BASE = "";
		private static string MSG_SIDE_GRAVITY_BENDING_AT_TOP = "";
		private static string MSG_SIDE_GRAVITY_BENDING_SHAPE = "";
		private static string MSG_BRANCH_COLLECTION_EMPTY = "This meshing mode requires a Branch Collection Scriptable Object" +
			" containing the definitions of the sprout meshes to populate the tree with.";
		private static string MSG_BRANCH_COLLECTION = "The meshes to be assigned to this group will be taken from the ones defined at this Branch Collection Scriptable Object.";
		private static string MSG_NOISE_PATTERN = "Pattern to distribute displacement noise on the sprout plane.";
		private static string MSG_NOISE_DISTRIBUTION = "Mode to calculate the noise according to the position of the sprout.";
		private static string MSG_NOISE_RESOLUTION_AT_BASE = "Noise resolution factor to apply to sprouts at the base positions.";
		private static string MSG_NOISE_RESOLUTION_AT_TOP = "Noise resolution factor to apply to sprouts at the top positions.";
		private static string MSG_NOISE_RESOLUTION_VARIANCE = "Adds variance to the noise resolution factor within the specified At Base and At Top range.";
		private static string MSG_NOISE_RESOLUTION_CURVE = "Curve to adjust the noise resolution factor for the sprout position.";
		private static string MSG_NOISE_STRENGTH_AT_BASE = "Noise strength factor to apply to sprouts at the base positions.";
		private static string MSG_NOISE_STRENGTH_AT_TOP = "Noise strength factor to apply to sprouts at the top positions.";
		private static string MSG_NOISE_STRENGTH_VARIANCE = "Adds variance to the noise strength factor within the specified At Base and At Top range.";
		private static string MSG_NOISE_STRENGTH_CURVE = "Curve to adjust the noise strength factor for the sprout position.";
		#endregion

		#region Events
		/// <summary>
		/// Actions to perform on the concrete class when the enable event is raised.
		/// </summary>
		protected override void OnEnableSpecific () {
			sproutMeshGeneratorElement = target as SproutMeshGeneratorElement;

			// Init Mesh List
			propSproutMeshes = GetSerializedProperty ("sproutMeshes");
			meshesList = new ReorderableList (serializedObject, propSproutMeshes, false, true, true, true);

			meshesList.draggable = false;
			meshesList.drawHeaderCallback += DrawMeshItemHeader;
			meshesList.drawElementCallback += DrawMeshItemElement;
			meshesList.onSelectCallback += OnSelectMeshItem;
			meshesList.onAddCallback += OnAddMeshItem;
			meshesList.onRemoveCallback += OnRemoveMeshItem;

			propNormalMode = GetSerializedProperty ("normalMode");
			propNormalModeStrength = GetSerializedProperty ("normalModeStrength");
		}
		/// <summary>
		/// Raises the inspector GUI event.
		/// </summary>
		protected override void OnInspectorGUISpecific () {
			CheckUndoRequest ();

			UpdateSerialized ();

			selectedToolbarOption = GUILayout.Toolbar (selectedToolbarOption, toolbarOptions);
			EditorGUILayout.Space ();

			EditorGUI.BeginChangeCheck ();

			if (selectedToolbarOption == OPTION_GROUPS) {
				// Log box.
				DrawLogBox ();
				if (sproutMeshGeneratorElement.selectedMeshIndex != meshesList.index &&
					sproutMeshGeneratorElement.selectedMeshIndex < meshesList.count) {
					meshesList.index = sproutMeshGeneratorElement.selectedMeshIndex;
				}
				meshesList.DoLayoutList ();
			} else if (selectedToolbarOption == OPTION_DATA) {
				EditorGUILayout.PropertyField (propNormalMode);
				ShowHelpBox (MSG_NORMAL_MODE);
				if (propNormalMode.intValue != (int)SproutMeshGeneratorElement.NormalMode.PerSprout) {
					EditorGUILayout.Slider (propNormalModeStrength, 0f, 1f, "Normal Mode Strength");
					ShowHelpBox (MSG_NORMAL_MODE_STRENGTH);
				}
			}
			EditorGUILayout.Space ();

			// Seed options.
			DrawSeedOptions ();

			if (EditorGUI.EndChangeCheck ()) {
				ApplySerialized ();
				var meshesEnumerator = previewMeshes.GetEnumerator ();
				while (meshesEnumerator.MoveNext ()) {
					DestroyImmediate (meshesEnumerator.Current.Value);
				}
				previewMeshes.Clear ();
				var materialsEnumerator = previewMaterials.GetEnumerator ();
				while (materialsEnumerator.MoveNext ()) {
					DestroyImmediate (materialsEnumerator.Current.Value);
				}
				previewMaterials.Clear ();
				SproutMeshSelected (meshesList.index);
				UpdatePipeline (GlobalSettings.processingDelayVeryHigh);
				RepaintCanvas ();
				SetUndoControlCounter ();
			}

			// Field descriptors option.
			DrawFieldHelpOptions ();
		}
		/// <summary>
		/// Raises the on disable event.
		/// </summary>
		private void OnDisable() {
			if (meshPreview != null) {
				meshPreview.Clear ();
			}
		}
		/// <summary>
		/// Event called when destroying this editor.
		/// </summary>
		private void OnDestroy() {
			var enumerator = previewMeshes.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				DestroyImmediate (enumerator.Current.Value);
			}
			previewMeshes.Clear ();
			var matEnumerator = previewMaterials.GetEnumerator ();
			while (matEnumerator.MoveNext ()) {
				DestroyImmediate (matEnumerator.Current.Value);
			}
			previewMaterials.Clear ();
			if (meshPreview != null) {
				meshPreview.Clear ();
				if (meshPreview.onDrawHandles != null) {
					meshPreview.onDrawHandles -= OnPreviewMeshDrawHandles;
					meshPreview.onDrawGUI -= OnPreviewMeshDrawGUI;
				}
			}
		}
		#endregion

		#region Mesh Ordereable List
		/// <summary>
		/// Draws the list item header.
		/// </summary>
		/// <param name="rect">Rect.</param>
		private void DrawMeshItemHeader(Rect rect)
		{
			GUI.Label(rect, "Meshes");
		}
		/// <summary>
		/// Draws the list item element.
		/// </summary>
		/// <param name="rect">Rect to draw to.</param>
		/// <param name="index">Index of the item.</param>
		/// <param name="isActive">If set to <c>true</c> the item is active.</param>
		/// <param name="isFocused">If set to <c>true</c> the item is focused.</param>
		private void DrawMeshItemElement (Rect rect, int index, bool isActive, bool isFocused) {
			var sproutMesh = meshesList.serializedProperty.GetArrayElementAtIndex (index);

			int sproutGroupId = sproutMesh.FindPropertyRelative ("groupId").intValue;
			if (sproutGroupId > 0) {
				rect.y += 2;
				SproutGroups sproutGroups = 
					sproutMeshGeneratorElement.pipeline.sproutGroups;
				EditorGUI.DrawRect (new Rect (rect.x, rect.y, 
					EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight), 
					sproutGroups.GetSproutGroupColor(sproutGroupId));
				rect.x += 22;
				EditorGUI.LabelField (new Rect (rect.x, rect.y, 200, EditorGUIUtility.singleLineHeight), 
					"Mesh Assigned to Sprout Group " + sproutGroupId);
			} else {
				rect.y += 2;
				EditorGUI.DrawRect (new Rect (rect.x, rect.y, 
					EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight), 
					Color.black);
				rect.x += 22;
				EditorGUI.LabelField (new Rect (rect.x, rect.y, 
					150, EditorGUIUtility.singleLineHeight), "Unassigned Mesh");
			}

			if (isActive) {
				SproutGroups sproutGroups = sproutMeshGeneratorElement.pipeline.sproutGroups;
				SproutMesh sproutMeshObj = sproutMeshGeneratorElement.sproutMeshes [index];

				if (index != sproutMeshGeneratorElement.selectedMeshIndex) {
					sproutMeshGeneratorElement.selectedMeshIndex = index;
					SproutMeshSelected (index);
				}

				EditorGUILayout.Space ();

				// Sprout group.
				EditorGUILayout.LabelField ("Assignation", BroccoEditorGUI.labelBoldCentered);
				int sproutGroupIndex = EditorGUILayout.Popup ("Sprout Group",
					sproutMeshGeneratorElement.pipeline.sproutGroups.GetSproutGroupIndex (sproutMeshObj.groupId, true),
					sproutMeshGeneratorElement.pipeline.sproutGroups.GetPopupOptions (true));
				int selectedSproutGroupId = 
					sproutMeshGeneratorElement.pipeline.sproutGroups.GetSproutGroupId (sproutGroupIndex);
				if (sproutMeshObj.groupId != selectedSproutGroupId) {
					if (sproutMeshGeneratorElement.GetSproutGroupsAssigned ().Contains (selectedSproutGroupId)) {
						Debug.LogWarning ("The sprout group has already been assigned to a mesh.");
					} else {
						sproutMesh.FindPropertyRelative ("groupId").intValue = selectedSproutGroupId;
					}
				}
				ShowHelpBox (MSG_SPROUT_GROUP);

				// Meshing Mode
				SproutMesh.MeshingMode _meshingMode = (SproutMesh.MeshingMode)EditorGUILayout.EnumPopup ("Meshing Mode", sproutMeshObj.meshingMode);
				if (_meshingMode != sproutMeshObj.meshingMode) {
					sproutMeshObj.meshingMode = _meshingMode;
					if (_meshingMode == SproutMesh.MeshingMode.Shape) {
						sproutGroups.GetSproutGroup (sproutGroupId).branchCollection = null;
					} else {
						sproutGroups.GetSproutGroup (sproutGroupId).branchCollection = sproutMeshObj.branchCollection;
					}
				}
				ShowHelpBox (MSG_MODE);
				EditorGUILayout.Space ();
				if (_meshingMode == SproutMesh.MeshingMode.Shape) {
					DrawMeshItemShape ( sproutMeshObj,  sproutMesh,  sproutGroupId, sproutGroups);
				} else if (_meshingMode == SproutMesh.MeshingMode.BranchCollection) {
					DrawMeshItemBranchCollection ( sproutMeshObj,  sproutMesh,  sproutGroupId, sproutGroups);
				}

				// Scale.
				showSectionScale = 
					EditorGUILayout.BeginFoldoutHeaderGroup (showSectionScale, scaleSectionLabel);
				if (showSectionScale) {
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("scaleMode"), scaleModeLabel);
					ShowHelpBox (MSG_SCALE_MODE);
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("scaleAtTop"), 0f, 5f, scaleAtTopLabel);
					ShowHelpBox (MSG_SCALE_AT_TOP);
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("scaleAtBase"), 0f, 5f, scaleAtBaseLabel);
					ShowHelpBox (MSG_SCALE_AT_BASE);
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("scaleVariance"), 0f, 1f, scaleVarianceLabel);
					ShowHelpBox (MSG_SCALE_VARIANCE);
					EditorGUILayout.CurveField (sproutMesh.FindPropertyRelative ("scaleCurve"),
						sproutGroups.GetSproutGroupColor(sproutGroupId), scaleCurveRange, scaleCurveLabel);
					ShowHelpBox (MSG_SCALE_CURVE);
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup ();

				// Horizontal alignment.
				showSectionHorizontalAlign = 
					EditorGUILayout.BeginFoldoutHeaderGroup (showSectionHorizontalAlign, "Horizontal Align");
				if (showSectionHorizontalAlign) {
					EditorGUI.indentLevel++;
					ShowHelpBox (MSG_HORIZONTAL_ALIGN_AT_TOP);
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("horizontalAlignAtTop"), -1f, 1f, "At Top");
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("horizontalAlignAtBase"), -1f, 1f, "At Base");
					ShowHelpBox (MSG_HORIZONTAL_ALIGN_AT_BASE);
					EditorGUILayout.CurveField (sproutMesh.FindPropertyRelative ("horizontalAlignCurve"),
						sproutGroups.GetSproutGroupColor(sproutGroupId), scaleCurveRange, new GUIContent ("Curve"));
					ShowHelpBox (MSG_HORIZONTAL_ALIGN_CURVE);
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup ();

				// Wind pattern.
				showSectionWindPattern = 
					EditorGUILayout.BeginFoldoutHeaderGroup (showSectionWindPattern, "Wind Pattern");
				if (showSectionWindPattern) {
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("windPattern"), windPatternLabel);
					ShowHelpBox (MSG_WIND_PATTERN);
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup ();
			}
		}
		private void DrawMeshItemShape (SproutMesh sproutMeshObj, SerializedProperty sproutMesh, int sproutGroupId, SproutGroups sproutGroups) {
			EditorGUILayout.LabelField ("Shape for Group " + sproutGroupId, BroccoEditorGUI.labelBoldCentered);

			// Shape Mode.
			EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("shapeMode"), new GUIContent ("Mesh Shape"));
			SproutMesh.ShapeMode sproutMode = sproutMeshObj.shapeMode;
			ShowHelpBox (MSG_SHAPE_MODE);
			if (sproutMode != SproutMesh.ShapeMode.Mesh) {
				// Size and origin.
				showSectionSize = 
					EditorGUILayout.BeginFoldoutHeaderGroup (showSectionSize, "Size");
				if (showSectionSize) {
					EditorGUI.indentLevel++;
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("width"), 0f, 5f);
					ShowHelpBox (MSG_WIDTH);
					if (!sproutMesh.FindPropertyRelative ("overrideHeightWithTexture").boolValue) {
						EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("height"), 0f, 5f);
						ShowHelpBox (MSG_HEIGHT);
					}
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("overrideHeightWithTexture"), 
						new GUIContent ("Override Height with Texture", "Height is proportional to the assigned texture dimension ratio."));
					ShowHelpBox (MSG_OVERRIDE_HEIGHT_WITH_TEXTURE);
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("includeScaleFromAtlas"), 
						new GUIContent ("Include Scale from Atlas", "Apply scaling according to the mapping of a texture atlas."));
					ShowHelpBox (MSG_INCLUDE_SCALE_FROM_ATLAS);
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("pivotX"), 0f, 1f);
					ShowHelpBox (MSG_PIVOT_X);
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("pivotY"), 0f, 1f);
					ShowHelpBox (MSG_PIVOT_Y);
					if (sproutMode == SproutMesh.ShapeMode.PlaneX) {
						// Plane X mode.
						EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("depth"), -3f, 3f, "Depth");
						ShowHelpBox (MSG_DEPTH);
					}
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup ();
			}
			if (sproutMode == SproutMesh.ShapeMode.Mesh) {
				// Mesh mode.
				showSectionMesh = 
					EditorGUILayout.BeginFoldoutHeaderGroup (showSectionMesh, "Mesh");
				if (showSectionMesh) {
					EditorGUI.indentLevel++;
					sproutMesh.FindPropertyRelative ("meshGameObject").objectReferenceValue =
					EditorGUILayout.ObjectField ("Mesh Object", sproutMesh.FindPropertyRelative ("meshGameObject").objectReferenceValue, typeof(GameObject), false);
					ShowHelpBox (MSG_MESH);
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("useMultiLOD"), new GUIContent ("Use Multi LOD"));
					ShowHelpBox (MSG_MULTI_LOD);
					if (sproutMeshObj.useMultiLOD) {
						sproutMesh.FindPropertyRelative ("meshGameObjectLOD1").objectReferenceValue =
							EditorGUILayout.ObjectField ("Mesh Object LOD1", sproutMesh.FindPropertyRelative ("meshGameObjectLOD1").objectReferenceValue, typeof(GameObject), false);
						ShowHelpBox (MSG_MESH_LOD1);
						sproutMesh.FindPropertyRelative ("meshGameObjectLOD2").objectReferenceValue =
							EditorGUILayout.ObjectField ("Mesh Object LOD2", sproutMesh.FindPropertyRelative ("meshGameObjectLOD2").objectReferenceValue, typeof(GameObject), false);
						ShowHelpBox (MSG_MESH_LOD2);
					}
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("meshScale"), new GUIContent ("Scale"));
					ShowHelpBox (MSG_MESH_SCALE);
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("meshRotation"), new GUIContent ("Rotation"));
					ShowHelpBox (MSG_MESH_ROTATION);
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("meshOffset"), new GUIContent ("Offset"));
					ShowHelpBox (MSG_MESH_OFFSET);
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup ();
			} else if (sproutMode == SproutMesh.ShapeMode.GridPlane || (sproutMode == SproutMesh.ShapeMode.Cross)) {
				// Grid plane resolution.
				showSectionResolution = 
					EditorGUILayout.BeginFoldoutHeaderGroup (showSectionResolution, "Resolution");
				if (showSectionResolution) {
					EditorGUI.indentLevel++;
					EditorGUILayout.IntSlider (sproutMesh.FindPropertyRelative ("resolutionWidth"), 1, 10, "Grid Width Resolution");
					ShowHelpBox (MSG_RESOLUTION_WIDTH);
					EditorGUILayout.IntSlider (sproutMesh.FindPropertyRelative ("resolutionHeight"), 1, 10, "Grid Height Resolution");
					ShowHelpBox (MSG_RESOLUTION_HEIGHT);
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup ();

				showSectionGravityBending = 
					EditorGUILayout.BeginFoldoutHeaderGroup (showSectionGravityBending, "Gravity Bending");
				if (showSectionGravityBending) {
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("gravityBendMode"), new GUIContent ("Bend Mode"));
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("gravityBendingAtBase"), 
						-1f, 1f, "Bending at Base");
					ShowHelpBox (MSG_GRAVITY_BENDING_AT_BASE);
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("gravityBendingAtTop"), 
						-1f, 1f, "Bending at Top");
					ShowHelpBox (MSG_GRAVITY_BENDING_AT_TOP);
					EditorGUILayout.CurveField (sproutMesh.FindPropertyRelative ("gravityBendingCurve"),
						sproutGroups.GetSproutGroupColor (sproutGroupId), gravityBendingCurveRange, 
						new GUIContent ("Bending Curve"));
					ShowHelpBox (MSG_GRAVITY_BENDING_CURVE);
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("sideGravityBendingAtBase"), 
						-1f, 1f, "Side Gravity at Base");
					ShowHelpBox (MSG_SIDE_GRAVITY_BENDING_AT_TOP);
					EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("sideGravityBendingAtTop"), 
						-1f, 1f, "Side Gravity at Top");
					ShowHelpBox (MSG_SIDE_GRAVITY_BENDING_AT_BASE);
					EditorGUILayout.CurveField (sproutMesh.FindPropertyRelative ("sideGravityBendingShape"),
						sproutGroups.GetSproutGroupColor (sproutGroupId), gravityBendingCurveRange, 
						new GUIContent ("Side Bending Curve"));
					ShowHelpBox (MSG_SIDE_GRAVITY_BENDING_SHAPE);
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup ();

				if (GlobalSettings.experimentalFastNoiseSprouts) {
				showSectionNoise = 
					EditorGUILayout.BeginFoldoutHeaderGroup (showSectionNoise, "Noise");
				if (showSectionNoise) {
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("noisePattern"), noisePatternLabel);
					ShowHelpBox (MSG_NOISE_PATTERN);
					if (sproutMeshObj.noisePattern != SproutMesh.NoisePattern.None) {
						EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("noiseDistribution"), noiseDistributionLabel);
						ShowHelpBox (MSG_NOISE_DISTRIBUTION);
						EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("noiseResolutionAtBase"), 
							0f, 2f, noiseResolutionAtBaseLabel);
						ShowHelpBox (MSG_NOISE_RESOLUTION_AT_BASE);
						EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("noiseResolutionAtTop"), 
							0f, 2f, noiseResolutionAtTopLabel);
						ShowHelpBox (MSG_NOISE_RESOLUTION_AT_TOP);
						EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("noiseResolutionVariance"), 
							0f, 1f, noiseResolutionVarianceLabel);
						ShowHelpBox (MSG_NOISE_RESOLUTION_VARIANCE);
						EditorGUILayout.CurveField (sproutMesh.FindPropertyRelative ("noiseResolutionCurve"),
							sproutGroups.GetSproutGroupColor (sproutGroupId), noiseResolutionCurveRange, 
							noiseResolutionCurveLabel);
						ShowHelpBox (MSG_NOISE_RESOLUTION_CURVE);
						EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("noiseStrengthAtBase"), 
							0f, 2.5f, noiseStrengthAtBaseLabel);
						ShowHelpBox (MSG_NOISE_STRENGTH_AT_BASE);
						EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("noiseStrengthAtTop"), 
							0f, 2.5f, noiseStrengthAtTopLabel);
						ShowHelpBox (MSG_NOISE_STRENGTH_AT_TOP);
						EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("noiseStrengthVariance"), 
							0f, 1f, noiseStrengthVarianceLabel);
						ShowHelpBox (MSG_NOISE_STRENGTH_VARIANCE);
						EditorGUILayout.CurveField (sproutMesh.FindPropertyRelative ("noiseStrengthCurve"),
							sproutGroups.GetSproutGroupColor (sproutGroupId), noiseStrengthCurveRange, 
							noiseStrengthCurveLabel);
						ShowHelpBox (MSG_NOISE_STRENGTH_CURVE);
					}
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup ();
				}
			}
		}
		private void DrawMeshItemBranchCollection (SproutMesh sproutMeshObj, SerializedProperty sproutMesh, int sproutGroupId, SproutGroups sproutGroups) {
			SerializedProperty propBranchCollection = sproutMesh.FindPropertyRelative ("branchCollection");
			ScriptableObject former = (ScriptableObject)propBranchCollection.objectReferenceValue;
			former = 
				(ScriptableObject)EditorGUILayout.ObjectField (
					"Branch Collection", 
					former,
					typeof (BranchDescriptorCollectionSO),
					false);
			if (former != (ScriptableObject)propBranchCollection.objectReferenceValue) {
				// The Branch Collection SO is set in both the SproutMesh and the SproutGroup object.
				propBranchCollection.objectReferenceValue = former;
				sproutGroups.GetSproutGroup (sproutGroupId).branchCollection = former;
				sproutMeshObj.SetBranchCollectionSO (former);
				UpdateSerialized ();
			}
			if (propBranchCollection.objectReferenceValue == null) {
				EditorGUILayout.HelpBox (MSG_BRANCH_COLLECTION_EMPTY, MessageType.Warning);
			} else {
				string msg = $"{MSG_BRANCH_COLLECTION}\n Branch Definitions: {sproutMeshObj.branchCollectionItemsEnabled.Count}";
				EditorGUILayout.HelpBox (msg, MessageType.None);
				// Draw collection item checkboxes.
				for (int i = 0; i < sproutMeshObj.branchCollectionItemsEnabled.Count; i++) {
					sproutMeshObj.branchCollectionItemsEnabled[i] = 
						EditorGUILayout.Toggle ("Item " + i, sproutMeshObj.branchCollectionItemsEnabled [i]);
				}
			}
			EditorGUILayout.Space ();
			showSectionGravityBending = 
				EditorGUILayout.BeginFoldoutHeaderGroup (showSectionGravityBending, "Gravity Bending");
			if (showSectionGravityBending) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField (sproutMesh.FindPropertyRelative ("gravityBendMode"), new GUIContent ("Bend Mode"));
				EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("gravityBendingAtBase"), 
					-1f, 1f, "Bending at Base");
				ShowHelpBox (MSG_GRAVITY_BENDING_AT_BASE);
				EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("gravityBendingAtTop"), 
					-1f, 1f, "Bending at Top");
				ShowHelpBox (MSG_GRAVITY_BENDING_AT_TOP);
				EditorGUILayout.CurveField (sproutMesh.FindPropertyRelative ("gravityBendingCurve"),
					sproutGroups.GetSproutGroupColor (sproutGroupId), gravityBendingCurveRange, 
					new GUIContent ("Bending Curve"));
				ShowHelpBox (MSG_GRAVITY_BENDING_CURVE);
				EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("sideGravityBendingAtBase"), 
					-1f, 1f, "Side Gravity at Base");
				ShowHelpBox (MSG_SIDE_GRAVITY_BENDING_AT_TOP);
				EditorGUILayout.Slider (sproutMesh.FindPropertyRelative ("sideGravityBendingAtTop"), 
					-1f, 1f, "Side Gravity at Top");
				ShowHelpBox (MSG_SIDE_GRAVITY_BENDING_AT_BASE);
				EditorGUILayout.CurveField (sproutMesh.FindPropertyRelative ("sideGravityBendingShape"),
					sproutGroups.GetSproutGroupColor (sproutGroupId), gravityBendingCurveRange, 
					new GUIContent ("Side Bending Curve"));
				ShowHelpBox (MSG_SIDE_GRAVITY_BENDING_SHAPE);
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndFoldoutHeaderGroup ();
		}
		/// <summary>
		/// Raises the select mesh item event.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnSelectMeshItem (ReorderableList list)
		{
			Undo.RecordObject (sproutMeshGeneratorElement, "Sprout Mesh selected");
			sproutMeshGeneratorElement.selectedMeshIndex = list.index;
			SproutMeshSelected (list.index);
		}
		/// <summary>
		/// Adds a list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnAddMeshItem (ReorderableList list)
		{
			if (sproutMeshGeneratorElement.CanAddSproutMesh ()) {
				Undo.RecordObject (sproutMeshGeneratorElement, "Sprout Mesh added");
				SproutMesh newSproutMesh;
				if (sproutMeshGeneratorElement.selectedMeshIndex >= 0) {
					newSproutMesh = sproutMeshGeneratorElement.sproutMeshes [sproutMeshGeneratorElement.selectedMeshIndex].Clone ();
					newSproutMesh.groupId = -1;
				} else {
					newSproutMesh = new SproutMesh ();
				}
				sproutMeshGeneratorElement.AddSproutMesh (newSproutMesh);
			}
		}
		/// <summary>
		/// Removes a list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnRemoveMeshItem (ReorderableList list)
		{
			int undoGroup = Undo.GetCurrentGroup ();
			Undo.SetCurrentGroupName ("Sprout Mesh removed");
			Undo.RecordObject (sproutMeshGeneratorElement, "Sprout Mesh removed");
			sproutMeshGeneratorElement.RemoveSproutMesh (list.index);
			Undo.RecordObject (sproutMeshGeneratorElement, "Sprout Mesh removed");
			sproutMeshGeneratorElement.selectedMeshIndex = -1;
			Undo.CollapseUndoOperations (undoGroup);
		}
		#endregion

		#region Mesh Preview
		/// <summary>
		/// Determines whether this instance has preview GUI.
		/// </summary>
		/// <returns><c>true</c> if this instance has preview GU; otherwise, <c>false</c>.</returns>
		public override bool HasPreviewGUI () {
			/*
			if (sproutMeshGeneratorNode.selectedToolbar == 1) return false;
			if (meshPreviewEnabled &&
				sproutMeshGeneratorNode.sproutMeshGeneratorElement.sproutMeshes.Count > 0 &&
				sproutMeshGeneratorNode.sproutMeshGeneratorElement.selectedMeshIndex > -1) {
				int index = sproutMeshGeneratorNode.sproutMeshGeneratorElement.selectedMeshIndex;
				SproutMesh sproutMesh = sproutMeshGeneratorNode.sproutMeshGeneratorElement.sproutMeshes [index];
				if (sproutMesh.shapeMode == SproutMesh.ShapeMode.Mesh && sproutMesh.meshGameObject == null) {
					return false;
				}
				return true;
			}
			*/
			return false;
		}
		/*
		/// <summary>
		/// Gets the preview title.
		/// </summary>
		/// <returns>The preview title.</returns>
		public override GUIContent GetPreviewTitle () {
			return previewTitleGUIContent;
		}
		*/
		/// <summary>
		/// Raises the interactive preview GUI event.
		/// </summary>
		/// <param name="r">Rect to draw to.</param>
		/// <param name="background">Background.</param>
		public override void OnInteractivePreviewGUI (Rect r, GUIStyle background) {
			//Mesh renderer missing?
			if(meshPreview == null)	{
				//EditorGUI.DropShadowLabel is used often in these preview areas - it 'fits' well.
				EditorGUI.DropShadowLabel (r, "Mesh Renderer Required");
			}
			else
			{
				meshPreview.RenderViewport (r, background);
				//Rect toolboxRect = new Rect (0, 0, 20, EditorGUIUtility.singleLineHeight);
				Rect toolboxRect = new Rect (r);
				toolboxRect.height = EditorGUIUtility.singleLineHeight;
				//GUI.Button (toolboxRect, "??");
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Shaded")) {
					meshPreview.showWireframe = false;
				}
				if (GUILayout.Button ("Wireframe")) {
					meshPreview.showWireframe = true;
				}
				GUILayout.EndHorizontal ();
			}
		}
		/// <summary>
		/// Get a preview mesh for a SproutMesh.
		/// </summary>
		/// <returns>Mesh for previewing.</returns>
		public Mesh GetPreviewMesh (SproutMesh sproutMesh, SproutMap.SproutMapArea sproutMapArea) {
			SproutMeshBuilder.GetInstance ().globalScale = 1f;
			bool isTwoSided = TreeFactory.GetActiveInstance ().materialManager.IsSproutTwoSided ();
			return SproutMeshBuilder.GetPreview (sproutMesh, isTwoSided, sproutMapArea);
		}
		/// <summary>
		/// Gets the sprout map assigned to a group id.
		/// </summary>
		/// <param name="groupId">Group id.</param>
		/// <returns>Sprout map.</returns>
		SproutMap GetSproutMap (int groupId) {
			if (groupId > -1) {
				SproutMapperElement sproutMapperElement = 
					(SproutMapperElement)sproutMeshGeneratorElement.GetDownstreamElement (PipelineElement.ClassType.SproutMapper);
				if (sproutMapperElement != null) {
					SproutMap sproutMap = sproutMapperElement.GetSproutMap (groupId);
					return sproutMap;
				}
			}
			SproutMap defaultSproutMap = new SproutMap ();
			return defaultSproutMap;
		}
		/// <summary>
		/// Show a preview mesh.
		/// </summary>
		/// <param name="index">Index.</param>
		public void SproutMeshSelected (int index) {
			if (index > 0 && index < sproutMeshGeneratorElement.sproutMeshes.Count) {
				SproutMesh sproutMeshObj = sproutMeshGeneratorElement.sproutMeshes [index];
				// For SproutMesh BranchCollection, check if the enabled list has been set.
				if (sproutMeshObj.meshingMode == SproutMesh.MeshingMode.BranchCollection && sproutMeshObj.branchCollection != null) {
					BranchDescriptorCollection bdc = ((BranchDescriptorCollectionSO)sproutMeshObj.branchCollection).branchDescriptorCollection;
					if (bdc.descriptorImplId == BranchDescriptorCollection.BASE_COLLECTION || 
						bdc.descriptorImplId == BranchDescriptorCollection.SNAPSHOT_COLLECTION)
					{
						if (bdc.snapshots.Count != sproutMeshObj.branchCollectionItemsEnabled.Count) {
							sproutMeshObj.SetBranchCollectionSO (sproutMeshObj.branchCollection);
							UpdateSerialized ();
						}
					} else if (bdc.descriptorImplId == BranchDescriptorCollection.VARIATION_COLLECTION) {
						if (bdc.variations.Count != sproutMeshObj.branchCollectionItemsEnabled.Count) {
							sproutMeshObj.SetBranchCollectionSO (sproutMeshObj.branchCollection);
							UpdateSerialized ();
						}
					}
				}
			}
			/*
			Mesh mesh = null;
			Material material = null;
			if (!previewMeshes.ContainsKey (index) || previewMeshes [index] == null) {
				SproutMesh sproutMesh = sproutMeshGeneratorNode.sproutMeshGeneratorElement.sproutMeshes [index];
				SproutMap sproutMap = GetSproutMap (sproutMesh.groupId);
				SproutMap.SproutMapArea sproutMapArea = sproutMap.GetMapArea ();
				mesh = GetPreviewMesh (sproutMesh, sproutMapArea);
				if (sproutMap != null) {
					material = MaterialManager.GetPreviewLeavesMaterial (sproutMap, sproutMapArea);
					previewMaterials.Add (index, material);
				}
				previewMeshes.Add (index, mesh);
			} else {
				mesh = previewMeshes [index];
				if (previewMaterials.ContainsKey (index)) {
					material = previewMaterials [index];
				}
			}
			meshPreview.Clear ();
			meshPreview.CreateViewport ();
			mesh.RecalculateBounds();
			if (material != null) {
				meshPreview.AddMesh (0, mesh, material, true);
			} else {
				meshPreview.AddMesh (0, mesh, true);
			}
			if (!autoZoomUsed) {
				autoZoomUsed = true;
				meshPreview.CalculateZoom (mesh);
			}
			*/
		}
		/// <summary>
		/// Draw additional handles on the mesh preview area.
		/// </summary>
		/// <param name="r">Rect</param>
		/// <param name="camera">Camera</param>
		public void OnPreviewMeshDrawHandles (Rect r, Camera camera) {
			Handles.color = Color.green;
			Handles.ArrowHandleCap (0,
				Vector3.zero, 
				Quaternion.LookRotation (Vector3.down), 
				1f * MeshPreview.GetHandleSize (Vector3.zero, camera), 
				EventType.Repaint);
		}
		/// <summary>
		/// Draws GUI elements on the mesh preview area.
		/// </summary>
		/// <param name="r">Rect</param>
		/// <param name="camera">Camera</param>
		public void OnPreviewMeshDrawGUI (Rect r, Camera camera) {
			r.y += EditorGUIUtility.singleLineHeight;
			GUI.Label (r, "[Pivot]", pivotLabelStyle);
			r.y += EditorGUIUtility.singleLineHeight;
			GUI.Label (r, "[Gravity]", gravityVectorLabelStyle);
		}
		#endregion

		#region Debug
		#if BROCCOLI_DEVEL
		/// <summary>
		/// Displays specific debug information for the type of element selected on the pipeline.
		/// </summary>
		override public void DrawDebugInfoSpecific () {
			string debugInfo = pipelineElement.elementName;
			debugInfo += string.Format("\n{0} SproutMeshes", sproutMeshGeneratorElement.sproutMeshes.Count);
			for (int i = 0; i < sproutMeshGeneratorElement.sproutMeshes.Count; i++) {
				DebugSproutMeshInfo (ref debugInfo, i, sproutMeshGeneratorElement.sproutMeshes [i]);
			}
			EditorGUILayout.HelpBox (debugInfo, MessageType.None);
		}
		/// <summary>
		/// Adds debug information for a SproutMesh instance.
		/// </summary>
		/// <param name="debugInfo">Debug information to apped data to.</param>
		/// <param name="index">Index of the SproutMesh instance.</param>
		/// <param name="sproutMesh">SproutMesh instance.</param>
		private void DebugSproutMeshInfo (ref string debugInfo, int index, SproutMesh sproutMesh) {
			if (sproutMesh != null) {
				debugInfo += $"\nSproutMesh {index} [{sproutMesh.meshingMode}]:";
				debugInfo += $"\n  Group Id: {sproutMesh.groupId}";
				debugInfo += $"\n  Meshing Mode: {sproutMesh.meshingMode}";
				if (sproutMesh.meshingMode == SproutMesh.MeshingMode.Shape) {
					debugInfo += $"\n  Shape Mode: {sproutMesh.shapeMode}";
				} else {
					debugInfo += $"\n  Branch Collection: {sproutMesh.branchCollection}";
					if (sproutMesh.branchCollection != null) {
						BranchDescriptorCollectionSO bdcso = (BranchDescriptorCollectionSO)sproutMesh.branchCollection;
						BranchDescriptorCollection bdc = bdcso.branchDescriptorCollection;
						if (bdc != null) {
							debugInfo += $"\n    Hash: {bdc.GetHashCode ()}";
							debugInfo += $"\n    ImplId: {bdc.descriptorImplId}";
							debugInfo += $"\n    ImplName: {bdc.descriptorImplName}";
							debugInfo += $"\n    Timestamp: {bdc.timestamp}";
							debugInfo += $"\n    Snapshots: {bdc.snapshots.Count}";
							debugInfo += $"\n    Variantions: {bdc.variations.Count}";
						}
					}
				}
			}
		}
		#endif
		#endregion
	}
}