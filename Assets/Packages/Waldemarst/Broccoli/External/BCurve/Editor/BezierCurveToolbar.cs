#if UNITY_2022_1_OR_NEWER

using UnityEngine;

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

using Broccoli.Model;
using Broccoli.Utils;
using Unity.VisualScripting;
using System.Collections;

namespace Broccoli.Utils
{
    //[Overlay(typeof(SceneView), "Bezier Curve Toolbar")]
    [Overlay(typeof(SceneView), "Bezier Curve Toolbar", "Bezier Curve Toolbar", false, defaultDockZone = DockZone.TopToolbar, defaultLayout = Layout.HorizontalToolbar)]
    public class BezierCurveToolbar : ToolbarOverlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        /// <summary>
        /// Teture manager for this canvas GUI.
        /// </summary>
        /// <returns>Texture manager.</returns>
        private VisualElement panelVisualElement = null;
        public BezierCurveEditor curveEditor = null;
        private BezierCurveToolbarEditMode editModeDropdown = null;
        private BezierCurveToolbarSmoothHandles smoothHandlesBtn = null;
        private BezierCurveToolbarAlignedHandles alignedHandlesBtn = null;
        private BezierCurveToolbarFreeHandles freeHandlesBtn = null; 
        private BezierCurveToolbarNoHandles noHandlesBtn = null;
        private BezierCurveToolbarRemoveNode removeNodeBtn = null;
        private BezierCurveToolbarPivotMode pivotModeDropdown = null;
        /*
        private BezierCurveToolbarLinkNode linkNodeBtn = null;
        */

        private const string LABEL_CLASS = "unity-editor-toolbar-element__label";
        
        BezierCurveToolbar () : base(
            BezierCurveToolbarEditMode.id,
            BezierCurveToolbarSmoothHandles.id,
            BezierCurveToolbarAlignedHandles.id,
            BezierCurveToolbarFreeHandles.id,
            BezierCurveToolbarNoHandles.id,
            BezierCurveToolbarRemoveNode.id,
            BezierCurveToolbarPivotMode.id/*,
            BezierCurveToolbarLinkNode.id
            */
            )
        {}

        private void OnLayoutChanged (Layout layout)
        {
            //Debug.Log ("Layout changed to " + layout);
        }

        public override VisualElement CreatePanelContent()
        {
            panelVisualElement = base.CreatePanelContent();
            SetToolbarElements (panelVisualElement);
            UpdateToolbar (curveEditor);
            return panelVisualElement;
        }

        //
        // Summary:
        //     Called when an Overlay is about to be destroyed.
        public override void OnWillBeDestroyed()
        {
        }
        
        public new OverlayToolbar CreateHorizontalToolbarContent ()
        {
            OverlayToolbar toolbar = new OverlayToolbar();
            TextElement text;

            editModeDropdown = new BezierCurveToolbarEditMode ();
            text = editModeDropdown.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            editModeDropdown.SetLayout (Layout.HorizontalToolbar);
            toolbar.Add (editModeDropdown);
            
            smoothHandlesBtn = new BezierCurveToolbarSmoothHandles ();
            text = smoothHandlesBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            smoothHandlesBtn.SetLayout (Layout.HorizontalToolbar);
            toolbar.Add (smoothHandlesBtn);

            alignedHandlesBtn = new BezierCurveToolbarAlignedHandles ();
            text = alignedHandlesBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            alignedHandlesBtn.SetLayout (Layout.HorizontalToolbar);
            toolbar.Add (alignedHandlesBtn);

            freeHandlesBtn = new BezierCurveToolbarFreeHandles ();
            text = freeHandlesBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            freeHandlesBtn.SetLayout (Layout.HorizontalToolbar);
            toolbar.Add (freeHandlesBtn);

            noHandlesBtn = new BezierCurveToolbarNoHandles ();
            text = noHandlesBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            noHandlesBtn.SetLayout (Layout.HorizontalToolbar);
            toolbar.Add (noHandlesBtn);

            removeNodeBtn = new BezierCurveToolbarRemoveNode ();
            text = removeNodeBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            removeNodeBtn.SetLayout (Layout.HorizontalToolbar);
            toolbar.Add (removeNodeBtn);

            pivotModeDropdown = new BezierCurveToolbarPivotMode ();
            text = pivotModeDropdown.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            pivotModeDropdown.SetLayout (Layout.HorizontalToolbar);
            toolbar.Add (pivotModeDropdown);
            /*
            linkNodeBtn = new BezierCurveToolbarLinkNode ();
            text = linkNodeBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            linkNodeBtn.SetLayout (Layout.HorizontalToolbar);
            toolbar.Add (linkNodeBtn);
            */
            UpdateToolbar (curveEditor);
            return toolbar;
        }

