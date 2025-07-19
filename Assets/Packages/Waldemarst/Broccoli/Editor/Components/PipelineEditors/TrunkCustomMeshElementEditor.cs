using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using Broccoli.Model; // For TrunkMeshDescriptor, TrunkDef, BezierCurve, BezierNode
using Broccoli.Pipe;  // For TrunkCustomMeshElement
using Broccoli.Utils; // For BezierCurveEditor
using Broccoli.Base;
using Broccoli.Component;
using Broccoli.BroccoEditor; // For GlobalSettings

namespace Broccoli.TreeNodeEditor
{
    [CustomEditor(typeof(TrunkCustomMeshElement))]
    public class TrunkCustomMeshElementEditor : PipelineElementEditor
    {
        #region Vars
        private TrunkCustomMeshElement trunkCustomMeshElement;
        private SerializedProperty _trunkMeshesProp;
        private ReorderableList _descriptorList;
        private ReorderableList _trunkDefList;

        private int _selectedDescriptorIndex = -1;
        private int _selectedTrunkDefIndex = -1;
        private TrunkMeshDescriptor _selectedTrunkMeshDesc = null;

        private bool _showAdvancedTexProps = false;

        private BezierCurveEditor _bezierCurveEditor;
        private bool _isCurveEditorInitialized = false;

        private UnityEditor.PreviewRenderUtility _previewRenderUtility;
        private Vector2 _previewDir = new Vector2(120f, -20f);
        private int _selectedLODPreviewIndex = 0;
        /// <summary>
        /// Changes requiring object serialization.
        /// </summary>
        bool serializeChange = false;
        /// <summary>
        /// Changes must be reflected to the trunk object container.
        /// </summary>
        bool localChange = false;
        /// <summary>
        /// Changes require the whole tree to be rebuild.
        /// </summary>
        bool rebuildTreeChange = false;
        /// <summary>
        /// Cylinder hander to modify TrunkDef adjustment area.
        /// </summary>
        CylinderHandle _cylinderHandle = new CylinderHandle();
        /// <summary>
        /// Temporary cylinder handle height.
        /// </summary>
        float _tmpCylHeight = 0.5f;
        /// <summary>
        /// Temporary cylinder handle radius.
        /// </summary>
        float _tmpCylRadius = 0.1f;
        bool _trunkDefTransitionChanged = false;
        #endregion

        #region GUIContent Definitions
        private static readonly GUIContent GC_DescriptorListHeader = new GUIContent("Trunk Mesh Descriptors", "List of defined mesh configurations to be used as trunks.");
        private static readonly GUIContent GC_EditingDescriptorHeader = new GUIContent("Editing Trunk {0}");
        private static readonly GUIContent GC_Enabled = new GUIContent("Enabled", "If checked, this trunk mesh descriptor will be considered for use.");
        private static readonly GUIContent GC_GameObject = new GUIContent("Game Object", "The main GameObject or Prefab to use for this trunk. Mesh and Material will be extracted from it.");
        private static readonly GUIContent GC_HasLOD = new GUIContent("Has LODs", "Check to enable Level of Detail (LOD) GameObjects.");
        private static readonly GUIContent GC_GameObjectLOD1 = new GUIContent("GameObject LOD1", "The GameObject or Prefab for Level of Detail 1.");
        private static readonly GUIContent GC_GameObjectLOD2 = new GUIContent("GameObject LOD2", "The GameObject or Prefab for Level of Detail 2.");
        private static readonly GUIContent GC_Position = new GUIContent("Position Offset", "Local position offset to apply to the mesh.");
        private static readonly GUIContent GC_Rotation = new GUIContent("Rotation Offset", "Local rotation offset (Euler angles) to apply to the mesh.");
        private static readonly GUIContent GC_Scale = new GUIContent("Scale Multiplier", "Local scale multiplier to apply to the mesh.");
        private static readonly GUIContent GC_AdvancedTexFoldout = new GUIContent("Advanced Texture Properties", "Settings for albedo, normal, and extra texture property names in the material.");
        private static readonly GUIContent GC_AlbedoTexProp = new GUIContent("Albedo Property Name", "The name of the main albedo texture property in the material's shader (e.g., _MainTex, _BaseMap).");
        private static readonly GUIContent GC_HasNormalTex = new GUIContent("Use Normal Texture", "Should a normal map be expected/used from the material?");
        private static readonly GUIContent GC_NormalTexProp = new GUIContent("Normal Property Name", "The name of the normal map texture property in the material's shader (e.g., _BumpMap, _NormalMap).");
        private static readonly GUIContent GC_HasExtraTex = new GUIContent("Use Extra Texture", "Should an additional custom texture be expected/used from the material?");
        private static readonly GUIContent GC_ExtraTexProp = new GUIContent("Extra Property Name", "The name of the extra texture property in the material's shader (e.g., _OcclusionMap, _EmissionMap, _DetailMap).");
        private static readonly GUIContent GC_TrunkDefListHeader = new GUIContent("Trunk Definitions", "List of definitions for the selected Trunk Mesh Descriptor.");
        private static readonly GUIContent GC_EditingTrunkDefHeader = new GUIContent("Editing Definition {0}");
        private static readonly GUIContent GC_CurveEditorSettings = new GUIContent("Curve Editor Settings", "Settings for the Bezier Curve appearance and behavior.");
        private static readonly GUIContent GC_GirthScale = new GUIContent("Transition Length", "The girth for the branch commint for this trunk definition.");
        private static readonly GUIContent GC_TransitionLength = new GUIContent("Transition Length", "The length of the transition zone for this trunk definition.");
        private static readonly GUIContent GC_TransitionRadius = new GUIContent("Transition Radius", "The radius at the transition point for this trunk definition.");
        #endregion

