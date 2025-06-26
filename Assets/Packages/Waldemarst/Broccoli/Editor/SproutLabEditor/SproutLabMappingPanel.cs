using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;

using Broccoli.Pipe;
using Broccoli.Base;
using Broccoli.Utils;

namespace Broccoli.BroccoEditor
{
    public class SproutLabMappingPanel {
        #region Vars
        bool isInit = false;
        static string containerName = "mappingPanel";
		SproutLabEditor sproutLabEditor = null;
		public bool requiresRepaint = false;
		bool listenGUIEvents = true;
        #endregion

        #region GUI Vars
        /// <summary>
        /// Container for the UI.
        /// </summary>
        VisualElement container;
        /// <summary>
        /// Rect used to draw the components in this panel.
        /// </summary>
        Rect rect;
		/// <summary>
		/// Side panel xml.
		/// </summary>
		private VisualTreeAsset panelXml;
		/// <summary>
		/// Side panel style.
		/// </summary>
		private StyleSheet panelStyle;
		/// <summary>
		/// Path to the side panel xml.
		/// </summary>
		private string panelXmlPath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/SproutLabMappingPanelView.uxml"; }
		}
		/// <summary>
		/// Path to the side style.
		/// </summary>
		private string panelStylePath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/SproutLabPanelStyle.uss"; }
		}
		private static List<string> optionItems = new List<string> {"Stem", "Sprout A", "Sprout B"};
		private static List<string> optionCrownItems = new List<string> {"Stem", "Sprout A", "Sprout B", "Crown"};
		private Action<VisualElement, int> bindOptionItem = (e, i) => (e as Label).text = optionItems[i];
		private Action<VisualElement, int> bindOptionCrownItem = (e, i) => (e as Label).text = optionCrownItems[i];
		private static string optionsListName = "options-list";
		private static string structuresListName = "structures-list";
		private static string stemContainerName = "container-stem";
		private static string sproutContainerName = "container-sprout-a";
		private static string sproutBContainerName = "container-sprout-b";
		private static string crownContainerName = "container-sprout-crown";
		private static string dissolveContainerName = "sprout-dissolve-container";
		private static string iconSaturationName = "icon-saturation";
		private static string iconShadeName = "icon-shade";
		private static string iconTintName = "icon-tint";
		private static string iconSurfaceName = "icon-surface";
		private static string iconDissolveName = "icon-dissolve";

		private static string branchSaturationName = "stem-saturation";
		private static string branchSubsurfaceName = "stem-subsurface";

		private static string sproutSaturationResetName = "sprout-a-saturation-reset";
		private static string sproutShadeResetName = "sprout-a-shade-reset";
		private static string sproutTintResetName = "sprout-a-tint-reset";
		private static string sproutDissolveResetName = "sprout-a-dissolve-reset";
		private static string sproutTintColorName = "sprout-a-tint-color";
		private static string sproutTintName = "sprout-a-tint";
		private static string sproutTintModeName = "sprout-a-tint-mode";
		private static string sproutTintModeInvertName = "sprout-a-tint-mode-invert";
		private static string sproutTintVarianceName = "sprout-a-tint-variance";
		private static string sproutShadeName = "sprout-a-shade";
		private static string sproutShadeModeName = "sprout-a-shade-mode";
		private static string sproutShadeModeInvertName = "sprout-a-shade-mode-invert";
		private static string sproutShadeVarianceName = "sprout-a-shade-variance";
		private static string sproutDissolveName = "sprout-a-dissolve";
		private static string sproutDissolveModeName = "sprout-a-dissolve-mode";
		private static string sproutDissolveModeInvertName = "sprout-a-dissolve-mode-invert";
		private static string sproutDissolveVarianceName = "sprout-a-dissolve-variance";
		private static string sproutSaturationName = "sprout-a-saturation";
		private static string sproutSaturationModeName = "sprout-a-saturation-mode";
		private static string sproutSaturationModeInvertName = "sprout-a-saturation-mode-invert";
		private static string sproutSaturationVarianceName = "sprout-a-saturation-variance";
		private static string sproutMetallicName = "sprout-a-metallic";
		private static string sproutGlossinessName = "sprout-a-glossiness";
		private static string sproutSubsurfaceName = "sprout-a-subsurface";

		private ListView optionsList; 
		private IMGUIContainer structuresList;
		private VisualElement stemContainer;
		private VisualElement sproutContainerElem;
		private VisualElement sproutBContainerElem;
		private VisualElement sproutCrownContainerElem;
		private VisualElement sproutDissolveContainerElem;
		private VisualElement sproutBDissolveContainerElem;
		private VisualElement sproutCrownDissolveContainerElem;

		private Slider branchSaturationElem;
		private Slider branchSubsurfaceElem;
		private Button sproutSaturationResetElem;
		private Button sproutShadeResetElem;
		private Button sproutTintResetElem;
		private Button sproutDissolveResetElem;
		private ColorField sproutTintColorElem;
		private MinMaxSlider sproutTintElem;
		private EnumField sproutTintModeElem;
		private Toggle sproutTintModeInvertElem;
		private Slider sproutTintVarianceElem;
		private MinMaxSlider sproutShadeElem;
		private EnumField sproutShadeModeElem;
		private Toggle sproutShadeModeInvertElem;
		private Slider sproutShadeVarianceElem;
		private MinMaxSlider sproutDissolveElem;
		private EnumField sproutDissolveModeElem;
		private Toggle sproutDissolveModeInvertElem;
		private Slider sproutDissolveVarianceElem;
		private MinMaxSlider sproutSaturationElem;
		private EnumField sproutSaturationModeElem;
		private Toggle sproutSaturationModeInvertElem;
		private Slider sproutSaturationVarianceElem;
		private Slider sproutMetallicElem;
		private Slider sproutGlossinessElem;
		private Slider sproutSubsurfaceElem;
        #endregion

        #region Constructor
        public SproutLabMappingPanel (SproutLabEditor sproutLabEditor) {
            Initialize (sproutLabEditor);
        }
        #endregion

        #region Init
		public void SproutSelected () {
			/*
			if (sproutLabEditor.snapSettings.hasCrown) {
				optionsList.bindItem = bindOptionCrownItem;
				optionsList.itemsSource = optionCrownItems;
				optionsList.selectedIndex = 0;
			} else {
				optionsList.bindItem = bindOptionItem;
				optionsList.itemsSource = optionItems;
			}
			#if UNITY_2021_2_OR_NEWER
			optionsList.Rebuild ();
			#else
			optionsList.Refresh ();
			#endif
			*/
		}
		private void OnStyleSelectionChanged(int selectedStyle) {
			stemContainer.style.display = DisplayStyle.None;
			sproutContainerElem.style.display = DisplayStyle.None;
			sproutBContainerElem.style.display = DisplayStyle.None;
			sproutCrownContainerElem.style.display = DisplayStyle.None;
			if (selectedStyle == 0) {
				stemContainer.style.display = DisplayStyle.Flex;
			} else {
				sproutContainerElem.style.display = DisplayStyle.Flex;
			}
		}
        public void Initialize (SproutLabEditor sproutLabEditor) {
			this.sproutLabEditor = sproutLabEditor;
            if (!isInit) {
                // Start the container UIElement.
                container = new VisualElement ();
                container.name = containerName;

				// Load the VisualTreeAsset from a file 
				panelXml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(panelXmlPath);

				// Create a new instance of the root VisualElement
				container.Add (panelXml.CloneTree()); 

				// Init List and Containers.
				optionsList = container.Q<ListView> (optionsListName);
				structuresList = container.Q<IMGUIContainer> (structuresListName);
				stemContainer = container.Q<VisualElement> (stemContainerName);
				sproutContainerElem = container.Q<VisualElement> (sproutContainerName);
				sproutBContainerElem = container.Q<VisualElement> (sproutBContainerName);
				sproutCrownContainerElem = container.Q<VisualElement> (crownContainerName);
				SetContainerIcons (sproutContainerElem);
				SetContainerIcons (sproutBContainerElem);
				SetContainerIcons (sproutCrownContainerElem);

				// Dissolve containers (hide if experimental is disabled).
				if (!Broccoli.Base.GlobalSettings.experimentalSproutLabDissolveSprouts) {
					sproutDissolveContainerElem = sproutContainerElem.Q<VisualElement> (dissolveContainerName);
					sproutBDissolveContainerElem = sproutBContainerElem.Q<VisualElement> (dissolveContainerName);
					sproutCrownDissolveContainerElem = sproutCrownContainerElem.Q<VisualElement> (dissolveContainerName);
					sproutDissolveContainerElem.style.display = DisplayStyle.None;
					sproutBDissolveContainerElem.style.display = DisplayStyle.None;
					sproutCrownDissolveContainerElem.style.display = DisplayStyle.None;
				}

				// Structure List
				structuresList.onGUIHandler = DrawStructureStyleList;

				// The "makeItem" function will be called as needed
				// when the ListView needs more items to render
				Func<VisualElement> makeItem = () => new Label();

				optionsList.makeItem = makeItem;
				optionsList.bindItem = bindOptionItem;
				optionsList.itemsSource = optionItems;
				#if UNITY_2021_2_OR_NEWER
				optionsList.Rebuild ();
				#else
				optionsList.Refresh ();
				#endif

				/*
				
				#if UNITY_2022_2_OR_NEWER
				optionsList.selectionChanged -= OnSelectionChanged;
				optionsList.selectionChanged += OnSelectionChanged;
				#else
				optionsList.onSelectionChange -= OnStyleSelectionChanged;
				optionsList.onSelectionChange += OnStyleSelectionChanged;
				#endif
				optionsList.selectedIndex = 0;
				*/
				InitializeBranchStyle ();
				InitializeSproutStyle ();
				// TODOSSS remove
				/*
				InitializeSproutStyleB ();
				InitializeSproutStyleCrown ();
				*/

				isInit = true;

                RefreshValues ();
            }
        }
		/// <summary>
		/// Structure views: branch or leaves.
		/// </summary>
		private static GUIContent[] structureViewOptions = new GUIContent[1] {new GUIContent ("Branches", "Settings for branches.")};
		/// <summary>
		/// Style selected.
		/// </summary>
		int currentStyleView = 0;
		private static string labelStructures = "Structures";
		/// <summary>
		/// Width for the left column on secondary panels.
		/// </summary>
		private int secondaryPanelColumnWidth = 120;
		private int structureSelected = -1;
		void DrawStructureStyleList ()
		{
			EditorGUILayout.BeginVertical ();
			EditorGUILayout.LabelField (labelStructures, BroccoEditorGUI.labelBoldCentered);
			int _currentStyleView = GUILayout.SelectionGrid (currentStyleView, structureViewOptions, 1, GUILayout.Width (secondaryPanelColumnWidth));
			if (_currentStyleView != currentStyleView) {
				structureSelected = _currentStyleView;
				currentStyleView = _currentStyleView;
				sproutLabEditor.sproutStyleList.index = -1;
				sproutLabEditor.selectedSproutStyleIndex = -1;
				OnStyleSelectionChanged (structureSelected);
				RefreshStyleValues ();
			}
			EditorGUILayout.Space ();
			if (sproutLabEditor.sproutStyleList != null) {
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(4f);
				EditorGUILayout.BeginVertical();
				sproutLabEditor.sproutStyleList.DoLayoutList ();
				EditorGUILayout.EndVertical();
				GUILayout.Space(4f);
				EditorGUILayout.EndHorizontal();
			}
			if (structureSelected != sproutLabEditor.selectedSproutStyleIndex + 1) {
				// Selection of structure changed. Reflect it on the panel.
				currentStyleView = -1;
				structureSelected = sproutLabEditor.selectedSproutStyleIndex + 1;
				OnStyleSelectionChanged (structureSelected);
				RefreshStyleValues ();
			}
			EditorGUILayout.EndVertical ();
		}

		void InitializeBranchStyle () {
			// Saturation Range.
			branchSaturationElem = container.Q<Slider> (branchSaturationName);
			branchSaturationElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.branchColorSaturation = newVal;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (branchSaturationElem);
			// Subsurface Range.
			branchSubsurfaceElem = container.Q<Slider> (branchSubsurfaceName);
			branchSubsurfaceElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.branchSubsurface = newVal;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (branchSubsurfaceElem);
		}
		
		void InitializeSproutStyle () {
			// Saturation Range.
			sproutSaturationElem = container.Q<MinMaxSlider> (sproutSaturationName);
			sproutSaturationElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.minColorSaturation = newVal.x;
					sproutLabEditor.selectedSproutStyle.maxColorSaturation = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutSaturationElem);

			// Saturation Mode.
			sproutSaturationModeElem = container.Q<EnumField> (sproutSaturationModeName);
			sproutSaturationModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform);
			sproutSaturationModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.sproutSaturationMode = (BranchDescriptorCollection.SproutStyle.SproutSaturationMode)evt.newValue;
					if (sproutLabEditor.selectedSproutStyle.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
						sproutSaturationModeInvertElem.style.display = DisplayStyle.None;
						sproutSaturationVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutSaturationModeInvertElem.style.display = DisplayStyle.Flex;
						sproutSaturationVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Invert.
			sproutSaturationModeInvertElem = container.Q<Toggle> (sproutSaturationModeInvertName);
			sproutSaturationModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.invertSproutSaturationMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Variance.
			sproutSaturationVarianceElem = container.Q<Slider> (sproutSaturationVarianceName);
			sproutSaturationVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.sproutSaturationVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutSaturationVarianceElem);

			// Shade Range
			sproutShadeElem = container.Q<MinMaxSlider> (sproutShadeName);
			sproutShadeElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.minColorShade = newVal.x;
					sproutLabEditor.selectedSproutStyle.maxColorShade = newVal.y;
					OnEdit (true);
					UpdateShade (sproutLabEditor.selectedSproutStyleIndex);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutShadeElem);

			// Shade Mode.
			sproutShadeModeElem = container.Q<EnumField> (sproutShadeModeName);
			sproutShadeModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform);
			sproutShadeModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.sproutShadeMode = (BranchDescriptorCollection.SproutStyle.SproutShadeMode)evt.newValue;
					if (sproutLabEditor.selectedSproutStyle.sproutShadeMode == BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform) {
						sproutShadeModeInvertElem.style.display = DisplayStyle.None;
						sproutShadeVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutShadeModeInvertElem.style.display = DisplayStyle.Flex;
						sproutShadeVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (true);
					UpdateShade (sproutLabEditor.selectedSproutStyleIndex);
				}
			});

			// Shade Invert.
			sproutShadeModeInvertElem = container.Q<Toggle> (sproutShadeModeInvertName);
			sproutShadeModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.invertSproutShadeMode = evt.newValue;
					OnEdit (true);
					UpdateShade (sproutLabEditor.selectedSproutStyleIndex);
				}
			});

			// Shade Variance.
			sproutShadeVarianceElem = container.Q<Slider> (sproutShadeVarianceName);
			sproutShadeVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.sproutShadeVariance = evt.newValue;
					OnEdit (true);
					UpdateShade (sproutLabEditor.selectedSproutStyleIndex);
				}
			});
			SproutLabEditor.SetupSlider (sproutShadeVarianceElem);

			// Dissolve Range
			sproutDissolveElem = container.Q<MinMaxSlider> (sproutDissolveName);
			sproutDissolveElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.minColorDissolve = newVal.x;
					sproutLabEditor.selectedSproutStyle.maxColorDissolve = newVal.y;
					OnEdit (true);
					UpdateDissolve (sproutLabEditor.selectedSproutStyleIndex);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutDissolveElem);

			// Dissolve Mode.
			sproutDissolveModeElem = container.Q<EnumField> (sproutDissolveModeName);
			sproutDissolveModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform);
			sproutDissolveModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.sproutDissolveMode = (BranchDescriptorCollection.SproutStyle.SproutDissolveMode)evt.newValue;
					if (sproutLabEditor.selectedSproutStyle.sproutDissolveMode == BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform) {
						sproutDissolveModeInvertElem.style.display = DisplayStyle.None;
						sproutDissolveVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutDissolveModeInvertElem.style.display = DisplayStyle.Flex;
						sproutDissolveVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (true);
					UpdateDissolve (sproutLabEditor.selectedSproutStyleIndex);
				}
			});

			// Dissolve Invert.
			sproutDissolveModeInvertElem = container.Q<Toggle> (sproutDissolveModeInvertName);
			sproutDissolveModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.invertSproutDissolveMode = evt.newValue;
					OnEdit (true);
					UpdateDissolve (sproutLabEditor.selectedSproutStyleIndex);
				}
			});

			// Dissolve Variance.
			sproutDissolveVarianceElem = container.Q<Slider> (sproutDissolveVarianceName);
			sproutDissolveVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.sproutDissolveVariance = evt.newValue;
					OnEdit (true);
					UpdateDissolve (sproutLabEditor.selectedSproutStyleIndex);
				}
			});
			SproutLabEditor.SetupSlider (sproutDissolveVarianceElem);

			// Tint Color.
			sproutTintColorElem = container.Q<ColorField> (sproutTintColorName);
			sproutTintColorElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.colorTint = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Range.
			sproutTintElem = container.Q<MinMaxSlider> (sproutTintName);
			sproutTintElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.minColorTint = newVal.x;
					sproutLabEditor.selectedSproutStyle.maxColorTint = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutTintElem);

			// Tint Mode.
			sproutTintModeElem = container.Q<EnumField> (sproutTintModeName);
			sproutTintModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform);
			sproutTintModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.sproutTintMode = (BranchDescriptorCollection.SproutStyle.SproutTintMode)evt.newValue;
					if (sproutLabEditor.selectedSproutStyle.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
						sproutTintModeInvertElem.style.display = DisplayStyle.None;
						sproutTintVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutTintModeInvertElem.style.display = DisplayStyle.Flex;
						sproutTintVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Invert.
			sproutTintModeInvertElem = container.Q<Toggle> (sproutTintModeInvertName);
			sproutTintModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.invertSproutTintMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Variance.
			sproutTintVarianceElem = container.Q<Slider> (sproutTintVarianceName);
			sproutTintVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.sproutTintVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutTintVarianceElem);

			// Metallic Slider.
			sproutMetallicElem = container.Q<Slider> (sproutMetallicName);
			sproutMetallicElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.metallic = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutMetallicElem);

			// Glossiness Slider.
			sproutGlossinessElem = container.Q<Slider> (sproutGlossinessName);
			sproutGlossinessElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.glossiness = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutGlossinessElem);

			// Subsurface Slider.
			sproutSubsurfaceElem = container.Q<Slider> (sproutSubsurfaceName);
			sproutSubsurfaceElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.selectedSproutStyle.subsurface = newVal;
					OnEdit (true, true, false, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutSubsurfaceElem);

			// Saturation Reset Button.
			sproutSaturationResetElem = container.Q<Button> (sproutSaturationResetName);
			sproutSaturationResetElem?.UnregisterCallback<ClickEvent> (ResetSproutSaturation);
			sproutSaturationResetElem?.RegisterCallback<ClickEvent> (ResetSproutSaturation);
			// Shade Reset Button.
			sproutShadeResetElem = container.Q<Button> (sproutShadeResetName);
			sproutShadeResetElem?.UnregisterCallback<ClickEvent> (ResetSproutShade);
			sproutShadeResetElem?.RegisterCallback<ClickEvent> (ResetSproutShade);
			// Tint Reset Button.
			sproutTintResetElem = container.Q<Button> (sproutTintResetName);
			sproutTintResetElem?.UnregisterCallback<ClickEvent> (ResetSproutTint);
			sproutTintResetElem?.RegisterCallback<ClickEvent> (ResetSproutTint);
			// Dissolve Reset Button.
			sproutDissolveResetElem = container.Q<Button> (sproutDissolveResetName);
			sproutDissolveResetElem?.UnregisterCallback<ClickEvent> (ResetSproutDissolve);
			sproutDissolveResetElem?.RegisterCallback<ClickEvent> (ResetSproutDissolve);
		}
		void ResetSproutSaturation (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.selectedSproutStyle.minColorSaturation = 1;
				sproutLabEditor.selectedSproutStyle.maxColorSaturation = 1;
				sproutLabEditor.selectedSproutStyle.sproutSaturationMode = BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform;
				sproutLabEditor.selectedSproutStyle.invertSproutSaturationMode = false;
				sproutLabEditor.selectedSproutStyle.sproutSaturationVariance = 0;
				RefreshStyleValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		void ResetSproutShade (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.selectedSproutStyle.minColorShade = 1;
				sproutLabEditor.selectedSproutStyle.maxColorShade = 1;
				sproutLabEditor.selectedSproutStyle.sproutShadeMode = BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform;
				sproutLabEditor.selectedSproutStyle.invertSproutShadeMode = false;
				sproutLabEditor.selectedSproutStyle.sproutShadeVariance = 0;
				RefreshStyleValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		void ResetSproutTint (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.selectedSproutStyle.minColorTint = 0;
				sproutLabEditor.selectedSproutStyle.maxColorTint = 0;
				sproutLabEditor.selectedSproutStyle.sproutTintMode = BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform;
				sproutLabEditor.selectedSproutStyle.invertSproutTintMode = false;
				sproutLabEditor.selectedSproutStyle.sproutTintVariance = 0;
				RefreshStyleValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		void ResetSproutDissolve (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.selectedSproutStyle.minColorDissolve = 0;
				sproutLabEditor.selectedSproutStyle.maxColorDissolve = 0;
				sproutLabEditor.selectedSproutStyle.sproutDissolveMode = BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform;
				sproutLabEditor.selectedSproutStyle.invertSproutDissolveMode = false;
				sproutLabEditor.selectedSproutStyle.sproutDissolveVariance = 0;
				RefreshStyleValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		/*
		void InitializeSproutStyleB () {
			// SPROUT B.
			// Saturation Range.
			sproutBSaturationElem = container.Q<MinMaxSlider> (sproutBSaturationName);
			sproutBSaturationElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorSaturation = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorSaturation = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutBSaturationElem);

			// Saturation Mode.
			sproutBSaturationModeElem = container.Q<EnumField> (sproutBSaturationModeName);
			sproutBSaturationModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform);
			sproutBSaturationModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationMode = (BranchDescriptorCollection.SproutStyle.SproutSaturationMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
						sproutBSaturationModeInvertElem.style.display = DisplayStyle.None;
						sproutBSaturationVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutBSaturationModeInvertElem.style.display = DisplayStyle.Flex;
						sproutBSaturationVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Invert.
			sproutBSaturationModeInvertElem = container.Q<Toggle> (sproutBSaturationModeInvertName);
			sproutBSaturationModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutSaturationMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Variance.
			sproutBSaturationVarianceElem = container.Q<Slider> (sproutBSaturationVarianceName);
			sproutBSaturationVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBSaturationVarianceElem);

			// Shade Range
			sproutBShadeElem = container.Q<MinMaxSlider> (sproutBShadeName);
			sproutBShadeElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorShade = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorShade = newVal.y;
					OnEdit (true);
					UpdateShade (1);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutBShadeElem);

			// Shade Mode.
			sproutBShadeModeElem = container.Q<EnumField> (sproutBShadeModeName);
			sproutBShadeModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform);
			sproutBShadeModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutShadeMode = (BranchDescriptorCollection.SproutStyle.SproutShadeMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutShadeMode == BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform) {
						sproutBShadeModeInvertElem.style.display = DisplayStyle.None;
						sproutBShadeVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutBShadeModeInvertElem.style.display = DisplayStyle.Flex;
						sproutBShadeVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (true);
					UpdateShade (1);
				}
			});

			// Shade Invert.
			sproutBShadeModeInvertElem = container.Q<Toggle> (sproutBShadeModeInvertName);
			sproutBShadeModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutShadeMode = evt.newValue;
					OnEdit (true);
					UpdateShade (1);
				}
			});

			// Shade Variance.
			sproutBShadeVarianceElem = container.Q<Slider> (sproutBShadeVarianceName);
			sproutBShadeVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutShadeVariance = evt.newValue;
					OnEdit (true);
					UpdateShade (1);
				}
			});
			SproutLabEditor.SetupSlider (sproutBShadeVarianceElem);

			// Dissolve Range
			sproutBDissolveElem = container.Q<MinMaxSlider> (sproutBDissolveName);
			sproutBDissolveElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorDissolve = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorDissolve = newVal.y;
					OnEdit (true);
					UpdateDissolve (0);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutBDissolveElem);

			// Dissolve Mode.
			sproutBDissolveModeElem = container.Q<EnumField> (sproutBDissolveModeName);
			sproutBDissolveModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform);
			sproutBDissolveModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveMode = (BranchDescriptorCollection.SproutStyle.SproutDissolveMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveMode == BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform) {
						sproutBDissolveModeInvertElem.style.display = DisplayStyle.None;
						sproutBDissolveVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutBDissolveModeInvertElem.style.display = DisplayStyle.Flex;
						sproutBDissolveVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (true);
					UpdateDissolve (0);
				}
			});

			// Dissolve Invert.
			sproutBDissolveModeInvertElem = container.Q<Toggle> (sproutBDissolveModeInvertName);
			sproutBDissolveModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutDissolveMode = evt.newValue;
					OnEdit (true);
					UpdateDissolve (0);
				}
			});

			// Dissolve Variance.
			sproutBDissolveVarianceElem = container.Q<Slider> (sproutBDissolveVarianceName);
			sproutBDissolveVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveVariance = evt.newValue;
					OnEdit (true);
					UpdateDissolve (0);
				}
			});
			SproutLabEditor.SetupSlider (sproutBDissolveVarianceElem);

			// Tint Color.
			sproutBTintColorElem = container.Q<ColorField> (sproutBTintColorName);
			sproutBTintColorElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.colorTint = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Range.
			sproutBTintElem = container.Q<MinMaxSlider> (sproutBTintName);
			sproutBTintElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorTint = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorTint = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutBTintElem);

			// Tint Mode.
			sproutBTintModeElem = container.Q<EnumField> (sproutBTintModeName);
			sproutBTintModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform);
			sproutBTintModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintMode = (BranchDescriptorCollection.SproutStyle.SproutTintMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
						sproutBTintModeInvertElem.style.display = DisplayStyle.None;
						sproutBTintVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutBTintModeInvertElem.style.display = DisplayStyle.Flex;
						sproutBTintVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Invert.
			sproutBTintModeInvertElem = container.Q<Toggle> (sproutBTintModeInvertName);
			sproutBTintModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutTintMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Variance.
			sproutBTintVarianceElem = container.Q<Slider> (sproutBTintVarianceName);
			sproutBTintVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBTintVarianceElem);

			// Metallic Slider.
			sproutBMetallicElem = container.Q<Slider> (sproutBMetallicName);
			sproutBMetallicElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.metallic = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBMetallicElem);

			// Glossiness Slider.
			sproutBGlossinessElem = container.Q<Slider> (sproutBGlossinessName);
			sproutBGlossinessElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.glossiness = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBGlossinessElem);

			// Subsurface Slider.
			sproutBSubsurfaceElem = container.Q<Slider> (sproutBSubsurfaceName);
			sproutBSubsurfaceElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.subsurface = newVal;
					OnEdit (true, true, false, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBSubsurfaceElem);

			// Saturation Reset Button.
			sproutBSaturationResetElem = container.Q<Button> (sproutBSaturationResetName);
			sproutBSaturationResetElem?.UnregisterCallback<ClickEvent> (ResetSproutBSaturation);
			sproutBSaturationResetElem?.RegisterCallback<ClickEvent> (ResetSproutBSaturation);
			// Shade Reset Button.
			sproutBShadeResetElem = container.Q<Button> (sproutBShadeResetName);
			sproutBShadeResetElem?.UnregisterCallback<ClickEvent> (ResetSproutBShade);
			sproutBShadeResetElem?.RegisterCallback<ClickEvent> (ResetSproutBShade);
			// Tint Reset Button.
			sproutBTintResetElem = container.Q<Button> (sproutBTintResetName);
			sproutBTintResetElem?.UnregisterCallback<ClickEvent> (ResetSproutBTint);
			sproutBTintResetElem?.RegisterCallback<ClickEvent> (ResetSproutBTint);
			// Dissolve Reset Button.
			sproutBDissolveResetElem = container.Q<Button> (sproutBDissolveResetName);
			sproutBDissolveResetElem?.UnregisterCallback<ClickEvent> (ResetSproutBDissolve);
			sproutBDissolveResetElem?.RegisterCallback<ClickEvent> (ResetSproutBDissolve);
		}
		void ResetSproutBSaturation (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorSaturation = 1;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorSaturation = 1;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationMode = 
					BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutSaturationMode = false;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationVariance = 0;
				RefreshStyleBValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		void ResetSproutBShade (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorShade = 1;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorShade = 1;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutShadeMode = 
					BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutShadeMode = false;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutShadeVariance = 0;
				RefreshStyleBValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		void ResetSproutBTint (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorTint = 0;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorTint = 0;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintMode = 
					BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutTintMode = false;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintVariance = 0;
				RefreshStyleBValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		void ResetSproutBDissolve (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorDissolve = 0;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorDissolve = 0;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveMode = 
					BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutDissolveMode = false;
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveVariance = 0;
				RefreshStyleBValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		*/
		/*
		void InitializeSproutStyleCrown () {
			// SPROUT B.
			// Saturation Range.
			sproutCrownSaturationElem = container.Q<MinMaxSlider> (sproutCrownSaturationName);
			sproutCrownSaturationElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorSaturation = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorSaturation = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutCrownSaturationElem);

			// Saturation Mode.
			sproutCrownSaturationModeElem = container.Q<EnumField> (sproutCrownSaturationModeName);
			sproutCrownSaturationModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform);
			sproutCrownSaturationModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationMode = (BranchDescriptorCollection.SproutStyle.SproutSaturationMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
						sproutCrownSaturationModeInvertElem.style.display = DisplayStyle.None;
						sproutCrownSaturationVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutCrownSaturationModeInvertElem.style.display = DisplayStyle.Flex;
						sproutCrownSaturationVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Invert.
			sproutCrownSaturationModeInvertElem = container.Q<Toggle> (sproutCrownSaturationModeInvertName);
			sproutCrownSaturationModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutSaturationMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Variance.
			sproutCrownSaturationVarianceElem = container.Q<Slider> (sproutCrownSaturationVarianceName);
			sproutCrownSaturationVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownSaturationVarianceElem);

			// Shade Range
			sproutCrownShadeElem = container.Q<MinMaxSlider> (sproutCrownShadeName);
			sproutCrownShadeElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorShade = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorShade = newVal.y;
					OnEdit (true);
					UpdateShade (2);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutCrownShadeElem);

			// Shade Mode.
			sproutCrownShadeModeElem = container.Q<EnumField> (sproutCrownShadeModeName);
			sproutCrownShadeModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform);
			sproutCrownShadeModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutShadeMode = (BranchDescriptorCollection.SproutStyle.SproutShadeMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutShadeMode == BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform) {
						sproutCrownShadeModeInvertElem.style.display = DisplayStyle.None;
						sproutCrownShadeVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutCrownShadeModeInvertElem.style.display = DisplayStyle.Flex;
						sproutCrownShadeVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (true);
					UpdateShade (2);
				}
			});

			// Shade Invert.
			sproutCrownShadeModeInvertElem = container.Q<Toggle> (sproutCrownShadeModeInvertName);
			sproutCrownShadeModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutShadeMode = evt.newValue;
					OnEdit (true);
					UpdateShade (2);
				}
			});

			// Shade Variance.
			sproutCrownShadeVarianceElem = container.Q<Slider> (sproutCrownShadeVarianceName);
			sproutCrownShadeVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutShadeVariance = evt.newValue;
					OnEdit (true);
					UpdateShade (2);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownShadeVarianceElem);

			// Dissolve Range
			sproutCrownDissolveElem = container.Q<MinMaxSlider> (sproutCrownDissolveName);
			sproutCrownDissolveElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorDissolve = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorDissolve = newVal.y;
					OnEdit (true);
					UpdateDissolve (0);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutCrownDissolveElem);

			// Dissolve Mode.
			sproutCrownDissolveModeElem = container.Q<EnumField> (sproutCrownDissolveModeName);
			sproutCrownDissolveModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform);
			sproutCrownDissolveModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveMode = (BranchDescriptorCollection.SproutStyle.SproutDissolveMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveMode == BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform) {
						sproutCrownDissolveModeInvertElem.style.display = DisplayStyle.None;
						sproutCrownDissolveVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutCrownDissolveModeInvertElem.style.display = DisplayStyle.Flex;
						sproutCrownDissolveVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (true);
					UpdateDissolve (0);
				}
			});

			// Dissolve Invert.
			sproutCrownDissolveModeInvertElem = container.Q<Toggle> (sproutCrownDissolveModeInvertName);
			sproutCrownDissolveModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutDissolveMode = evt.newValue;
					OnEdit (true);
					UpdateDissolve (0);
				}
			});

			// Dissolve Variance.
			sproutCrownDissolveVarianceElem = container.Q<Slider> (sproutCrownDissolveVarianceName);
			sproutCrownDissolveVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveVariance = evt.newValue;
					OnEdit (true);
					UpdateDissolve (0);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownDissolveVarianceElem);

			// Tint Color.
			sproutCrownTintColorElem = container.Q<ColorField> (sproutCrownTintColorName);
			sproutCrownTintColorElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.colorTint = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Range.
			sproutCrownTintElem = container.Q<MinMaxSlider> (sproutCrownTintName);
			sproutCrownTintElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorTint = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorTint = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutCrownTintElem);

			// Tint Mode.
			sproutCrownTintModeElem = container.Q<EnumField> (sproutCrownTintModeName);
			sproutCrownTintModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform);
			sproutCrownTintModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintMode = (BranchDescriptorCollection.SproutStyle.SproutTintMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
						sproutCrownTintModeInvertElem.style.display = DisplayStyle.None;
						sproutCrownTintVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutCrownTintModeInvertElem.style.display = DisplayStyle.Flex;
						sproutCrownTintVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Invert.
			sproutCrownTintModeInvertElem = container.Q<Toggle> (sproutCrownTintModeInvertName);
			sproutCrownTintModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutTintMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Variance.
			sproutCrownTintVarianceElem = container.Q<Slider> (sproutCrownTintVarianceName);
			sproutCrownTintVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownTintVarianceElem);

			// Metallic Slider.
			sproutCrownMetallicElem = container.Q<Slider> (sproutCrownMetallicName);
			sproutCrownMetallicElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.metallic = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownMetallicElem);

			// Glossiness Slider.
			sproutCrownGlossinessElem = container.Q<Slider> (sproutCrownGlossinessName);
			sproutCrownGlossinessElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.glossiness = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownGlossinessElem);

			// Subsurface Slider.
			sproutCrownSubsurfaceElem = container.Q<Slider> (sproutCrownSubsurfaceName);
			sproutCrownSubsurfaceElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.subsurface = newVal;
					OnEdit (true, true, false, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownSubsurfaceElem);

			// Saturation Reset Button.
			sproutCrownSaturationResetElem = container.Q<Button> (sproutCrownSaturationResetName);
			sproutCrownSaturationResetElem?.UnregisterCallback<ClickEvent> (ResetSproutCrownSaturation);
			sproutCrownSaturationResetElem?.RegisterCallback<ClickEvent> (ResetSproutCrownSaturation);
			// Shade Reset Button.
			sproutCrownShadeResetElem = container.Q<Button> (sproutCrownShadeResetName);
			sproutCrownShadeResetElem?.UnregisterCallback<ClickEvent> (ResetSproutCrownShade);
			sproutCrownShadeResetElem?.RegisterCallback<ClickEvent> (ResetSproutCrownShade);
			// Tint Reset Button.
			sproutCrownTintResetElem = container.Q<Button> (sproutCrownTintResetName);
			sproutCrownTintResetElem?.UnregisterCallback<ClickEvent> (ResetSproutCrownTint);
			sproutCrownTintResetElem?.RegisterCallback<ClickEvent> (ResetSproutCrownTint);
			// Dissolve Reset Button.
			sproutCrownDissolveResetElem = container.Q<Button> (sproutCrownDissolveResetName);
			sproutCrownDissolveResetElem?.UnregisterCallback<ClickEvent> (ResetSproutCrownDissolve);
			sproutCrownDissolveResetElem?.RegisterCallback<ClickEvent> (ResetSproutCrownDissolve);
		}
		void ResetSproutCrownSaturation (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorSaturation = 1;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorSaturation = 1;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationMode = 
					BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutSaturationMode = false;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationVariance = 0;
				RefreshStyleCrownValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		void ResetSproutCrownShade (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorShade = 1;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorShade = 1;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutShadeMode = 
					BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutShadeMode = false;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutShadeVariance = 0;
				RefreshStyleCrownValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		void ResetSproutCrownTint (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorTint = 0;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorTint = 0;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintMode = 
					BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutTintMode = false;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintVariance = 0;
				RefreshStyleCrownValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		void ResetSproutCrownDissolve (ClickEvent evt) {
			if (listenGUIEvents) {
				OnBeforeEdit ();
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorDissolve = 0;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorDissolve = 0;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveMode = 
					BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutDissolveMode = false;
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveVariance = 0;
				RefreshStyleCrownValues ();
				OnEdit (true, true, false, false, true);
			}
		}
		*/
		private void OnBeforeEdit () {
			sproutLabEditor.onBeforeEditBranchDescriptor?.Invoke (
			sproutLabEditor.selectedSnapshot, sproutLabEditor.branchDescriptorCollection);
		}
		private void OnEdit (
			bool updatePipeline = false, 
			bool updateCompositeMaterials = false, 
			bool updateAlbedoMaterials = false,
			bool updateExtrasMaterials = false,
			bool updateSubsurfaceMaterials = false)
		{
			// If a LOD is selected, return to geometry view.
			if (sproutLabEditor.selectedLODView != 0) {
				sproutLabEditor.ShowPreviewMesh ();
			}
			
			sproutLabEditor.onEditBranchDescriptor?.Invoke (
				sproutLabEditor.selectedSnapshot, sproutLabEditor.branchDescriptorCollection);
			if (updatePipeline) {
				sproutLabEditor.ReflectChangesToPipeline ();
			}
			if (updateCompositeMaterials) {
				UpdateCompositeMaterials ();
			}
			if (updateAlbedoMaterials) {
				UpdateAlbedoMaterials ();
			}
			if (updateExtrasMaterials) {
				UpdateExtrasMaterials ();
			}
			if (updateSubsurfaceMaterials) {
				UpdateSubsurfaceMaterials ();
			}
			sproutLabEditor.sproutSubfactory.sproutCompositeManager.Clear ();
		}
		/// <summary>
		/// Updates the shade of sprout meshes using a sprout style.
		/// </summary>
		/// <param name="sproutStyleIndex">Sprout style index (0 = A, 1 = B, 2 = Crown).</param>
		void UpdateShade (int sproutStyleIndex) {
			BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
			BranchDescriptorCollection.SproutStyle sproutStyle = sproutLabEditor.branchDescriptorCollection.sproutStyles [sproutStyleIndex];

			int subMeshIndex;
			int subMeshCount;
			float minShade;
			float maxShade;
			BranchDescriptorCollection.SproutStyle.SproutShadeMode sproutShadeMode;
			bool invertSproutShadeMode;
			float sproutShadeVariance;
			foreach (BranchDescriptor.SproutStructure sproutStructure in snapshot.sproutStructures)
			{
				if (sproutStructure.styleId == sproutStyle.id) {
					subMeshIndex = sproutStructure.submeshIndex;
					subMeshCount = sproutStructure.submeshCount;
					minShade = sproutStyle.minColorShade;
					maxShade = sproutStyle.maxColorShade;
					sproutShadeMode = sproutStyle.sproutShadeMode;
					invertSproutShadeMode = sproutStyle.invertSproutShadeMode;
					sproutShadeVariance = sproutStyle.sproutShadeVariance;
					for (int i = subMeshIndex; i < subMeshIndex + subMeshCount; i++) {
						Broccoli.Component.SproutMapperComponent.UpdateShadeVariance (
						sproutLabEditor.sproutSubfactory.snapshotTreeMesh,
						minShade, maxShade, sproutShadeMode, invertSproutShadeMode, sproutShadeVariance, i);
					}
				}	
			}
		}
		/// <summary>
		/// Updates the dissolve of sprout meshes using a sprout style.
		/// </summary>
		/// <param name="sproutStyleIndex">Sprout style index (0 = A, 1 = B, 2 = Crown).</param>
		void UpdateDissolve (int sproutStyleIndex) {
			BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
			BranchDescriptorCollection.SproutStyle sproutStyle = sproutLabEditor.branchDescriptorCollection.sproutStyles [sproutStyleIndex];

			int subMeshIndex;
			int subMeshCount;
			float minDissolve;
			float maxDissolve;
			BranchDescriptorCollection.SproutStyle.SproutDissolveMode sproutDissolveMode;
			bool invertSproutDissolveMode;
			float sproutDissolveVariance;
			foreach (BranchDescriptor.SproutStructure sproutStructure in snapshot.sproutStructures)
			{
				if (sproutStructure.styleId == sproutStyle.id) {
					subMeshIndex = sproutStructure.submeshIndex;
					subMeshCount = sproutStructure.submeshCount;
					minDissolve = sproutStyle.minColorDissolve;
					maxDissolve = sproutStyle.maxColorDissolve;
					sproutDissolveMode = sproutStyle.sproutDissolveMode;
					invertSproutDissolveMode = sproutStyle.invertSproutDissolveMode;
					sproutDissolveVariance = sproutStyle.sproutDissolveVariance;
					for (int i = subMeshIndex; i < subMeshIndex + subMeshCount; i++) {
						Broccoli.Component.SproutMapperComponent.UpdateDissolveVariance (
						sproutLabEditor.sproutSubfactory.snapshotTreeMesh,
						minDissolve, maxDissolve, sproutDissolveMode, invertSproutDissolveMode, sproutDissolveVariance, i);
					}
				}	
			}
			/*
			int subMeshIndex;
			int subMeshCount;
			float minDissolve;
			float maxDissolve;
			BranchDescriptorCollection.SproutStyle.SproutDissolveMode sproutDissolveMode;
			bool invertSproutDissolveMode;
			float sproutDissolveVariance;
			BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
			if (styleIndex == 0) {
				subMeshIndex = snapshot.sproutASubmeshIndex;
				subMeshCount = snapshot.sproutASubmeshCount;
				minDissolve = sproutLabEditor.branchDescriptorCollection.sproutStyleA.minColorDissolve;
				maxDissolve = sproutLabEditor.branchDescriptorCollection.sproutStyleA.maxColorDissolve;
				sproutDissolveMode = sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutDissolveMode;
				invertSproutDissolveMode = sproutLabEditor.branchDescriptorCollection.sproutStyleA.invertSproutDissolveMode;
				sproutDissolveVariance = sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutDissolveVariance;
			} else if (styleIndex == 1) {
				subMeshIndex = snapshot.sproutBSubmeshIndex;
				subMeshCount = snapshot.sproutBSubmeshCount;
				minDissolve = sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorDissolve;
				maxDissolve = sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorDissolve;
				sproutDissolveMode = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveMode;
				invertSproutDissolveMode = sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutDissolveMode;
				sproutDissolveVariance = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveVariance;
			} else {
				subMeshIndex = snapshot.sproutCrownSubmeshIndex;
				subMeshCount = snapshot.sproutCrownSubmeshCount;
				minDissolve = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorDissolve;
				maxDissolve = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorDissolve;
				sproutDissolveMode = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveMode;
				invertSproutDissolveMode = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutDissolveMode;
				sproutDissolveVariance = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveVariance;
			}
			for (int i = subMeshIndex; i < subMeshIndex + subMeshCount; i++) {
				Broccoli.Component.SproutMapperComponent.UpdateDissolveVariance (
				sproutLabEditor.sproutSubfactory.snapshotTreeMesh,
				minDissolve, maxDissolve, sproutDissolveMode, invertSproutDissolveMode, sproutDissolveVariance, i);
			}
			*/
			// TODOOOO
		}
		void UpdateCompositeMaterials () {
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_COMPOSITE) {
				BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
				sproutLabEditor.sproutSubfactory.UpdateCompositeMaterials (snapshot, sproutLabEditor.currentPreviewMaterials);
			}
			/*
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_COMPOSITE) {
				BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
				sproutLabEditor.sproutSubfactory.UpdateCompositeMaterials (sproutLabEditor.currentPreviewMaterials,
					sproutLabEditor.branchDescriptorCollection.sproutStyleA,
					sproutLabEditor.branchDescriptorCollection.sproutStyleB,
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown,
					snapshot.sproutASubmeshIndex,
					snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
			}
			*/
			// TODOOOOO
		}
		void UpdateAlbedoMaterials () {
			Material[] mats = null;
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_ALBEDO) {
				mats = sproutLabEditor.currentPreviewMaterials;
			} else if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_COMPOSITE) {
				mats = sproutLabEditor.meshPreview.secondPassMaterials;
			}
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_ALBEDO || sproutLabEditor.currentMapView == SproutLabEditor.VIEW_COMPOSITE) {
				BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
				sproutLabEditor.sproutSubfactory.UpdateAlbedoMaterials (
					snapshot, 
					mats,
					sproutLabEditor.branchDescriptorCollection.branchColorShade,
					sproutLabEditor.branchDescriptorCollection.branchColorSaturation);
			}
		}
		void UpdateExtrasMaterials () {
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_EXTRAS) {
				BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
				sproutLabEditor.sproutSubfactory.UpdateExtrasMaterials (
					snapshot,
					sproutLabEditor.currentPreviewMaterials);
			}
		}
		void UpdateSubsurfaceMaterials () {
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_SUBSURFACE) {
				BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
				sproutLabEditor.sproutSubfactory.UpdateSubsurfaceMaterials (
					snapshot,
					sproutLabEditor.currentPreviewMaterials,
					sproutLabEditor.branchDescriptorCollection.branchColorSaturation,
					sproutLabEditor.branchDescriptorCollection.branchSubsurface);
			}
		}
		public void RefreshValues () {
			if (sproutLabEditor.branchDescriptorCollection != null) {
				listenGUIEvents = false;

				RefreshStyleValues ();

				listenGUIEvents = true;
			}
		}
		private void RefreshStyleValues () {
			if (sproutLabEditor.selectedSproutStyle == null) return;

			// SATURATION SPROUT A
			sproutSaturationElem.value = new Vector2 (
				sproutLabEditor.selectedSproutStyle.minColorSaturation,
				sproutLabEditor.selectedSproutStyle.maxColorSaturation);
			SproutLabEditor.RefreshMinMaxSlider (sproutSaturationElem, sproutSaturationElem.value);
			sproutSaturationModeElem.value = sproutLabEditor.selectedSproutStyle.sproutSaturationMode;
			if (sproutLabEditor.selectedSproutStyle.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
				sproutSaturationModeInvertElem.style.display = DisplayStyle.None;
				sproutSaturationVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutSaturationModeInvertElem.style.display = DisplayStyle.Flex;
				sproutSaturationVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutSaturationModeInvertElem.value = sproutLabEditor.selectedSproutStyle.invertSproutSaturationMode;
			sproutSaturationVarianceElem.value = sproutLabEditor.selectedSproutStyle.sproutSaturationVariance;
			// TINT SPROUT A
			sproutTintColorElem.value = sproutLabEditor.selectedSproutStyle.colorTint;
			sproutTintElem.value = new Vector2 (
				sproutLabEditor.selectedSproutStyle.minColorTint,
				sproutLabEditor.selectedSproutStyle.maxColorTint);
			SproutLabEditor.RefreshMinMaxSlider (sproutTintElem, sproutTintElem.value);
			sproutTintModeElem.value = sproutLabEditor.selectedSproutStyle.sproutTintMode;
			if (sproutLabEditor.selectedSproutStyle.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
				sproutTintModeInvertElem.style.display = DisplayStyle.None;
				sproutTintVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutTintModeInvertElem.style.display = DisplayStyle.Flex;
				sproutTintVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutTintModeInvertElem.value = sproutLabEditor.selectedSproutStyle.invertSproutTintMode;
			sproutTintVarianceElem.value = sproutLabEditor.selectedSproutStyle.sproutTintVariance;
			// SHADE A
			sproutShadeElem.value = new Vector2 (
				sproutLabEditor.selectedSproutStyle.minColorShade,
				sproutLabEditor.selectedSproutStyle.maxColorShade);
			SproutLabEditor.RefreshMinMaxSlider (sproutShadeElem, sproutShadeElem.value);
			sproutShadeModeElem.value = sproutLabEditor.selectedSproutStyle.sproutShadeMode;
			if (sproutLabEditor.selectedSproutStyle.sproutShadeMode == BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform) {
				sproutShadeModeInvertElem.style.display = DisplayStyle.None;
				sproutShadeVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutShadeModeInvertElem.style.display = DisplayStyle.Flex;
				sproutShadeVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutShadeModeInvertElem.value = sproutLabEditor.selectedSproutStyle.invertSproutShadeMode;
			sproutShadeVarianceElem.value = sproutLabEditor.selectedSproutStyle.sproutShadeVariance;
			// DISSOLVE
			sproutDissolveElem.value = new Vector2 (
				sproutLabEditor.selectedSproutStyle.minColorDissolve,
				sproutLabEditor.selectedSproutStyle.maxColorDissolve);
			SproutLabEditor.RefreshMinMaxSlider (sproutDissolveElem, sproutDissolveElem.value);
			sproutDissolveModeElem.value = sproutLabEditor.selectedSproutStyle.sproutDissolveMode;
			if (sproutLabEditor.selectedSproutStyle.sproutDissolveMode == BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform) {
				sproutDissolveModeInvertElem.style.display = DisplayStyle.None;
				sproutDissolveVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutDissolveModeInvertElem.style.display = DisplayStyle.Flex;
				sproutDissolveVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutDissolveModeInvertElem.value = sproutLabEditor.selectedSproutStyle.invertSproutDissolveMode;
			sproutDissolveVarianceElem.value = sproutLabEditor.selectedSproutStyle.sproutDissolveVariance;
			// METALLIC, GLOSSINESS, SUBSURFACE SPROUT A
			sproutMetallicElem.value = sproutLabEditor.selectedSproutStyle.metallic;
			SproutLabEditor.RefreshSlider (sproutMetallicElem, sproutLabEditor.selectedSproutStyle.metallic);
			sproutGlossinessElem.value = sproutLabEditor.selectedSproutStyle.glossiness;
			SproutLabEditor.RefreshSlider (sproutGlossinessElem, sproutLabEditor.selectedSproutStyle.glossiness);
			sproutSubsurfaceElem.value = sproutLabEditor.selectedSproutStyle.subsurface;
			SproutLabEditor.RefreshSlider (sproutSubsurfaceElem, sproutLabEditor.selectedSproutStyle.subsurface);
		}
		/*
		private void RefreshStyleBValues () {
			// SATURATION B
			sproutBSaturationElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorSaturation,
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorSaturation);
			SproutLabEditor.RefreshMinMaxSlider (sproutBSaturationElem, sproutBSaturationElem.value);
			sproutBSaturationModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
				sproutBSaturationModeInvertElem.style.display = DisplayStyle.None;
				sproutBSaturationVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutBSaturationModeInvertElem.style.display = DisplayStyle.Flex;
				sproutBSaturationVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutBSaturationModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutSaturationMode;
			sproutBSaturationVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationVariance;
			// TINT B
			sproutBTintColorElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.colorTint;
			sproutBTintElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorTint,
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorTint);
			SproutLabEditor.RefreshMinMaxSlider (sproutBTintElem, sproutBTintElem.value);
			sproutBTintModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
				sproutBTintModeInvertElem.style.display = DisplayStyle.None;
				sproutBTintVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutBTintModeInvertElem.style.display = DisplayStyle.Flex;
				sproutBTintVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutBTintModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutTintMode;
			sproutBTintVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintVariance;
			// SHADE B
			sproutBShadeElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorShade,
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorShade);
			SproutLabEditor.RefreshMinMaxSlider (sproutBShadeElem, sproutBShadeElem.value);
			sproutBShadeModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutShadeMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutShadeMode == BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform) {
				sproutBShadeModeInvertElem.style.display = DisplayStyle.None;
				sproutBShadeVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutBShadeModeInvertElem.style.display = DisplayStyle.Flex;
				sproutBShadeVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutBShadeModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutShadeMode;
			sproutBShadeVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutShadeVariance;
			// DISSOLVE B
			sproutBDissolveElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorDissolve,
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorDissolve);
			SproutLabEditor.RefreshMinMaxSlider (sproutBDissolveElem, sproutBDissolveElem.value);
			sproutBDissolveModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveMode == BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform) {
				sproutBDissolveModeInvertElem.style.display = DisplayStyle.None;
				sproutBDissolveVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutBDissolveModeInvertElem.style.display = DisplayStyle.Flex;
				sproutBDissolveVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutBDissolveModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutDissolveMode;
			sproutBDissolveVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutDissolveVariance;
			// METALLIC, GLOSSINESS, SUBSURFACE SPROUT A
			sproutBMetallicElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.metallic;
			SproutLabEditor.RefreshSlider (sproutBMetallicElem, sproutLabEditor.branchDescriptorCollection.sproutStyleB.metallic);
			sproutBGlossinessElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.glossiness;
			SproutLabEditor.RefreshSlider (sproutBGlossinessElem, sproutLabEditor.branchDescriptorCollection.sproutStyleB.glossiness);
			sproutBSubsurfaceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.subsurface;
			SproutLabEditor.RefreshSlider (sproutBSubsurfaceElem, sproutLabEditor.branchDescriptorCollection.sproutStyleB.subsurface);
		}
		private void RefreshStyleCrownValues () {
			// SATURATION SPROUT CROWN
			sproutCrownSaturationElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorSaturation,
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorSaturation);
			SproutLabEditor.RefreshMinMaxSlider (sproutCrownSaturationElem, sproutCrownSaturationElem.value);
			sproutCrownSaturationModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
				sproutCrownSaturationModeInvertElem.style.display = DisplayStyle.None;
				sproutCrownSaturationVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutCrownSaturationModeInvertElem.style.display = DisplayStyle.Flex;
				sproutCrownSaturationVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutCrownSaturationModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutSaturationMode;
			sproutCrownSaturationVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationVariance;
			// TINT SPROUT A
			sproutCrownTintColorElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.colorTint;
			sproutCrownTintElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorTint,
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorTint);
			SproutLabEditor.RefreshMinMaxSlider (sproutCrownTintElem, sproutCrownTintElem.value);
			sproutCrownTintModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
				sproutCrownTintModeInvertElem.style.display = DisplayStyle.None;
				sproutCrownTintVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutCrownTintModeInvertElem.style.display = DisplayStyle.Flex;
				sproutCrownTintVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutCrownTintModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutTintMode;
			sproutCrownTintVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintVariance;
			// SHADE A
			sproutCrownShadeElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorShade,
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorShade);
			SproutLabEditor.RefreshMinMaxSlider (sproutCrownShadeElem, sproutCrownShadeElem.value);
			sproutCrownShadeModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutShadeMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutShadeMode == BranchDescriptorCollection.SproutStyle.SproutShadeMode.Uniform) {
				sproutCrownShadeModeInvertElem.style.display = DisplayStyle.None;
				sproutCrownShadeVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutCrownShadeModeInvertElem.style.display = DisplayStyle.Flex;
				sproutCrownShadeVarianceElem.style.display = DisplayStyle.Flex; 
			}
			sproutCrownShadeModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutShadeMode;
			sproutCrownShadeVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutShadeVariance;
			// DISSOLVE
			sproutCrownDissolveElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorDissolve,
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorDissolve);
			SproutLabEditor.RefreshMinMaxSlider (sproutCrownDissolveElem, sproutCrownDissolveElem.value);
			sproutCrownDissolveModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveMode == BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Uniform) {
				sproutCrownDissolveModeInvertElem.style.display = DisplayStyle.None;
				sproutCrownDissolveVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutCrownDissolveModeInvertElem.style.display = DisplayStyle.Flex;
				sproutCrownDissolveVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutCrownDissolveModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutDissolveMode;
			sproutCrownDissolveVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutDissolveVariance;
			// METALLIC, GLOSSINESS, SUBSURFACE SPROUT A
			sproutCrownMetallicElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.metallic;
			SproutLabEditor.RefreshSlider (sproutCrownMetallicElem, sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.metallic);
			sproutCrownGlossinessElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.glossiness;
			SproutLabEditor.RefreshSlider (sproutCrownGlossinessElem, sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.glossiness);
			sproutCrownSubsurfaceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.subsurface;
			SproutLabEditor.RefreshSlider (sproutCrownSubsurfaceElem, sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.subsurface);
		}
		*/
		public void Attach () {
			if (!this.sproutLabEditor.rootVisualElement.Contains (container)) {
				this.sproutLabEditor.rootVisualElement.Add (container);
			}
		}
		public void Detach () {
			if (this.sproutLabEditor.rootVisualElement.Contains (container)) {
				this.sproutLabEditor.rootVisualElement.Remove (container);
			}
		}
		/// <summary>
        /// Called when the GUI textures are loaded.
        /// </summary>
        public void OnGUITexturesLoaded () {
			if (sproutContainerElem != null)
				SetContainerIcons (sproutContainerElem);
			if (sproutBContainerElem != null)
				SetContainerIcons (sproutBContainerElem);
			if (sproutCrownContainerElem != null)
				SetContainerIcons (sproutCrownContainerElem);
		}
		void SetContainerIcons (VisualElement container) {
			VisualElement iconSaturationElem = container.Q<VisualElement> (iconSaturationName);
			if (iconSaturationElem != null ) 
				iconSaturationElem.style.backgroundImage = new StyleBackground (GUITextureManager.IconSaturation);
			VisualElement iconShadeElem = container.Q<VisualElement> (iconShadeName);
			if (iconShadeElem != null ) 
				iconShadeElem.style.backgroundImage = new StyleBackground (GUITextureManager.IconShade);
			VisualElement iconTintElem = container.Q<VisualElement> (iconTintName);
			if (iconTintElem != null ) 
				iconTintElem.style.backgroundImage = new StyleBackground (GUITextureManager.IconTint);
			VisualElement iconSurfaceElem = container.Q<VisualElement> (iconSurfaceName);
			if (iconSurfaceElem != null ) 
				iconSurfaceElem.style.backgroundImage = new StyleBackground (GUITextureManager.IconSurface);
			if (Broccoli.Base.GlobalSettings.experimentalSproutLabDissolveSprouts) {
				VisualElement iconDissolveElem = container.Q<VisualElement> (iconDissolveName);
				if (iconDissolveElem != null ) 
					iconDissolveElem.style.backgroundImage = new StyleBackground (GUITextureManager.IconDissolve);
			}
		}
		#endregion

		#region Side Panel 
		public void Repaint () {
			RefreshValues ();
			requiresRepaint = false;
		}
		public void OnUndoRedo () {
			RefreshValues ();
			//LoadSidePanelFields (selectedVariationGroup);
		}
        #endregion

        #region Draw
        public void SetVisible (bool visible) {
            if (visible) {
                container.style.display = DisplayStyle.Flex;
            } else {
                container.style.display = DisplayStyle.None;
            }
        }
        /// <summary>
        /// Sets the draw area for the components.
        /// </summary>
        /// <param name="refRect">Rect to draw the componentes.</param>
        public void SetRect (Rect refRect) {
            if (Event.current.type != EventType.Repaint) return;
            rect = refRect;
            container.style.marginTop = refRect.y;
            container.style.height = refRect.height;
        }
        #endregion
    }
}