        public new OverlayToolbar CreateVerticalToolbarContent ()
        {
            OverlayToolbar toolbar = new OverlayToolbar();
            TextElement text;

            editModeDropdown = new BezierCurveToolbarEditMode ();
            text = editModeDropdown.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            editModeDropdown.SetLayout (Layout.VerticalToolbar);
            toolbar.Add (editModeDropdown);
            
            smoothHandlesBtn = new BezierCurveToolbarSmoothHandles ();
            text = smoothHandlesBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            smoothHandlesBtn.SetLayout (Layout.VerticalToolbar);
            toolbar.Add (smoothHandlesBtn);

            alignedHandlesBtn = new BezierCurveToolbarAlignedHandles ();
            text = alignedHandlesBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            alignedHandlesBtn.SetLayout (Layout.VerticalToolbar);
            toolbar.Add (alignedHandlesBtn);

            freeHandlesBtn = new BezierCurveToolbarFreeHandles ();
            text = freeHandlesBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            freeHandlesBtn.SetLayout (Layout.VerticalToolbar);
            toolbar.Add (freeHandlesBtn);

            noHandlesBtn = new BezierCurveToolbarNoHandles ();
            text = noHandlesBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            noHandlesBtn.SetLayout (Layout.VerticalToolbar);
            toolbar.Add (noHandlesBtn);

            removeNodeBtn = new BezierCurveToolbarRemoveNode ();
            text = removeNodeBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            removeNodeBtn.SetLayout (Layout.VerticalToolbar);
            toolbar.Add (removeNodeBtn);

            pivotModeDropdown = new BezierCurveToolbarPivotMode ();
            text = pivotModeDropdown.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            pivotModeDropdown.SetLayout (Layout.VerticalToolbar);
            toolbar.Add (pivotModeDropdown);
            /*
            linkNodeBtn = new BezierCurveToolbarLinkNode ();
            text = linkNodeBtn.Query<TextElement> (null, LABEL_CLASS);
            text.style.display = DisplayStyle.None;
            linkNodeBtn.SetLayout (Layout.VerticalToolbar);
            toolbar.Add (linkNodeBtn);
            */
            UpdateToolbar (curveEditor);
            return toolbar;
        }

        private void SetToolbarElements (VisualElement rootVisualElement)
        {
            editModeDropdown = rootVisualElement.Query<BezierCurveToolbarEditMode> ();
            smoothHandlesBtn = rootVisualElement.Query<BezierCurveToolbarSmoothHandles> ();
            alignedHandlesBtn = rootVisualElement.Query<BezierCurveToolbarAlignedHandles> ();
            freeHandlesBtn = rootVisualElement.Query<BezierCurveToolbarFreeHandles> ();
            noHandlesBtn = rootVisualElement.Query<BezierCurveToolbarNoHandles> ();
            removeNodeBtn = rootVisualElement.Query<BezierCurveToolbarRemoveNode> ();
            pivotModeDropdown = rootVisualElement.Query<BezierCurveToolbarPivotMode> ();
            /*
            linkNodeBtn = rootVisualElement.Query<BezierCurveToolbarLinkNode> ();
            */
        }

        private void ReloadIcons ()
        {
            LoadIcons ();
            editModeDropdown.SetIcon ();
            smoothHandlesBtn.SetIcon ();
            alignedHandlesBtn.SetIcon ();
            freeHandlesBtn.SetIcon ();
            noHandlesBtn.SetIcon ();
            removeNodeBtn.SetIcon ();
            pivotModeDropdown.SetIcon ();
            /*
            linkNodeBtn.SetIcon ();
            */
        }