        // Property names
        private const string TrunkMeshesPropName = "trunkMeshes";
        private const string EnabledPropName = "enabled";
        private const string GameObjectPropName = "_gameObject";
        private const string HasLODPropName = "hasLOD";
        private const string GameObjectLOD1PropName = "_gameObjectLOD1";
        private const string GameObjectLOD2PropName = "_gameObjectLOD2";
        private const string PositionPropName = "position";
        private const string RotationPropName = "rotation";
        private const string ScalePropName = "scale";
        private const string AlbedoTexPropName = "albedoTexProp";
        private const string HasNormalTexPropName = "hasNormalTex";
        private const string NormalTexPropName = "normalTexProp";
        private const string HasExtraTexPropName = "hasExtraTex";
        private const string ExtraTexPropName = "extraTexProp";
        private const string TrunkDefsPropName = "trunkDefs";
        private const string CurvePropName = "curve";
        private const string GirthScalePropName = "girthScale";
        private const string TransitionLengthPropName = "transitionLength";
        private const string TransitionRadiusPropName = "transitionRadius";

        protected override void OnEnableSpecific ()
        {
            trunkCustomMeshElement = (TrunkCustomMeshElement)target;
            _trunkMeshesProp = serializedObject.FindProperty(TrunkMeshesPropName);

            if (_trunkMeshesProp == null) return;
            SetupDescriptorList();
            InitializeCurveEditor();

            if (_previewRenderUtility == null)
            {
                _previewRenderUtility = new UnityEditor.PreviewRenderUtility();
                _previewRenderUtility.camera.fieldOfView = 30;
                _previewRenderUtility.camera.transform.position = new Vector3(0, 0, -5);
                _previewRenderUtility.camera.transform.rotation = Quaternion.identity;
                _previewRenderUtility.camera.nearClipPlane = 0.1f;
                _previewRenderUtility.camera.farClipPlane = 100;
                _previewRenderUtility.lights[0].intensity = 1.4f;
                _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
                _previewRenderUtility.lights[1].intensity = 1.4f;
            }
        }

        private void OnDisable()
        {
            if (_bezierCurveEditor != null && _isCurveEditorInitialized)
            {
                _bezierCurveEditor.onEditModeChanged -= HandleCurveEditModeChanged;
                _bezierCurveEditor.onSelectionChanged -= HandleCurveNodeSelectionChanged;
                _bezierCurveEditor.onCheckDragNodes -= HandleCurveCheckDragNodes;
                _bezierCurveEditor.onBeginDragNodes -= HandleCurveBeginDragNodes;
                _bezierCurveEditor.onBeforeDragNodes -= HandleCurveBeforeDragNodes;
                _bezierCurveEditor.onDragNodes -= HandleCurveDragNodes;
                _bezierCurveEditor.onEndDragNodes -= HandleCurveEndDragNodes;
                _bezierCurveEditor.onBeforeEditNodeStyle -= HandleCurveBeforeEditNodeStyle;
                _bezierCurveEditor.onEditNodeStyle -= HandleCurveEditNodeStyle;
                _bezierCurveEditor.onBeforeAddNode -= HandleCurveBeforeAddNode;
                _bezierCurveEditor.onAddNode -= HandleCurveAddNode;
                _bezierCurveEditor.onCheckRemoveNodes -= HandleCurveCheckRemoveNodes;
                _bezierCurveEditor.onBeforeRemoveNodes -= HandleCurveBeforeRemoveNodes;
                _bezierCurveEditor.onRemoveNodes -= HandleCurveRemoveNodes;
                _bezierCurveEditor.onBeginDragHandle -= HandleCurveBeginDragHandle;
                _bezierCurveEditor.onDragHandle -= HandleCurveDragHandle;
                _bezierCurveEditor.onEndDragHandle -= HandleCurveEndDragHandle;
                _bezierCurveEditor.onCheckSelectCmd -= HandleCurveCheckSelectCmd;
                _bezierCurveEditor.onSelectNode -= HandleCurveSelectNode;
                _bezierCurveEditor.onDeselectNode -= HandleCurveDeselectNode;
                _bezierCurveEditor.onCheckNodeControls -= HandleCurveCheckNodeControls;
                _bezierCurveEditor.onChangeCurves -= HandleCurvesChanged;

                _bezierCurveEditor.OnDisable();
                _isCurveEditorInitialized = false;
            }

            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.Cleanup();
                _previewRenderUtility = null;
            }
        }

        private void InitializeCurveEditor()
        {
            if (_bezierCurveEditor == null)
            {
                _bezierCurveEditor = new BezierCurveEditor();
            }

            _bezierCurveEditor.OnEnable();
            _bezierCurveEditor.enableOverlayToolbar = true;
            _bezierCurveEditor.rotateEnabled = true;
            _bezierCurveEditor.scaleEnabled = true;
            _bezierCurveEditor.showPivot = true;
            _bezierCurveEditor.autoCurveManagement = false;
            _bezierCurveEditor.curveColor = new Color(0.3f, 0.7f, 1f, 0.9f);
            _bezierCurveEditor.selectedCurveColor = Color.yellow;
            _bezierCurveEditor.nodeColor = _bezierCurveEditor.curveColor;
            _bezierCurveEditor.selectedNodeColor = _bezierCurveEditor.selectedCurveColor;
            _bezierCurveEditor.nodeHandleColor = Color.Lerp(_bezierCurveEditor.curveColor, Color.white, 0.5f);
            _bezierCurveEditor.selectedNodeHandleColor = _bezierCurveEditor.selectedCurveColor;
            _bezierCurveEditor.curveWidth = 2f;
            _bezierCurveEditor.selectedCurveWidth = 2.5f;
            _bezierCurveEditor.nodeSize = 0.07f;
            _bezierCurveEditor.nodeHandleSize = 0.05f;

            _bezierCurveEditor.onEditModeChanged += HandleCurveEditModeChanged;
            _bezierCurveEditor.onSelectionChanged += HandleCurveNodeSelectionChanged;
            _bezierCurveEditor.onCheckDragNodes += HandleCurveCheckDragNodes;
            _bezierCurveEditor.onBeginDragNodes += HandleCurveBeginDragNodes;
            _bezierCurveEditor.onBeforeDragNodes += HandleCurveBeforeDragNodes;
            _bezierCurveEditor.onDragNodes += HandleCurveDragNodes;
            _bezierCurveEditor.onEndDragNodes += HandleCurveEndDragNodes;
            _bezierCurveEditor.onBeforeEditNodeStyle += HandleCurveBeforeEditNodeStyle;
            _bezierCurveEditor.onEditNodeStyle += HandleCurveEditNodeStyle;
            _bezierCurveEditor.onBeforeAddNode += HandleCurveBeforeAddNode;
            _bezierCurveEditor.onAddNode += HandleCurveAddNode;
            _bezierCurveEditor.onCheckRemoveNodes += HandleCurveCheckRemoveNodes;
            _bezierCurveEditor.onBeforeRemoveNodes += HandleCurveBeforeRemoveNodes;
            _bezierCurveEditor.onRemoveNodes += HandleCurveRemoveNodes;
            _bezierCurveEditor.onBeginDragHandle += HandleCurveBeginDragHandle;
            _bezierCurveEditor.onDragHandle += HandleCurveDragHandle;
            _bezierCurveEditor.onEndDragHandle += HandleCurveEndDragHandle;
            _bezierCurveEditor.onCheckSelectCmd += HandleCurveCheckSelectCmd;
            _bezierCurveEditor.onSelectNode += HandleCurveSelectNode;
            _bezierCurveEditor.onDeselectNode += HandleCurveDeselectNode;
            _bezierCurveEditor.onCheckNodeControls += HandleCurveCheckNodeControls;
            _bezierCurveEditor.onChangeCurves += HandleCurvesChanged;

            _isCurveEditorInitialized = true;
        }

        private void SetupDescriptorList()
        {
            _descriptorList = new ReorderableList(serializedObject, _trunkMeshesProp, true, true, true, true)
            {
                drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, GC_DescriptorListHeader),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    float buttonWidth = 60f;
                    Rect labelRect = new Rect(rect.x, rect.y, rect.width - buttonWidth - 5, EditorGUIUtility.singleLineHeight);
                    Rect buttonRect = new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);

                    SerializedProperty elementProp = _descriptorList.serializedProperty.GetArrayElementAtIndex(index);
                    bool isEnabled = elementProp.FindPropertyRelative(EnabledPropName).boolValue;
                    var labelContent = new GUIContent($"Trunk {index}{(isEnabled ? "" : " [disabled]")}");
                    
                    EditorGUI.LabelField(labelRect, labelContent);