        public void UpdateToolbar (BezierCurveEditor curveEditor)
        {
            if (curveEditor == null) return;
            this.curveEditor = curveEditor;

            if (editModeDropdown == null) {
                SetToolbarElements (panelVisualElement);
            }

            if (editModeDropdown.icon == null || smoothHandlesBtn.icon == null) {
                ReloadIcons ();
            }
            
            editModeDropdown.curveToolbar = this;
            smoothHandlesBtn.curveToolbar = this;
            alignedHandlesBtn.curveToolbar = this;
            freeHandlesBtn.curveToolbar = this;
            noHandlesBtn.curveToolbar = this;
            removeNodeBtn.curveToolbar = this;
            pivotModeDropdown.curveToolbar = this;
            /*
            linkNodeBtn.curveToolbar = this;
            */

            // EDIT MODE DROPDOWN.
            if (editModeDropdown != null) {
                if (this.curveEditor.editMode == BezierCurveEditor.EditMode.Add) {
                    editModeDropdown.editMode = BezierCurveToolbarEditMode.EditMode.AddNode;
                } else {
                    editModeDropdown.editMode = BezierCurveToolbarEditMode.EditMode.Selection;
                }
            }

            
            // NODE HANDLE STYLE.
            if (smoothHandlesBtn != null && alignedHandlesBtn != null && 
                freeHandlesBtn != null && noHandlesBtn != null)
            {
                bool enableHandleBtns = this.curveEditor.hasSingleSelection && this.curveEditor.enableToolbarNode;
                smoothHandlesBtn.SetValueWithoutNotify (false);
                alignedHandlesBtn.SetValueWithoutNotify (false);
                freeHandlesBtn.SetValueWithoutNotify (false);
                noHandlesBtn.SetValueWithoutNotify (false);

                // Disable all handles mode btns.
                if (!enableHandleBtns) {
                    smoothHandlesBtn.SetEnabled (false);
                    alignedHandlesBtn.SetEnabled (false);
                    freeHandlesBtn.SetEnabled (false);
                    noHandlesBtn.SetEnabled (false);
                } else {
                    smoothHandlesBtn.SetEnabled (true);
                    alignedHandlesBtn.SetEnabled (true);
                    freeHandlesBtn.SetEnabled (true);
                    noHandlesBtn.SetEnabled (true);

                    BezierNode node = this.curveEditor.selectedNode;
                    if (node != null) {
                        switch (node.handleStyle) {
                            case BezierNode.HandleStyle.Auto: smoothHandlesBtn.SetValueWithoutNotify (true); break;
                            case BezierNode.HandleStyle.Aligned: alignedHandlesBtn.SetValueWithoutNotify (true); break;
                            case BezierNode.HandleStyle.Free: freeHandlesBtn.SetValueWithoutNotify (true); break;
                            case BezierNode.HandleStyle.None: noHandlesBtn.SetValueWithoutNotify (true); break;
                        }
                    }
                }
            }

            // REMOVE BUTTON
            if (removeNodeBtn != null) {
                bool enableRemove = this.curveEditor.hasSingleSelection && this.curveEditor.enableToolbarNode;
                BezierNode nodeToRemove = this.curveEditor.selectedNode;
                removeNodeBtn.SetEnabled (false);
                if (enableRemove && nodeToRemove.relativePosition != 0f && nodeToRemove.relativePosition != 1f) {
                    removeNodeBtn.SetEnabled (true);
                }
            }

            // PIVOT MODE DROPDOWN.
            if (pivotModeDropdown != null) {
                switch (this.curveEditor.pivotMode) {
                    case BezierCurveEditor.PivotMode.Origin:
                        pivotModeDropdown.pivotMode = BezierCurveToolbarPivotMode.PivotMode.Origin;
                        break;
                    case BezierCurveEditor.PivotMode.Center:
                        pivotModeDropdown.pivotMode = BezierCurveToolbarPivotMode.PivotMode.Center;
                        break;
                    case BezierCurveEditor.PivotMode.CenterBottom:
                        pivotModeDropdown.pivotMode = BezierCurveToolbarPivotMode.PivotMode.CenterBottom;
                        break;
                    case BezierCurveEditor.PivotMode.FirstNode:
                        pivotModeDropdown.pivotMode = BezierCurveToolbarPivotMode.PivotMode.FirstNode;
                        break;
                    case BezierCurveEditor.PivotMode.LastNode:
                        pivotModeDropdown.pivotMode = BezierCurveToolbarPivotMode.PivotMode.LastNode;
                        break;
                }
            }
            /*
            // SOCKET CONNECT MODE
            if (linkNodeBtn != null) {
                if (arrayEditor.editMode == SocketArrayEditor.EditMode.Connect) {
                    linkNodeBtn.SetValueWithoutNotify (true);
                } else {
                    linkNodeBtn.SetValueWithoutNotify (false);
                }
            }
            */
        }

        public void SetEditModeToSelect () {
            if (curveEditor != null) curveEditor.editMode = BezierCurveEditor.EditMode.Selection;
        }

        public void SetEditModeToAdd () {
            if (curveEditor != null) curveEditor.editMode = BezierCurveEditor.EditMode.Add;
        }