                    if (trunkCustomMeshElement.selectedTrunkIndex == index)
                    {
                        // If this is the selected one, show a label
                        EditorGUI.LabelField(buttonRect, "Selected", EditorStyles.centeredGreyMiniLabel);
                    }
                },
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                onSelectCallback = (list) =>
                {
                    if (_selectedDescriptorIndex != list.index) {
                        _selectedDescriptorIndex = list.index;
                        _selectedTrunkMeshDesc = trunkCustomMeshElement.trunkMeshes [_selectedDescriptorIndex];
                        _selectedTrunkDefIndex = -1;
                        _trunkDefList = null;
                        if (_bezierCurveEditor != null) {
                            _bezierCurveEditor.ClearSelection();
                            _bezierCurveEditor.curve = null;
                            _bezierCurveEditor.curveId = System.Guid.Empty;
                        }
                        _selectedLODPreviewIndex = 0;
                        trunkCustomMeshElement.selectedTrunkIndex = list.index;
                        rebuildTreeChange = true;
                    }
                },
                onAddCallback = (list) =>
                {
                    Undo.RecordObject(trunkCustomMeshElement, "Add Trunk Mesh Descriptor");
                    TrunkMeshDescriptor newDescriptor = new TrunkMeshDescriptor();
                    trunkCustomMeshElement.trunkMeshes.Add(newDescriptor);
                    EditorUtility.SetDirty(trunkCustomMeshElement);
                    _selectedDescriptorIndex = trunkCustomMeshElement.trunkMeshes.Count - 1;
                    _selectedTrunkMeshDesc = trunkCustomMeshElement.trunkMeshes [_selectedDescriptorIndex];
                    list.index = _selectedDescriptorIndex;
                    _trunkDefList = null;
                },
                onRemoveCallback = (list) =>
                {
                    if (EditorUtility.DisplayDialog("Remove Trunk Mesh Descriptor",
                        $"Are you sure you want to remove descriptor 'Trunk {list.index}'?", "Yes", "No"))
                    {
                        Undo.RecordObject(trunkCustomMeshElement, "Remove Trunk Mesh Descriptor");

                        if (trunkCustomMeshElement.selectedTrunkIndex == list.index)
                        {
                            rebuildTreeChange = true;
                            trunkCustomMeshElement.selectedTrunkIndex = -1;
                        }
                        else if (trunkCustomMeshElement.selectedTrunkIndex > list.index)
                        {
                            trunkCustomMeshElement.selectedTrunkIndex--;
                        }

                        trunkCustomMeshElement.trunkMeshes.RemoveAt(list.index);
                        EditorUtility.SetDirty(trunkCustomMeshElement);
                        
                        if (_selectedDescriptorIndex == list.index) {
                            _selectedDescriptorIndex = -1;
                             _trunkDefList = null;
                        } else if (_selectedDescriptorIndex > list.index) {
                            _selectedDescriptorIndex--;
                        }
                        if (_selectedDescriptorIndex >= 0) {
                            _selectedTrunkMeshDesc = trunkCustomMeshElement.trunkMeshes[_selectedDescriptorIndex];
                        } else {
                            _selectedTrunkMeshDesc = null;
                        }
                    }
                }
            };
            if (_selectedDescriptorIndex >= trunkCustomMeshElement.trunkMeshes.Count) {
                _selectedDescriptorIndex = -1;
            }
             if (_selectedDescriptorIndex < 0 && trunkCustomMeshElement.trunkMeshes.Count > 0) {
                _selectedDescriptorIndex = 0;
            }
            _descriptorList.index = _selectedDescriptorIndex;
            if (_selectedDescriptorIndex >= 0) {
                _selectedTrunkMeshDesc = trunkCustomMeshElement.trunkMeshes[_selectedDescriptorIndex];
            } else {
                _selectedTrunkMeshDesc = null;
            }

            // If the list is not empty but no trunk is selected, select the first one.
            if (trunkCustomMeshElement.trunkMeshes.Count > 0 && trunkCustomMeshElement.selectedTrunkIndex < 0) {
                trunkCustomMeshElement.selectedTrunkIndex = 0;
            }
            // If the list is empty, ensure selection is -1.
            if (trunkCustomMeshElement.trunkMeshes.Count == 0) {
                trunkCustomMeshElement.selectedTrunkIndex = -1;
            }
        }

        private void SetupTrunkDefList(SerializedProperty trunkDefsArrayProp)
        {
            _trunkDefList = new ReorderableList(serializedObject, trunkDefsArrayProp, true, true, true, true)
            {
                drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, GC_TrunkDefListHeader),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    EditorGUI.LabelField(rect, $"Definition {index}");
                },
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                onSelectCallback = (list) =>
                {
                     if (_selectedTrunkDefIndex != list.index) {
                        _selectedTrunkDefIndex = list.index;
                        if (_bezierCurveEditor != null) _bezierCurveEditor.ClearSelection();
                    }
                },
                onAddCallback = (list) =>
                {
                    var parentDescriptorProp = _trunkMeshesProp.GetArrayElementAtIndex(_selectedDescriptorIndex);
                    var trunkDefsProp = parentDescriptorProp.FindPropertyRelative(TrunkDefsPropName);
                    var newIndex = trunkDefsProp.arraySize;

                    Undo.RecordObject(trunkCustomMeshElement, "Add Trunk Definition");
                    TrunkMeshDescriptor descriptor = trunkCustomMeshElement.trunkMeshes[_selectedDescriptorIndex];
                    TrunkMeshDescriptor.TrunkDef newTrunkDef = new TrunkMeshDescriptor.TrunkDef
                    {
                        transitionLength = 0.5f,
                        transitionRadius = 0.1f,
                        curve = new BezierCurve()
                    };
                    newTrunkDef.curve.nodes.Clear();
                    newTrunkDef.curve.AddNode(new BezierNode(Vector3.zero));
                    newTrunkDef.curve.AddNode(new BezierNode(Vector3.up * 2f));
                    newTrunkDef.curve.Process();

                    descriptor.trunkDefs.Add(newTrunkDef);
                    EditorUtility.SetDirty(trunkCustomMeshElement);
                    serializedObject.Update();
                    _trunkDefList.serializedProperty = serializedObject.FindProperty(parentDescriptorProp.propertyPath).FindPropertyRelative(TrunkDefsPropName);

                    _selectedTrunkDefIndex = newIndex;
                    list.index = newIndex;
                },
                onRemoveCallback = (list) =>
                {
                    if (EditorUtility.DisplayDialog("Remove Trunk Definition",
                        $"Are you sure you want to remove definition '{list.index}'?", "Yes", "No"))
                    {
                        Undo.RecordObject(trunkCustomMeshElement, "Remove Trunk Definition");
                        TrunkMeshDescriptor descriptor = trunkCustomMeshElement.trunkMeshes[_selectedDescriptorIndex];
                        descriptor.trunkDefs.RemoveAt(list.index);
                        EditorUtility.SetDirty(trunkCustomMeshElement);
                        serializedObject.Update();
                         _trunkDefList.serializedProperty = serializedObject.FindProperty(_trunkMeshesProp.GetArrayElementAtIndex(_selectedDescriptorIndex).propertyPath).FindPropertyRelative(TrunkDefsPropName);

                        if (_selectedTrunkDefIndex == list.index) {
                            _selectedTrunkDefIndex = -1;
                        } else if (_selectedTrunkDefIndex > list.index) {
                            _selectedTrunkDefIndex--;
                        }
                    }
                }
            };

            if (_selectedTrunkDefIndex >= (_trunkDefList.serializedProperty != null ? _trunkDefList.serializedProperty.arraySize : 0) ) {
                _selectedTrunkDefIndex = -1;
            }
            if (_selectedTrunkDefIndex < 0 && (_trunkDefList.serializedProperty != null && _trunkDefList.serializedProperty.arraySize > 0)) {
                _selectedTrunkDefIndex = 0;
            }
            _trunkDefList.index = _selectedTrunkDefIndex;
        }

        protected override void OnInspectorGUISpecific()
        {
            UpdateSerialized ();

            if (_descriptorList == null) {
                if (_trunkMeshesProp != null) SetupDescriptorList();
                else {
                    EditorGUILayout.HelpBox($"'{TrunkMeshesPropName}' property not found.", MessageType.Error);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
            }
             if (!_isCurveEditorInitialized && _bezierCurveEditor != null) {
                InitializeCurveEditor();
            }

            EditorGUILayout.Space();
            _descriptorList.DoLayoutList();
            EditorGUILayout.Space();

            if (_selectedDescriptorIndex >= 0 && _selectedDescriptorIndex < _trunkMeshesProp.arraySize)
            {
                var editingHeaderContent = new GUIContent(string.Format(GC_EditingDescriptorHeader.text, _selectedDescriptorIndex), GC_EditingDescriptorHeader.tooltip);
                EditorGUILayout.LabelField(editingHeaderContent, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                SerializedProperty selectedDescriptorProp = _trunkMeshesProp.GetArrayElementAtIndex(_selectedDescriptorIndex);

                EditorGUI.BeginChangeCheck ();
                EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(EnabledPropName), GC_Enabled);
                EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(GameObjectPropName), GC_GameObject);
                if (EditorGUI.EndChangeCheck () && trunkCustomMeshElement.selectedTrunkIndex == _selectedDescriptorIndex) {
                    rebuildTreeChange = true;
                }

                EditorGUI.BeginChangeCheck ();
                SerializedProperty hasLODProp = selectedDescriptorProp.FindPropertyRelative(HasLODPropName);
                EditorGUILayout.PropertyField(hasLODProp, GC_HasLOD);
                if (hasLODProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(GameObjectLOD1PropName), GC_GameObjectLOD1);
                    EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(GameObjectLOD2PropName), GC_GameObjectLOD2);
                    EditorGUI.indentLevel--;
                }
                if (EditorGUI.EndChangeCheck ()) {
                    serializeChange = true;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Mesh Transform Offsets", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck ();
                EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(PositionPropName), GC_Position);
                EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(RotationPropName), GC_Rotation);
                EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(ScalePropName), GC_Scale);
                if (EditorGUI.EndChangeCheck () && trunkCustomMeshElement.selectedTrunkIndex == _selectedDescriptorIndex) {
                    localChange = true;
                }
                EditorGUILayout.Space();

                DrawMeshPreview(selectedDescriptorProp);

                _showAdvancedTexProps = EditorGUILayout.Foldout(_showAdvancedTexProps, GC_AdvancedTexFoldout, true);
                if (_showAdvancedTexProps)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(AlbedoTexPropName), GC_AlbedoTexProp);
                    SerializedProperty hasNormalTexProp = selectedDescriptorProp.FindPropertyRelative(HasNormalTexPropName);
                    EditorGUILayout.PropertyField(hasNormalTexProp, GC_HasNormalTex);
                    if (hasNormalTexProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(NormalTexPropName), GC_NormalTexProp);
                    }
                    SerializedProperty hasExtraTexProp = selectedDescriptorProp.FindPropertyRelative(HasExtraTexPropName);
                    EditorGUILayout.PropertyField(hasExtraTexProp, GC_HasExtraTex);
                    if (hasExtraTexProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(selectedDescriptorProp.FindPropertyRelative(ExtraTexPropName), GC_ExtraTexProp);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();

                SerializedProperty trunkDefsProp = selectedDescriptorProp.FindPropertyRelative(TrunkDefsPropName);
                if (_trunkDefList == null || _trunkDefList.serializedProperty.propertyPath != trunkDefsProp.propertyPath) {
                    SetupTrunkDefList(trunkDefsProp);
                }
                _trunkDefList.DoLayoutList();
                EditorGUILayout.Space();

                if (_selectedTrunkDefIndex >= 0 && _selectedTrunkDefIndex < trunkDefsProp.arraySize)
                {
                    var editingDefHeaderContent = new GUIContent(string.Format(GC_EditingTrunkDefHeader.text, _selectedTrunkDefIndex), GC_EditingTrunkDefHeader.tooltip);
                    EditorGUILayout.LabelField(editingDefHeaderContent, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    SerializedProperty selectedTrunkDefProp = trunkDefsProp.GetArrayElementAtIndex(_selectedTrunkDefIndex);
                    /*
                    BezierCurve currentSelectedCurve = GetSelectedTrunkDef()?.curve;
                    if (_bezierCurveEditor != null && _isCurveEditorInitialized && currentSelectedCurve != null) {
                         _bezierCurveEditor.curve = currentSelectedCurve;
                         _bezierCurveEditor.curveId = currentSelectedCurve.guid;
                         _bezierCurveEditor.ShowToolbar();
                         EditorGUILayout.LabelField(GC_CurveEditorSettings);
                         _bezierCurveEditor.OnInspectorGUI(currentSelectedCurve);
                         EditorGUILayout.Space();
                    } else {
                        EditorGUILayout.HelpBox("BezierCurve or Curve Editor not available for this definition.", MessageType.Warning);
                        EditorGUILayout.PropertyField(selectedTrunkDefProp.FindPropertyRelative(CurvePropName), new GUIContent("Bezier Curve (Raw)"), true);
                    }
                    */
                    EditorGUI.BeginChangeCheck ();
                    EditorGUILayout.PropertyField(selectedTrunkDefProp.FindPropertyRelative(GirthScalePropName), GC_GirthScale);
                    EditorGUILayout.PropertyField(selectedTrunkDefProp.FindPropertyRelative(TransitionLengthPropName), GC_TransitionLength);
                    EditorGUILayout.PropertyField(selectedTrunkDefProp.FindPropertyRelative(TransitionRadiusPropName), GC_TransitionRadius);
                    if (EditorGUI.EndChangeCheck () && trunkCustomMeshElement.selectedTrunkIndex == _selectedDescriptorIndex) {
                        rebuildTreeChange = true;
                    }
                    EditorGUI.indentLevel--;
                }
                 EditorGUI.indentLevel--;
            }
            else if (_trunkMeshesProp.arraySize > 0)
            {
                 EditorGUILayout.HelpBox("Select a Trunk Mesh Descriptor from the list to edit its properties.", MessageType.Info);
            }

            if (localChange || rebuildTreeChange || serializeChange) {
                ApplySerialized ();
                if (localChange) {
                    Debug.Log ("LOCAL: Change made to TrunkCustomMeshElementEditor.");
                    UpdateComponent (TrunkCustomMeshComponent.CMD_UPDATE_TRUNK_GO);
                }
                if (rebuildTreeChange) {
                    UpdatePipeline (GlobalSettings.processingDelayVeryHigh);
                    Debug.Log ("REBUILD: Change made to TrunkCustomMeshElementEditor.");
                }
				trunkCustomMeshElement.Validate ();
                SceneView.RepaintAll();

                serializeChange = false;
                localChange = false;
                rebuildTreeChange = false;
			}
			EditorGUILayout.Space ();
        }

        /// <summary>
        /// Draws handles and gizmos on the scene view.
        /// </summary>
        /// <param name="sceneView">SceneView instance to draw the handles/gizmos.</param>
        protected override void OnSceneGUI (SceneView sceneView)
        {
            if (_bezierCurveEditor == null || !_isCurveEditorInitialized) return;

            if (_selectedTrunkMeshDesc != null) {
                // Get the selected trunk def.
                TrunkMeshDescriptor.TrunkDef currentTrunkDef = GetSelectedTrunkDef();

                // Prepare the curve editor.
                float scale = TreeFactoryEditorWindow.editorWindow.treeFactory.treeFactoryPreferences.factoryScale;
                Vector3 offset = TreeFactoryEditorWindow.editorWindow.treeFactory.GetPreviewTreeWorldOffset ();

                _bezierCurveEditor.curve = currentTrunkDef.curve;
                _bezierCurveEditor.curveId = currentTrunkDef.curve.guid;
                _bezierCurveEditor.scale = scale;

                _cylinderHandle.scale = scale;
                _cylinderHandle.offset = offset;


                // Draw all the trunk def curves.
                foreach (TrunkMeshDescriptor.TrunkDef trunkDef in _selectedTrunkMeshDesc.trunkDefs) {
                    // If the trunk def is selected, draw the editable curve.
                    if (trunkDef == currentTrunkDef) {
                        if (currentTrunkDef != null && currentTrunkDef.curve != null) {
                            _bezierCurveEditor.OnCurveGUI(currentTrunkDef.curve, offset, true, true);
                            _bezierCurveEditor.OnControlsGUI(Vector3.zero);
                            currentTrunkDef.curve.Process ();

                            // Draw cylinder handle.
                            Color col = Color.red;
                            col.a = 0.5f;
                            _tmpCylHeight = currentTrunkDef.transitionLength;
                            _tmpCylRadius = currentTrunkDef.transitionRadius;
                            _trunkDefTransitionChanged = false;
                            _cylinderHandle.Draw (
                                currentTrunkDef.curve.Last().position, 
                                currentTrunkDef.curve.GetPointAt (0.99f).forward, 
                                ref _tmpCylHeight, 
                                ref _tmpCylRadius, 
                                col);
                            if (_tmpCylHeight != currentTrunkDef.transitionLength || _tmpCylRadius != currentTrunkDef.transitionRadius) {
                                _trunkDefTransitionChanged = true;
                                currentTrunkDef.transitionLength = _tmpCylHeight;
                                currentTrunkDef.transitionRadius = _tmpCylRadius;
                            }
                            if (GUI.changed || _trunkDefTransitionChanged)
                            {
                                EditorUtility.SetDirty(trunkCustomMeshElement);
                            }
                        }
                    }
                    // If not, draw the preview curve. 
                    else {
                        _bezierCurveEditor.OnCurveGUI(trunkDef.curve, offset, false, false);
                    }
                }
            }
        }

        private void DrawMeshPreview(SerializedProperty selectedDescriptorProp)
        {
            bool hasLOD = selectedDescriptorProp.FindPropertyRelative(HasLODPropName).boolValue;
            GameObject go = (GameObject)selectedDescriptorProp.FindPropertyRelative(GameObjectPropName).objectReferenceValue;
            GameObject goLOD1 = hasLOD ? (GameObject)selectedDescriptorProp.FindPropertyRelative(GameObjectLOD1PropName).objectReferenceValue : null;
            GameObject goLOD2 = hasLOD ? (GameObject)selectedDescriptorProp.FindPropertyRelative(GameObjectLOD2PropName).objectReferenceValue : null;

            List<string> lodOptions = new List<string>();
            List<GameObject> availableGOs = new List<GameObject>();

            if (go != null) availableGOs.Add(go);
            if (goLOD1 != null) availableGOs.Add(goLOD1);
            if (goLOD2 != null) availableGOs.Add(goLOD2);

            for (int i = 0; i < availableGOs.Count; i++) {
                lodOptions.Add("LOD " + i);
            }

            if (availableGOs.Count > 0)
            {
                EditorGUILayout.LabelField("Mesh Preview", EditorStyles.boldLabel);
                if (availableGOs.Count > 1) {
                    _selectedLODPreviewIndex = GUILayout.Toolbar(_selectedLODPreviewIndex, lodOptions.ToArray(), EditorStyles.toolbarButton);
                } else {
                    _selectedLODPreviewIndex = 0;
                }

                if (_selectedLODPreviewIndex >= availableGOs.Count) {
                    _selectedLODPreviewIndex = 0;
                }

                GameObject targetGO = availableGOs[_selectedLODPreviewIndex];

                Mesh tempMesh = null;
                if (targetGO != null) {
                    MeshFilter mf = targetGO.GetComponent<MeshFilter>();
                    SkinnedMeshRenderer smr = targetGO.GetComponent<SkinnedMeshRenderer>();
                    if (mf != null) tempMesh = mf.sharedMesh;
                    else if (smr != null) tempMesh = smr.sharedMesh;
                }

                if (tempMesh != null) {
                    string meshInfo = $"Verts: {tempMesh.vertexCount} | Tris: {tempMesh.triangles.Length / 3}";
                    EditorGUILayout.LabelField(meshInfo, EditorStyles.centeredGreyMiniLabel);
                }

                Rect previewRect = GUILayoutUtility.GetRect(0, 200, GUILayout.ExpandWidth(true));

                if (targetGO != null && _previewRenderUtility != null)
                {
                    int controlID = GUIUtility.GetControlID(FocusType.Passive);
                    Event currentEvent = Event.current;
                    if (previewRect.Contains(currentEvent.mousePosition))
                    {
                        if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
                        {
                            _previewDir.x += currentEvent.delta.x * 2.0f;
                            _previewDir.y -= currentEvent.delta.y * 2.0f;
                            currentEvent.Use();
                        }
                    }

                    _previewRenderUtility.BeginPreview(previewRect, EditorStyles.helpBox);

                    Mesh meshToDraw = tempMesh;
                    Material[] materialsToDraw = null;
                    if (targetGO.GetComponent<MeshRenderer>() != null) {
                        materialsToDraw = targetGO.GetComponent<MeshRenderer>().sharedMaterials;
                    } else if (targetGO.GetComponent<SkinnedMeshRenderer>() != null) {
                        materialsToDraw = targetGO.GetComponent<SkinnedMeshRenderer>().sharedMaterials;
                    }

                    if (meshToDraw != null)
                    {
                        Quaternion rotation = Quaternion.Euler(_previewDir.y, 0, 0) * Quaternion.Euler(0, _previewDir.x, 0);
                        Bounds bounds = meshToDraw.bounds;
                        float magnitude = bounds.extents.magnitude;
                        float distance = 4.0f * magnitude;

                        _previewRenderUtility.camera.transform.position = bounds.center - (rotation * Vector3.forward * distance);
                        _previewRenderUtility.camera.transform.rotation = rotation;
                        _previewRenderUtility.camera.farClipPlane = distance + magnitude * 1.1f;
                        _previewRenderUtility.camera.nearClipPlane = distance - magnitude * 1.1f;

                        if (materialsToDraw != null && materialsToDraw.Length > 0) {
                            for (int i = 0; i < materialsToDraw.Length; i++) {
                                if (i < meshToDraw.subMeshCount) {
                                    _previewRenderUtility.DrawMesh(meshToDraw, Matrix4x4.identity, materialsToDraw[i], i);
                                }
                            }
                        } else {
                            Material tempMat = new Material(Shader.Find("Standard"));
                            _previewRenderUtility.DrawMesh(meshToDraw, Matrix4x4.identity, tempMat, 0);
                            DestroyImmediate(tempMat);
                        }
                    }

                    _previewRenderUtility.Render();
                    Texture result = _previewRenderUtility.EndPreview();
                    GUI.DrawTexture(previewRect, result, ScaleMode.StretchToFill, false);
                }
            }
        }
        private TrunkMeshDescriptor.TrunkDef GetSelectedTrunkDef()
        {
            if (_selectedTrunkMeshDesc != null && _selectedTrunkDefIndex >= 0 && 
                _selectedTrunkDefIndex < _selectedTrunkMeshDesc.trunkDefs.Count)
            {
                return _selectedTrunkMeshDesc.trunkDefs[_selectedTrunkDefIndex];
            }
            return null;
        }

        #region BezierCurveEditor Event Handlers
        private void RecordChange(string undoMessage = "Modify Bezier Curve") {
            Undo.RecordObject(trunkCustomMeshElement, undoMessage);
            EditorUtility.SetDirty(trunkCustomMeshElement);
            if (trunkCustomMeshElement.selectedTrunkIndex == _selectedDescriptorIndex)
                rebuildTreeChange = true;
        }
        private void HandleCurvesChanged(List<BezierCurve> curves) {
            RecordChange("Curve(s) Changed");
        }

        private void HandleCurveEditModeChanged(BezierCurveEditor.EditMode editMode) { Repaint(); }
        private void HandleCurveNodeSelectionChanged(List<BezierNode> nodes, List<int> indexes, List<System.Guid> curveIds) {
            Repaint();
            if (_bezierCurveEditor != null) {
                if (nodes.Count == 1 && curveIds.Count > 0) {
                     _bezierCurveEditor.focusedCurveId = curveIds [0];
                } else {
                    _bezierCurveEditor.focusedCurveId = System.Guid.Empty;
                     if (_bezierCurveEditor.editMode == BezierCurveEditor.EditMode.Add)
                        _bezierCurveEditor.editMode = BezierCurveEditor.EditMode.Selection;
                }
            }
        }
        private Vector3 HandleCurveCheckDragNodes(Vector3 offset) { return offset; }
        private void HandleCurveBeginDragNodes(List<BezierNode> nodes, List<int> indexes, List<System.Guid> curveIds) { RecordChange("Move Bezier Nodes"); }
        private void HandleCurveBeforeDragNodes(List<BezierNode> nodes, List<int> indexes, List<System.Guid> curveIds) { }
        private void HandleCurveDragNodes(List<BezierNode> nodes, List<int> indexes, List<System.Guid> curveIds) { EditorUtility.SetDirty(trunkCustomMeshElement); }
        private void HandleCurveEndDragNodes(List<BezierNode> nodes, List<int> indexes, List<System.Guid> curveIds) {
            EditorUtility.SetDirty(trunkCustomMeshElement);
        }
        private void HandleCurveBeforeEditNodeStyle(BezierNode node, int index) { RecordChange("Edit Bezier Node Style"); }
        private void HandleCurveEditNodeStyle(BezierNode node, int index) { EditorUtility.SetDirty(trunkCustomMeshElement); }
        private void HandleCurveBeforeAddNode(BezierNode candidateNode) {
            RecordChange("Add Bezier Node");
        }
        private void HandleCurveAddNode(BezierNode addedNode, int index, float relativePosition) {
            addedNode.handleStyle = BezierNode.HandleStyle.Auto;
            EditorUtility.SetDirty(trunkCustomMeshElement);
             if (_bezierCurveEditor != null) {
                _bezierCurveEditor.editMode = BezierCurveEditor.EditMode.Selection;
                 if (addedNode.curve != null) {
                    _bezierCurveEditor.ClearSelection (true, false);
                    _bezierCurveEditor.AddNodeToSelection (addedNode, index, addedNode.curve.guid);
                 }
            }
        }
        private bool HandleCurveCheckRemoveNodes(List<BezierNode> nodes, List<int> indexes, List<System.Guid> curveIds) { return true; }
        private void HandleCurveBeforeRemoveNodes(List<BezierNode> nodes, List<int> index, List<System.Guid> curveIds) { RecordChange("Remove Bezier Nodes");}
        private void HandleCurveRemoveNodes(List<BezierNode> nodes, List<int> index, List<System.Guid> curveIds) { EditorUtility.SetDirty(trunkCustomMeshElement); }
        private bool HandleCurveBeginDragHandle(BezierNode node, int index, System.Guid curveId, int handle) { RecordChange("Move Bezier Handle"); return true; }
        private bool HandleCurveDragHandle(BezierNode node, int index, System.Guid curveId, int handle) { EditorUtility.SetDirty(trunkCustomMeshElement); return true; }
        private bool HandleCurveEndDragHandle(BezierNode node, int index, System.Guid curveId, int handle) { EditorUtility.SetDirty(trunkCustomMeshElement); return true; }
        private BezierCurveEditor.SelectionCommand HandleCurveCheckSelectCmd(BezierNode node) { return BezierCurveEditor.SelectionCommand.Select; }
        private void HandleCurveSelectNode(BezierNode node) { }
        private void HandleCurveDeselectNode(BezierNode node) { }
        private BezierCurveEditor.ControlType HandleCurveCheckNodeControls(BezierNode node, int index, System.Guid curveId) { return BezierCurveEditor.ControlType.Move; }
        #endregion
    }
}