        public void SetHandlesMode (int handlesMode) {
            if (curveEditor != null) {
                bool enableChange = curveEditor.hasSingleSelection;
                BezierNode node = curveEditor.selectedNode;
                bool isLastNode = (curveEditor.selectedNodeIndex == curveEditor.curve.nodeCount - 1);
                BezierNode.HandleStyle newHandleStyle = BezierNode.HandleStyle.Auto;
                if (enableChange && node != null) {
                    switch (handlesMode) {
                        case 0: newHandleStyle = BezierNode.HandleStyle.Auto; break;
                        case 1: newHandleStyle = BezierNode.HandleStyle.Aligned; break;
                        case 2: newHandleStyle = BezierNode.HandleStyle.Free; break;
                        case 3: newHandleStyle = BezierNode.HandleStyle.None; break;
                    }
                }
                curveEditor.ChangeNodeHandleStyle (node, curveEditor.selectedNodeIndex, 
                    newHandleStyle, isLastNode);
                UpdateToolbar (curveEditor);
            }
        }

        public void RemoveNode () {
            if (curveEditor != null) {
                curveEditor.RemoveSelectedNodes ();
            }
        }

        public void EnableArrayConnect (bool enableLinkMode) {
            /*
            if (arrayEditor != null) {
                if (enableLinkMode) {
                    arrayEditor.editMode = SocketArrayEditor.EditMode.Connect;
                } else {
                    arrayEditor.editMode = SocketArrayEditor.EditMode.EditArray;
                }
            }
            */
        }

        #region Icons
        public static Texture2D selectionModeIcon = null;
        public static Texture2D addNodeModeIcon = null;
        public static Texture2D nodeSmoothIcon = null;
        public static Texture2D nodeAlignedIcon = null;
        public static Texture2D nodeBrokenIcon = null;
        public static Texture2D nodeNoneIcon = null;
        public static Texture2D removeNodeIcon = null;
        public static Texture2D pivotOriginIcon = null;
        public static Texture2D pivotCenterIcon = null;
        public static Texture2D pivotCenterBottomIcon = null;
        public static Texture2D pivotFirstIcon = null;
        public static Texture2D pivotLastIcon = null;
        public static void LoadIcons ()
        {
            int theme = EditorGUIUtility.isProSkin?0:32;
            selectionModeIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 5, theme, 32, 32);
            addNodeModeIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 6, theme, 32, 32);
            nodeSmoothIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 0, theme, 32, 32);
            nodeAlignedIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 1, theme, 32, 32);
            nodeBrokenIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 2, theme, 32, 32);
            nodeNoneIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 3, theme, 32, 32);
            removeNodeIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 4, theme, 32, 32);
            pivotOriginIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 7, theme, 32, 32);
            pivotCenterIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 8, theme, 32, 32);
            pivotCenterBottomIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 9, theme, 32, 32);
            pivotFirstIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 10, theme, 32, 32);
            pivotLastIcon = BezierCurveEditor.LoadSprite ("bezier_canvas_GUI_32", 32 * 11, theme, 32, 32);
        }
        #endregion
    }

    /// <summary>
    /// Bezier Curve Toolbar Edit Mode Selector.
    /// 1. Selection Mode: select Curves and nodes for edition.
    /// 2. Add Mode: add nodes to Curves.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    class BezierCurveToolbarEditMode : EditorToolbarDropdown
    {
        #region Vars
        public const string id = "Bezier Curve Toolbar/Edit Mode";
        public const string selectModeText = "Selection";
        public const string addModeText = "Add Mode";
        public const string selectModeTooltip = "Selection Mode. Edition Mode to select Curves and Nodes for edition.";
        public const string addModeTooltip = "Add Mode. Edition Mode to add Nodes to Curves.";
        public enum EditMode {
            Selection,
            AddNode
        }
        public BezierCurveToolbar curveToolbar = null;
        private EditMode _editMode = EditMode.Selection;
        public EditMode editMode {
            get {
                return _editMode; 
            }
            set {
                _editMode = value;
                if (_editMode == EditMode.Selection) {
                    text = selectModeText;
                    tooltip = selectModeTooltip;
                    if (curveToolbar != null) curveToolbar.SetEditModeToSelect ();
                } else {
                    text = addModeText;
                    tooltip = addModeTooltip;
                    if (curveToolbar != null) curveToolbar.SetEditModeToAdd ();
                }
                SetIcon (); 
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for this Toolbar Element.
        /// </summary>
        public BezierCurveToolbarEditMode () {
            text = selectModeText;
            tooltip = selectModeTooltip;
            SetIcon ();
            clicked += ShowDropdown;
        }
        #endregion

        #region Setup and Events
        /// <summary>
        /// Set the icon for this button.
        /// </summary>
        public void SetIcon () {
            if (BezierCurveToolbar.selectionModeIcon == null) BezierCurveToolbar.LoadIcons ();
            if (_editMode == EditMode.Selection) {
                icon = BezierCurveToolbar.selectionModeIcon;
            } else {
                icon = BezierCurveToolbar.addNodeModeIcon;
            }
        }
        /// <summary>
        /// Sets the style for this button according to the layout type.
        /// </summary>
        /// <param name="layout">Type of layout.</param>
        public void SetLayout (UnityEditor.Overlays.Layout layout) {
            if (layout == UnityEditor.Overlays.Layout.VerticalToolbar) {
                this.style.marginTop = new StyleLength (1.5f);
                this.style.marginBottom = new StyleLength (1.5f);
            }
        }
        /// <summary>
        /// Function to show the dropdown for edition modes.
        /// </summary>
        void ShowDropdown() {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Selection"), _editMode == EditMode.Selection, () => { 
                editMode = EditMode.Selection;
            });
            menu.AddItem(new GUIContent("Add Node"), _editMode == EditMode.AddNode, () => {
                editMode = EditMode.AddNode;
            });
            menu.ShowAsContext();
        }
        #endregion
    }
    
    /// <summary>
    /// Button to set a selected Node handle style to smooth.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    class BezierCurveToolbarSmoothHandles : EditorToolbarToggle
    {
        #region Vars
        public const string id = "Bezier Curve Toolbar/Smooth Handles";
        public BezierCurveToolbar curveToolbar = null;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for this button.
        /// </summary>
        public BezierCurveToolbarSmoothHandles ()
        {
            text = "Smooth";
            tooltip = "Smooth Node Handles. Node handles aligned in parallel and at the same distance of their node.";
            this.RegisterValueChangedCallback (OnToggleChange);
            SetIcon ();
        }
        #endregion

        #region Setup and Events
        /// <summary>
        /// Set the icon for this button.
        /// </summary>
        public void SetIcon () {
            if (BezierCurveToolbar.nodeSmoothIcon == null) BezierCurveToolbar.LoadIcons ();
            icon = BezierCurveToolbar.nodeSmoothIcon;
        }
        /// <summary>
        /// Sets the style for this button according to the layout type.
        /// </summary>
        /// <param name="layout">Type of layout.</param>
        public void SetLayout (UnityEditor.Overlays.Layout layout) {
            if (layout == UnityEditor.Overlays.Layout.HorizontalToolbar) {
                this.style.marginRight = new StyleLength (0.5f);
            } else if (layout == UnityEditor.Overlays.Layout.VerticalToolbar) {
                this.style.marginTop = new StyleLength (1.5f);
                this.style.marginBottom = new StyleLength (0.5f);
            }
        }
        /// <summary>
        /// Event to call when this button toggles.
        /// </summary>
        /// <param name="evt">Toggle value.</param>
        void OnToggleChange (ChangeEvent<bool> evt) {
            if (evt.newValue && curveToolbar != null) {
                curveToolbar.SetHandlesMode (0);
            } else {
                SetValueWithoutNotify (true);
            }
        }
        #endregion
    }

    /// <summary>
    /// Button to set a selected Node handle style to aligned.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    class BezierCurveToolbarAlignedHandles : EditorToolbarToggle
    {
        #region Vars
        public const string id = "Bezier Curve Toolbar/Aligned Handles";
        public BezierCurveToolbar curveToolbar = null;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for this button.
        /// </summary>
        public BezierCurveToolbarAlignedHandles ()
        {
            text = "Aligned";
            tooltip = "Aligned Node Handles. Node handles aligned in parallel and allowed to have different distance between them.";
            this.RegisterValueChangedCallback (OnToggleChange);
            SetIcon ();
        }
        #endregion

        #region Setup and Events
        /// <summary>
        /// Set the icon for this button.
        /// </summary>
        public void SetIcon () {
            if (BezierCurveToolbar.nodeAlignedIcon == null) BezierCurveToolbar.LoadIcons ();
                icon = BezierCurveToolbar.nodeAlignedIcon;
        }
        /// <summary>
        /// Sets the style for this button according to the layout type.
        /// </summary>
        /// <param name="layout">Type of layout.</param>
        public void SetLayout (UnityEditor.Overlays.Layout layout) {
            if (layout == UnityEditor.Overlays.Layout.HorizontalToolbar) {
                this.style.marginLeft = new StyleLength (0f);
                this.style.marginRight = new StyleLength (0.5f);
            } else if (layout == UnityEditor.Overlays.Layout.VerticalToolbar) {
                this.style.marginTop = new StyleLength (0f);
                this.style.marginBottom = new StyleLength (0.5f);
            }
        }
        /// <summary>
        /// Event to call when this button toggles.
        /// </summary>
        /// <param name="evt">Toggle value.</param>
        void OnToggleChange (ChangeEvent<bool> evt) {
            if (evt.newValue && curveToolbar != null) {
                curveToolbar.SetHandlesMode (1);
            } else {
                SetValueWithoutNotify (true);
            }
        }
        #endregion
    }

    /// <summary>
    /// Button to set a selected Node handle style to free.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    class BezierCurveToolbarFreeHandles : EditorToolbarToggle
    {
        #region Vars
        public const string id = "Bezier Curve Toolbar/Free Handles";
        public BezierCurveToolbar curveToolbar = null;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for this button.
        /// </summary>
        public BezierCurveToolbarFreeHandles ()
        {
            text = "Free";
            tooltip = "Free Node Handles. Node handles free to move independently from each other.";
            this.RegisterValueChangedCallback (OnToggleChange);
            SetIcon ();
        }
        #endregion

        #region Setup and Events
        /// <summary>
        /// Set the icon for this button.
        /// </summary>
        public void SetIcon () {
            if (BezierCurveToolbar.nodeBrokenIcon == null) BezierCurveToolbar.LoadIcons ();
                icon = BezierCurveToolbar.nodeBrokenIcon;
        }
        /// <summary>
        /// Sets the style for this button according to the layout type.
        /// </summary>
        /// <param name="layout">Type of layout.</param>
        public void SetLayout (UnityEditor.Overlays.Layout layout) {
            if (layout == UnityEditor.Overlays.Layout.HorizontalToolbar) {
                this.style.marginLeft = new StyleLength (0f);
                this.style.marginRight = new StyleLength (0.5f);
            } else if (layout == UnityEditor.Overlays.Layout.VerticalToolbar) {
                this.style.marginTop = new StyleLength (0f);
                this.style.marginBottom = new StyleLength (0.5f);
            }
        }
        /// <summary>
        /// Event to call when this button toggles.
        /// </summary>
        /// <param name="evt">Toggle value.</param>
        void OnToggleChange (ChangeEvent<bool> evt) {
            if (evt.newValue && curveToolbar != null) {
                curveToolbar.SetHandlesMode (2);
            } else {
                SetValueWithoutNotify (true);
            }
        }
        #endregion
    }

    /// <summary>
    /// Button to set a selected Node handle style to none.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    class BezierCurveToolbarNoHandles : EditorToolbarToggle
    {
        #region Vars
        public const string id = "Bezier Curve Toolbar/No Handles";
        public BezierCurveToolbar curveToolbar = null;
        #endregion

        #region Constructor
        /// <summary>
        /// Set the icon for this button.
        /// </summary>
        public BezierCurveToolbarNoHandles ()
        {
            text = "None";
            tooltip = "No Node Handles. The node has no handles, making them a hard angle on the Unit path.";
            this.RegisterValueChangedCallback (OnToggleChange);
            SetIcon ();
        }
        #endregion

        #region Setup and Events
        /// <summary>
        /// Set the icon for this button.
        /// </summary>
        public void SetIcon () {
            if (BezierCurveToolbar.nodeNoneIcon == null) BezierCurveToolbar.LoadIcons ();
                icon = BezierCurveToolbar.nodeNoneIcon;
        }
        /// <summary>
        /// Sets the style for this button according to the layout type.
        /// </summary>
        /// <param name="layout">Type of layout.</param>
        public void SetLayout (UnityEditor.Overlays.Layout layout) {
            if (layout == UnityEditor.Overlays.Layout.HorizontalToolbar) {
                this.style.marginLeft = new StyleLength (0f);
            } else if (layout == UnityEditor.Overlays.Layout.VerticalToolbar) {
                this.style.marginTop = new StyleLength (0f);
                this.style.marginBottom = new StyleLength (1.5f);
            }
        }
        /// <summary>
        /// Event to call when this button toggles.
        /// </summary>
        /// <param name="evt">Toggle value.</param>
        void OnToggleChange (ChangeEvent<bool> evt) {
            if (evt.newValue && curveToolbar != null) {
                curveToolbar.SetHandlesMode (3);
            } else {
                SetValueWithoutNotify (true);
            }
        }
        #endregion
    }

    /// <summary>
    /// Button to remove a node.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    class BezierCurveToolbarRemoveNode : EditorToolbarButton
    {
        #region Vars
        public const string id = "Bezier Curve Toolbar/Remove Node";
        public BezierCurveToolbar curveToolbar = null;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for this button.
        /// </summary>
        public BezierCurveToolbarRemoveNode()
        {
            text = "Remove Node";
            tooltip = "Removes a single selected non-terminal node.";
            clicked += OnClick;
            SetIcon ();
        }
        #endregion

        #region Setup and Events
        /// <summary>
        /// Set the icon for this button.
        /// </summary>
        public void SetIcon () {
            if (BezierCurveToolbar.removeNodeIcon == null) BezierCurveToolbar.LoadIcons ();
                icon = BezierCurveToolbar.removeNodeIcon;
        }
        /// <summary>
        /// Sets the style for this button according to the layout type.
        /// </summary>
        /// <param name="layout">Type of layout.</param>
        public void SetLayout (UnityEditor.Overlays.Layout layout) {
            if (layout == UnityEditor.Overlays.Layout.VerticalToolbar) {
                this.style.marginTop = new StyleLength (1.5f);
                this.style.marginBottom = new StyleLength (1.5f);
            }
        }
        /// <summary>
        /// Event to call when the button is clicked.
        /// </summary>
        void OnClick() {
            if (curveToolbar != null) {
                if (EditorUtility.DisplayDialog ("Delete node",
                    "Do you really want to remove this node?", "Yes", "No")) {
                    curveToolbar.RemoveNode ();
                }
                GUIUtility.ExitGUI();
            }
        }
        #endregion
    }

    /// <summary>
    /// Button to switch the curve editor to a mode suited to connect nodes to arrays.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    class BezierCurveToolbarLinkNode : EditorToolbarToggle
    {
        #region Vars
        public const string id = "Bezier Curve Toolbar/Link Node";
        public BezierCurveToolbar curveToolbar = null;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for this button.
        /// </summary>
        public BezierCurveToolbarLinkNode ()
        {
            text = "Link Node";
            tooltip = "Enables mode to drag Nodes to connect/disconnect them to/from Array Sockets.";
            this.RegisterValueChangedCallback (OnToggleChange);
            SetIcon ();
        }
        #endregion

        #region Setup and Events
        /// <summary>
        /// Set the icon for this button.
        /// </summary>
        public void SetIcon () {
            //icon = BezierCurveToolbar.textureManager.GetLinkNodeIcon ();
        }
        /// <summary>
        /// Sets the style for this button according to the layout type.
        /// </summary>
        /// <param name="layout">Type of layout.</param>
        public void SetLayout (UnityEditor.Overlays.Layout layout) {
            if (layout == UnityEditor.Overlays.Layout.VerticalToolbar) {
                this.style.marginTop = new StyleLength (1.5f);
                this.style.marginBottom = new StyleLength (1.5f);
            }
        }
        /// <summary>
        /// Event to call when this button toggles.
        /// </summary>
        /// <param name="evt">Toggle value.</param>
        void OnToggleChange (ChangeEvent<bool> evt) {
            if (curveToolbar != null) {
                curveToolbar.EnableArrayConnect (evt.newValue);
            }
        }
        #endregion
    }

    /// <summary>
    /// Bezier Curve Toolbar Pivot Mode Selector.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    class BezierCurveToolbarPivotMode : EditorToolbarDropdown
    {
        #region Vars
        public const string id = "Bezier Curve Toolbar/Pivot Mode";
        public const string originModeText = "Origin Pivot";
        public const string centerModeText = "Center Pivot";
        public const string centerBottomModeText = "CenterBottom Pivot";
        public const string firstNodeModeText = "First Node Pivot";
        public const string lastNodeModeText = "Last Node Pivot";
        public const string originModeTooltip = "Pivot to the origin of the curves.";
        public const string centerModeTooltip = "Pivot to the center of the node selection.";
        public const string centerBottomModeTooltip = "Pivot to the center and bottom of the node selection.";
        public const string firstNodeModeTooltip = "Pivot to the first selected Node position.";
        public const string lastNodeModeTooltip = "Pivot to the last selected Node position.";

        public enum PivotMode {
            Origin = 0,
            Center = 1,
            CenterBottom = 2,
            FirstNode = 3,
            LastNode = 4
        }
        public BezierCurveToolbar curveToolbar = null;
        private PivotMode _pivotMode = PivotMode.Origin;
        public PivotMode pivotMode {
            get {
                return _pivotMode; 
            }
            set {
                _pivotMode = value;
                switch (_pivotMode) {
                    case PivotMode.Origin:
                        text = originModeText;
                        tooltip = originModeTooltip;
                        if (curveToolbar != null && curveToolbar.curveEditor != null)
                            curveToolbar.curveEditor.SetPivotToOrigin ();
                        break;
                    case PivotMode.Center:
                        text = centerModeText;
                        tooltip = centerModeTooltip;
                        if (curveToolbar != null && curveToolbar.curveEditor != null)
                            curveToolbar.curveEditor.SetPivotToSelectionCenter ();
                        break;
                    case PivotMode.CenterBottom:
                        text = centerBottomModeText;
                        tooltip = centerBottomModeTooltip;
                        if (curveToolbar != null && curveToolbar.curveEditor != null)
                            curveToolbar.curveEditor.SetPivotToSelectionCenterBottom ();
                        break;
                    case PivotMode.FirstNode:
                        text = firstNodeModeText;
                        tooltip = firstNodeModeTooltip;
                        if (curveToolbar != null && curveToolbar.curveEditor != null)
                            curveToolbar.curveEditor.SetPivotToFirstNode ();
                        break;
                    case PivotMode.LastNode:
                        text = lastNodeModeText;
                        tooltip = lastNodeModeTooltip;
                        if (curveToolbar != null && curveToolbar.curveEditor != null)
                            curveToolbar.curveEditor.SetPivotToLastNode ();
                        break;
                }
                SetIcon (); 
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for this Toolbar Element.
        /// </summary>
        public BezierCurveToolbarPivotMode () {
            text = originModeText;
            tooltip = originModeTooltip;
            SetIcon ();
            clicked += ShowDropdown;
        }
        #endregion

        #region Setup and Events
        /// <summary>
        /// Set the icon for this button.
        /// </summary>
        public void SetIcon () {
            if (BezierCurveToolbar.pivotOriginIcon == null) BezierCurveToolbar.LoadIcons ();
            switch (_pivotMode) {
                case PivotMode.Origin:
                    icon = BezierCurveToolbar.pivotOriginIcon;
                    break;
                case PivotMode.Center:
                    icon = BezierCurveToolbar.pivotCenterIcon;
                    break;
                case PivotMode.CenterBottom:
                    icon = BezierCurveToolbar.pivotCenterBottomIcon;
                    break;
                case PivotMode.FirstNode:
                    icon = BezierCurveToolbar.pivotFirstIcon;
                    break;
                case PivotMode.LastNode:
                    icon = BezierCurveToolbar.pivotLastIcon;
                    break;
            }
        }
        /// <summary>
        /// Sets the style for this button according to the layout type.
        /// </summary>
        /// <param name="layout">Type of layout.</param>
        public void SetLayout (UnityEditor.Overlays.Layout layout) {
            if (layout == UnityEditor.Overlays.Layout.VerticalToolbar) {
                this.style.marginTop = new StyleLength (1.5f);
                this.style.marginBottom = new StyleLength (1.5f);
            }
        }
        /// <summary>
        /// Function to show the dropdown for edition modes.
        /// </summary>
        void ShowDropdown() {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Pivot to Origin"), _pivotMode == PivotMode.Origin, () => { 
                pivotMode = PivotMode.Origin;
            });
            menu.AddItem(new GUIContent("Pivot to Selection Center"), _pivotMode == PivotMode.Center, () => { 
                pivotMode = PivotMode.Center;
            });
            menu.AddItem(new GUIContent("Pivot to Selection Center Bottom"), _pivotMode == PivotMode.CenterBottom, () => { 
                pivotMode = PivotMode.CenterBottom;
            });
            menu.AddItem(new GUIContent("Pivot to First Selected Node"), _pivotMode == PivotMode.FirstNode, () => { 
                pivotMode = PivotMode.FirstNode;
            });
            menu.AddItem(new GUIContent("Pivot to Last Selected Node"), _pivotMode == PivotMode.LastNode, () => { 
                pivotMode = PivotMode.LastNode;
            });
            menu.ShowAsContext();
        }
        #endregion
    }
}
#endif