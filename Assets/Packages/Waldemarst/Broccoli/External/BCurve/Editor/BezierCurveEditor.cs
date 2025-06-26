using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEditor.EditorTools;

using Broccoli.Model;
using System;
using UnityEditor.Graphs;

using UnityEditor.Overlays;
using UnityEditor.Toolbars;

namespace Broccoli.Utils
{
    /// <summary>
    /// Bezier Curves Editor.
    /// The editor displays controls to maniputale a curve on the Unity Editor.
    /// The edition toolbar changes for versions greater than 2020.3.
    /* 
    EVENTS
    Order is onValidateAction, onBeforeAction, onAction
    onValidateAction: stops or continues the propagation of events.
    onBeforeAction: the action passed and is about to be applied.
                    Useful for undo: Undo.RecordObject (objectToRecord, "Add Node");
    onAction: action has been applied to target curve or node.
              Should call serialize target object and update views (undo or not implemented).

    EVENTS LIST
    onEditModeChanged   The editor mode has changed.

    # CURVE EDITING
    onBeforeChangeCurves    Called before a curve or curves have been modified.
    onChangeCurves          Called when a curve or curves have been modified.
    onRegisterCurve         Called when a curve has been registered to be managed by this editor.
    onDeregisterCurve       Called when a curve has been deregistered from management by this editor.

    # NODE EDITING
    onCheckDragNodes    Validates allowed drag offsets for selected nodes (restricting movement).
    onBeginDragNodes    Event to raise when a selection of nodes begin dragging.
    onBeforeDragNodes   Event before dragging nodes.
    onDragNodes         Event after dragging nodes.
    onEndRotateNodes      Event to raise when a selection of nodes lose focus after having been rotated.
    onBeginRotateNodes    Event to raise when a selection of nodes begin rotating.
    onBeforeRotateNodes   Event before rotating nodes.
    onRotateNodes         Event after rotating nodes.
    onEndDragNodes      Event to raise when a selection of nodes lose focus after having been dragged.
    onValidateAddNode      Returns true if a point can be added.
    *onBeforeAddNode        Event to call before adding a node.
    *onAddNode              Event to call after a node has been added.
    *onCheckRemoveNodes     Validates if nodes can be removed.
    *onBeforeRemoveNodes    Event before moving nodes.
    *onRemoveNodes          Event after moving nodes.
    onBeforeEditNodeStyle  Event before editing a node style.
    onEditNodeStyle        Event after editing a node style.
    *onBeforeEditNode       Event before editing a node properties (ex. node handle style).
    *onEditNode             Event after editing a node properties.

    # NODE SELECTION
    onCheckSelectCmd    Event to receive a command on how to handle a newly selected node.
    onSelectionChanged  The selection of nodes has changed.
    onSelectNode        Event to call when a node is clicked.
    onDeselectNode      Event to call when a node is deselected.
    onSelectHandle      Event to call when a handle has been selected.
    OnDeselectHandle    Event to call when a handle has been deselected.
    
    # HANDLES EDITING
    onBeginDragHandle   Event to raise when a selected handle begins moving.
    onBeforeDragHandle  Event before moving node handles.
    onDragHandle        Event after moving node handles.
    onEndDragHandle     Event to raise whan a selected handle lost focus after having moved.

    # PIVOT METHODS
    onBeforeSetPivot    Event to call before setting the pivot for rotation and scaling operations.
    onPivotSet          Event to call when the pivot has been set for rotation and scaling operations.

    # DRAWING
    *onCheckDrawNode         Validates if a point in a curve should be drawn (with controls or not) or not.
    *onCheckNodeControls     Validates drawing a moving gizmo on a node.
    *onCheckDrawFirstHandle  Validates if the first handle of a node should be drawn.
    *onCheckDrawSecondHandle Validates if the second handle of a node should be drawn.
    *onDrawToolbar           Called after the on-scene toolbar has been drawn.
    */
    /// </summary>
    public class BezierCurveEditor { 
        #region Style Vars
        /// <summary>
		/// Color for the curve.
		/// </summary>
		public Color curveColor = Color.white;
        /// <summary>
        /// Color for the curve when selected.
        /// </summary>
        public Color selectedCurveColor = Color.gray;
        /// <summary>
        /// Color for the curve when disabled.
        /// </summary>
        public Color disabledCurveColor = new Color (1f, 1f, 1f, 0.4f);
		/// <summary>
		/// Width for the curve.
		/// </summary>
		public float curveWidth = 1f;
        /// <summary>
		/// Width for the curve when selected.
		/// </summary>
		public float selectedCurveWidth = 2f;
		/// <summary>
		/// Resolution of the curve.
		/// </summary>
		public float curveResolution = 8f;
        public Color nodeColor = new Color (0.8f, 0.8f, 0.8f, 1f);
        public Color selectedNodeColor = new Color (1f, 1f, 1f, 1f);
        public Color nodeHandleColor = new Color (0.8f, 0.8f, 0.8f, 1f);
        public Color selectedNodeHandleColor = new Color (1f, 1f, 1f, 1f);
        public Color nodeHandleLineColor = new Color (0.8f, 0.8f, 0.8f, 1f);
        public Color preselectedNodeColor = Color.yellow;
        public Color pivotColor = Color.Lerp (Color.yellow, Color.red, 0.5f);
        /// <summary>
        /// Size on extra small nodes.
        /// </summary>
        public float extraSmallNodeSize = 0.05f;
        /// <summary>
        /// Size on small nodes.
        /// </summary>
        public float smallNodeSize = 0.075f;
        /// <summary>
		/// Size of the node handle.
		/// </summary>
		public float nodeSize = 0.08f;
		/// <summary>
		/// Size of the bezier nodes handles handles.
		/// </summary>
		public float nodeHandleSize = 0.05f;
        /// <summary>
		/// Size of the pivot.
		/// </summary>
		public float pivotSize = 0.1f;
        /// <summary>
        /// Label size for the nodes.
        /// </summary>
        public float labelSize = 0.4f;
        public Handles.CapFunction nodeDrawFunction = Handles.RectangleHandleCap;
        public Handles.CapFunction selectedNodeDrawFunction = Handles.DotHandleCap;
        /// <summary>
        /// Function used to draw bezier node handlers.
        /// </summary>
        public Handles.CapFunction nodeHandleDrawFunction = Handles.RectangleHandleCap;
        public Handles.CapFunction selectedNodeHandleDrawFunction = Handles.DotHandleCap;
        /// <summary>
        /// Temporary variable to save the handles color.
        /// </summary>
        private Color _tmpColor = Color.white;
        Texture2D pivotOrigin;
        Texture2D pivotCenter;
        Texture2D pivotCenterBottom;
        Texture2D pivotFirst;
        Texture2D pivotLast;
        #endregion

        #region Mode and Settings Vars
        /// <summary>
		/// <c>True</c> if the the editor allows to select multiple nodes.
		/// </summary>
		public bool multiselectEnabled = true;
        /// <summary>
        /// <c>True</c> to allow deleting the first or the final node of a curve.
        /// </summary>
        bool deleteTerminalNodesEnabled = false;
        /// <summary>
        /// Scale to draw and edit this curve.
        /// </summary>
        public float scale = 1f;
        /// <summary>
		/// Modes available for the editor.
		/// </summary>
		public enum EditMode {
            Show,
			Selection,
			Add,
            Custom
		}
        /// <summary>
		/// Current editor mode.
		/// </summary>
		private EditMode _editMode = EditMode.Selection;
        /// <summary>
        /// Current editor mode.
        /// </summary>
        /// <value>Editor mode from the enum.</value>
        public EditMode editMode {
            get { return _editMode; }
            set {
                if (_editMode != value) {
                    _editMode = value;
                    onEditModeChanged?.Invoke(value);
                    if (debugEditorEvents) Debug.Log ($"onEditModeChanged: {_editMode}" );
                }
            }
        }
        /// <summary>
        /// Flag to allow rotation of selected points.
        /// </summary>
        public bool rotateEnabled = true;
        /// <summary>
        /// Flag to allow scaling of selected points.
        /// </summary>
        public bool scaleEnabled = true;
        /// <summary>
        /// Flag to show the pivot point for rotate and scale operations.
        /// </summary>
        public bool showPivot = true;
        /// <summary>
        /// If set to <c>true</c>, then moving nodes is always set to ray drag mode.
        /// </summary>
        public bool rayDragEnabled = false;
        /// <summary>
        /// True to display handles to edit this curve.
        /// </summary>
        public bool showHandles = true;
        /// <summary>
        /// True to show the first handle on each node.
        /// Usually this handle is not drawn at the first node of the curve.
        /// </summary>
        public bool showFirstHandleAlways = false;
        /// <summary>
        /// True to show the second handle on each node.
        /// Usually this handle is not drawn at the last node of the curve.
        /// </summary>
        public bool showSecondHandleAlways = false;
        /// <summary>
        /// Relative position on the curve to restrict the addition of points.
        /// </summary>
        public float addNodeLowerLimit = 0f;
        /// <summary>
        /// Relative position on the curve to restrict the addition of points.
        /// </summary>
        public float addNodeUpperLimit = 1f;
        /// <summary>
        /// Commands to handle selection.
        /// </summary>
        public enum SelectionCommand {
            DoNotSelect,
            Select,
            SingleSelect
        }
        /// <summary>
        /// Pivot position.
        /// </summary>
        public Vector3 Pivot { get; internal set; } = Vector3.zero;
        /// <summary>
        /// Pivot position mode.
        /// </summary>
        public enum PivotMode {
            Origin = 0,
            Center = 1,
            CenterBottom = 2,
            FirstNode = 3,
            LastNode = 4,
            Node = 5,
            Free = 6
        }
        /// <summary>
        /// Pivot position mode.
        /// </summary>
        public PivotMode pivotMode = PivotMode.Origin;
        #endregion

        #region Debug Vars
        /// <summary>
        /// Global flag to enable debug to gizmos.
        /// </summary>
        public bool debugEnabled = false;
        /// <summary>
        /// Draw the sample points of the curve.
        /// </summary>
        public bool debugDrawSampleLine = false;
        /// <summary>
        /// Enabled to show curve nodes on the curve.
        /// </summary>
        public bool debugShowNodes = false;
        /// <summary>
        /// Enabled to show curve points on the curve.
        /// </summary>
        public bool debugShowPoints = false;
        /// <summary>
        /// Flag to show fine curve points (samples) on the curve.
        /// </summary>
        public bool debugShowFinePoints = false;
        /// <summary>
        /// Flag to show a custom point in the curve.
        /// </summary>
        public bool debugShowCustomPoint = false;
        /// <summary>
        /// Relative position of the point to display in the curve.
        /// </summary>
        public float debugCustomPointPosition = 0f;
        /// <summary>
        /// Custom point to debug display.
        /// </summary>
        private CurvePoint debugCustomPoint = null;
        /// <summary>
        /// Flag to show labels next to each node on the curve.
        /// </summary>
        public bool debugShowNodeLabels = false;
        /// <summary>
        /// Flag to show an arrow pointing at the forward vector of each point.
        /// </summary>
        public bool debugShowPointForward = false;
        /// <summary>
        /// Flag to show an arrow pointing at the forward vector of each point.
        /// </summary>
        public bool debugShowPointNormal = false;
        /// <summary>
        /// Flag to show an arrow pointing at the up vector of each point.
        /// </summary>
        public bool debugShowPointUp = false;
        /// <summary>
        /// Flag to show an arrow pointing at the tangent vector of each point.
        /// </summary>
        public bool debugShowPointTangent = false;
        /// <summary>
        /// Flag to display the GUID assigned to a node in the curve.
        /// </summary>
        public bool debugShowNodeGUID = false;
        /// <summary>
        /// Flag to display the relative position of nodes.
        /// </summary>
        public bool debugShowRelativePos = false;
        /// <summary>
        /// Labels for nodes color.
        /// </summary>
        public Color nodeLabelColor = Color.gray;
        #endregion

        #region Vars
        /// <summary>
		/// Curve on focus from the selection.
		/// </summary>
		protected BezierCurve _curve;
		/// <summary>
		/// Accessor to the curve on focus.
		/// </summary>
		public BezierCurve curve {
			get {
				return _curve;
			}
			set {
				_curve = value;
				UpdateSelection ();
			}
		}
        /// <summary>
        /// Id of the curve currently on focus.
        /// </summary>
        public System.Guid curveId = System.Guid.Empty;
        /// <summary>
        /// List of curves registered to be handled by this editor.
        /// </summary>
        protected List<BezierCurve> _curves = new List<BezierCurve>();
        /// <summary>
        /// Type of control used on the nodes.
        /// </summary>
        public enum ControlType {
            Move,
            Rotate,
            Scale,
            DrawSelectable,
            DrawOnly,
            None
        }
        /// <summary>
        /// Vector to use on a node slider control.
        /// </summary>
        public Vector3 sliderVector = Vector3.up;
        Vector3 lastKnownOffset = Vector3.zero;
        public Vector3 offsetStep = Vector3.zero;
        /// <summary>
        /// Flag that marks the need to update data for a focused curve.
        /// </summary>
        private bool shouldUpdateFocusedData = false;
        /// <summary>
        /// The focused curve Guid.
        /// </summary>
        private System.Guid _focusedCurveId = System.Guid.Empty;
        /// <summary>
        /// Assign the focused curve id.
        /// </summary>
        public System.Guid focusedCurveId {
            get { return _focusedCurveId; }
            set {
                _focusedCurveId = value;
                shouldUpdateFocusedData = true;
            }
        }
        #endregion

        #region Selection Vars
        /// <summary>
		/// Flag to turn when a selection of a node happened.
		/// </summary>
		private bool selectionHappened = false;
		/// <summary>
		/// Selected nodes.
		/// </summary>
		private List<BezierNode> _selectedNodes = new List<BezierNode> ();
        /// <summary>
		/// Selected node index;
		/// </summary>
		private List<int> _selectedNodesIndex = new List<int> ();
        /// <summary>
        /// Selected curve ids.
        /// </summary>
        private List<System.Guid> _selectedCurveIds = new List<System.Guid> ();
        /// <summary>
        /// Relatonship between selected nodes and their ids.
        /// </summary>
        /// <typeparam name="Guid">Id of the bezier node.</typeparam>
        /// <typeparam name="BezierNode">Bezier node.</typeparam>
        /// <returns>Id to bezier node relationship.</returns>
        private Dictionary<System.Guid, BezierNode> _idToNode = new Dictionary<System.Guid, BezierNode> ();
        /// <summary>
        /// Node id to curve id.
        /// </summary>
        /// <typeparam name="Guid">Node id.</typeparam>
        /// <typeparam name="int">Curve id.</typeparam>
        /// <returns>Node id to curve id relationship.</returns>
        private Dictionary<System.Guid, System.Guid> _nodeToCurve = new Dictionary<System.Guid, System.Guid> ();
        public Dictionary<System.Guid, System.Guid> nodeToCurve {
            get { return _nodeToCurve; }
        }
		/// <summary>
		/// Single selected node or the first on a multiselection.
		/// </summary>
		/// <value>Single node selected.</value>
		public BezierNode selectedNode {
			get {
				if (_selectedNodes.Count > 0)
					return _selectedNodes[0];
				return null;	
			}
		}
		/// <summary>
		/// Single selected node index or the first index in the selection.
		/// </summary>
		/// <value>Single selected node index.</value>
		public int selectedNodeIndex {
			get {
				if (_selectedNodesIndex.Count > 0)
					return _selectedNodesIndex[0];
				return -1;
			}
		}
        /// <summary>
        /// Single selected curve or the first curve id in the selection.
        /// </summary>
        /// <value>Single selected curve.</value>
        public System.Guid selectedCurveId {
            get {
				if (_selectedCurveIds.Count > 0)
					return _selectedCurveIds[0];
				return System.Guid.Empty;
			}
        }
        /*
        /// <summary>
        /// Return the id of the curve the first selected node belongs to.
        /// </summary>
        /// <value>Id of the curve the node belongs to, otherwise -1.</value>
        public int selectedNodeCurveId {
            get {
                if (_selectedNodes.Count > 0 && _nodeToCurve.ContainsKey(_selectedNodes[0].guid)) {
                    return _nodeToCurve[_selectedNodes[0].guid];
                }
                return -1;
            }
        }
        */
		/// <summary>
		/// Selected nodes.
		/// </summary>
		/// <value>Selected nodes list.</value>
		public List<BezierNode> selectedNodes {
			get { return _selectedNodes; }
		}
		/// <summary>
		/// Selected nodes indexes.
		/// </summary>
		/// <value>Selected nodes indexes for the selection.</value>
		public List<int> selectedNodesIndex {
			get { return _selectedNodesIndex; }
		}
        /// <summary>
        /// Selected curve ids.
        /// </summary>
        /// <value>Selected curve ids for the selection.</value>
        public List<System.Guid> selectedCurveIds {
            get { return _selectedCurveIds; }
        }
		/// <summary>
		/// Checks if one or more nodes are selected.
		/// </summary>
		/// <value><c>True</c> if there is a selection.</value>
		public bool hasSelection {
			get {
				if (_selectedNodes.Count > 0)
					return true;
				return false;
			}
		}
		/// <summary>
		/// Checks if only one node in the curve is selected.
		/// </summary>
		/// <value><c>True</c> with a single node selected.</value>
		public bool hasSingleSelection {
			get {
				if (_selectedNodes.Count == 1)
					return true;
				return false;
			}
		}
        /// <summary>
        /// Checks first if there is a single node being selected,
        /// then checks if it is a terminal node in the curve.
        /// </summary>
        /// <value></value>
        public bool hasSingleSelectionTerminalNode {
            get {
                if (_selectedNodes.Count == 1)
					if (_selectedNodes[0].relativePosition == 1f ||
                        _selectedNodes[1].relativePosition == 0f)
                        return true;
				return false;
            }
        }
        public bool hasMultipleSelection {
            get {
                if (_selectedNodes.Count > 1)
                    return true;
                return false;
            }
        }
        #endregion

        #region Handlers Vars
        /// <summary>
        /// Temporary variable used to draw nodes.
        /// </summary>
        private static Vector3 s_tmpPos;
        /// <summary>
        /// Temporary variable used to draw the first handle on bezier nodes.
        /// </summary>
        private static Vector3 s_tmpHandle1;
        /// <summary>
        /// Temporary variable used to draw the second handle on bezier nodes.
        /// </summary>
        private static Vector3 s_tmpHandle2;
        private static bool s_tmpHandle1Drawn = true;
        private static bool s_tmpHandle2Drawn = true;
        int tmpHotControl = 0;
        Quaternion s_tmpPreviousRotation = Quaternion.identity;
        Vector3 s_tmpPreviousScale = Vector3.one;
        Vector3 scaleOffset = Vector3.one;
        /// <summary>
        /// Temporary variables for mouse positions.
        /// </summary>
        private static Vector2 s_StartMousePosition, s_CurrentMousePosition;
        /// <summary>
        /// Temporary variables for mouse dragging.
        /// </summary>
        private static Vector3 s_StartPosition;
        /// <summary>
        /// Temporary point variable.
        /// </summary>
        private static Vector3 s_curvePoint;
        /// <summary>
        /// Temporary ray structure.
        /// </summary>
        private static Ray s_curveRay;
        /// <summary>
        /// Plane from the center of the curve, loking at the camera.
        /// </summary>
        private static Plane s_curvePlane;
        /// <summary>
        /// Temporary curve bounds.
        /// </summary>
        private Bounds _curveBounds;
        /// <summary>
        /// Lower point to display the range to add points to the loaded curve.
        /// </summary>
        private Vector3 _addNodeLowerPoint;
        /// <summary>
        /// Upper point to display the range to add points to the loaded curve.
        /// </summary>
        private Vector3 _addNodeUpperPoint;
        /// <summary>
        /// Set to false when first selecting a handle, checking against this flag marks the first movement of the handle
        /// in order to call OnBeginMove events.
        /// </summary>
        private bool _selectedHandleHasMoved = false;
        /// <summary>
        /// Set to false when first selecting a node, checking against this flag marks the first movement of the node
        /// in order to call OnBeginMove events.
        /// </summary>
        private bool _selectedNodesMoved = false;
        /// <summary>
        /// Set to false when first selecting a pivot for rotation, checking against this flag marks the first rotation of
        /// a selection of nodes in order to call OnBeginRotate events.
        /// </summary>
        private bool _selectedNodesRotating = false;
        /// <summary>
        /// Set to false when first selecting a pivot for scaling, checking against this flag marks the first scaling of
        /// a selection of nodes in order to call OnBeginScale events.
        /// </summary>
        private bool _selectedNodesScaling = false;
        /// <summary>
        /// Helper var to save the state of a node for listening events.
        /// </summary>
        private bool _nodeListenEvents = false;
        #endregion

        #region Inspector GUI Vars
        /// <summary>
		/// If <c>true</c> shows the remove node(s) button if there are nodes selected.
		/// </summary>
		public bool removeNodeButtonEnabled = true;
		/// <summary>
		/// Array of GUI contents for the edit mode buttons.
		/// </summary>
		GUIContent[] editModeOptions;
		/// <summary>
		/// Saves the index for the edit mode.
		/// </summary>
		int editModeIndex = 0;
		/// <summary>
		/// Array of GUI contents for the node handle style buttons.
		/// </summary>
		GUIContent[] handleStyleOptions;
        /// <summary>
		/// Array of GUI contents for the modes in the editor using icons.
		/// </summary>
		GUIContent[] editorModeIconOptions;
        /// <summary>
		/// Array of GUI contents for the actions on nodes using icons.
		/// </summary>
		GUIContent[] nodeActionsIconOptions;
        /// <summary>
		/// Array of GUI contents for the node handle style buttons using icons.
		/// </summary>
		GUIContent[] handleStyleIconOptions;
        /// <summary>
        /// Array of GUI content for setting the pivot.
        /// </summary>
        GUIContent[] pivotActionsIconOptions;
		/// <summary>
		/// Saves the style index for a single selected node.
		/// </summary>
		int handleStyleIndex = -1;
		/// <summary>
		/// GUI content for the remove button.
		/// </summary>
		GUIContent removeNodeButtonContent;
        #endregion

        #region Toolbar
        /// <summary>
        /// Flag to show tool GUI buttons to edit the selected curve.
        /// </summary>
        protected bool showTools = false;
        /// <summary>
        /// Set to true to use the overlay toolbar, available from Unity 2022.1
        /// </summary>
        public bool enableOverlayToolbar = true;
        /// <summary>
        /// Flag to display the editor mode buttons in the toolbar as enabled.
        /// </summary>
        public bool enableToolbarModes = true;
        /// <summary>
        /// Flag to display the action buttons in the toolbar as enabled.
        /// </summary>
        public bool enableToolbarActions = true;
        /// <summary>
        /// Flag to display the node buttons in the toolbar as enabled.
        /// </summary>
        public bool enableToolbarNode = true;
        /// <summary>
        /// Offset for the toolbar on the x axis.
        /// </summary>
        public int toolbarXOffset = 0;
        /// <summary>
        /// Rect to draw the on-scene toolbar.
        /// </summary>
        private Rect toolbarContainer;
        #endregion

        #region Delegates
        /// <summary>
        /// The editor changed its mode.
        /// </summary>
        /// <param name="newEditMode">New mode the editor switched to.</param>
        public delegate void OnEditModeChanged (BezierCurveEditor.EditMode newEditMode);
        /// <summary>
        /// OnCurve delegate definition.
        /// </summary>
        public delegate void OnCurveDelegate (BezierCurve curve);
        /// <summary>
        /// OnCurves delegate definition.
        /// </summary>
        public delegate void OnCurvesDelegate (List<BezierCurve> curve);
        /// <summary>
        /// OnNode delegate definition.
        /// </summary>
        /// <param name="node">Node.</param>
        public delegate void OnNodeDelegate (BezierNode node);
        /// <summary>
        /// OnMultiNodeIndex delegate definition.
        /// </summary>
        /// <param name="nodes">List of nodes.</param>
        /// <param name="index">List of indexes.</param>
        /// <param name="curveIds">Ids of curves.</param>
        public delegate void OnMultiNodeIndexDelegate (List<BezierNode> nodes, List<int> index, List<System.Guid> curveIds);
        /// <summary>
        /// OnNodeIndex delegate definition.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <param name="index">Index on the curve.</param>
        public delegate void OnNodeIndexDelegate (BezierNode node, int index);
        /// <summary>
        /// OnNodeIndexPos delegate definition.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <param name="index">Index on the curve.</param>
        /// <param name="relativePos">Relative position of the point in the curve.</param>
        public delegate void OnNodeIndexPosDelegate (BezierNode node, int index, float relativePos);
        /// <summary>
        /// OnValidatePosition delegate definition.
        /// </summary>
        /// <param name="position">Validates if a position on the canvas is valid for a node.</param>
        /// <returns><c>True</c> if the position is valid.</returns>
        public delegate bool OnValidatePositionDelegate (Vector3 position);
        /// <summary>
        /// OnCheckPosition delegate definition.
        /// </summary>
        /// <param name="offset">Offset to check or modify.</param>
        /// <returns>Position.</returns>
        public delegate Vector3 OnCheckOffsetDelegate (Vector3 offset);
        /// <summary>
        /// OnCheckNodes delegate definition.
        /// </summary>
        /// <param name="nodes">Nodes to check.</param>
        /// <param name="indexes">Index of nodes to check.</param>
        /// <returns></returns>
        public delegate bool OnCheckNodesDelegate (List<BezierNode> nodes, List<int> indexes, List<System.Guid> curveIds);
        /// <summary>
        /// OnCheckNodeDelegate delegate definition.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <param name="index">Index of the node to check.</param>
        /// <param name="curveId">Id of the curve the node belongs to.</param>
        /// <returns>Boolean</returns>
        public delegate bool OnCheckNodeDelegate (BezierNode node, int index, System.Guid curveId);
        /// <summary>
        /// OnCheckNodeHandlerDelegate delegate definition.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <param name="index">Index of the node to check.</param>
        /// <param name="curveId">Id of the curve the node belongs to.</param>
        /// <param name="handle">Number of handle selected.</param>
        /// <returns>Boolean</returns>
        public delegate bool OnCheckNodeHandlerDelegate (BezierNode node, int index, System.Guid curveId, int handle);
        /// <summary>
        /// OnCheckNodeControlType delegate definition.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <param name="index">Index of the node to check.</param>
        /// <param name="curveId">Id of the curve the node belongs to.</param>
        /// <returns>Boolean</returns>
        public delegate ControlType OnCheckNodeControlType (BezierNode node, int index, System.Guid curveId);
        /// <summary>
        /// OnBeforeNodeMove delegate definition.
        /// </summary>
        /// <param name="node">Node to move.</param>
        /// <param name="previousPosition">Previous position.</param>
        /// <param name="newPosition">New position.</param>
        /// <param name="scale">Scale on the editor.</param>
        /// <param name="index">Index of the node to move.</param>
        /// <param name="curveId">Id of the curve the node belongs to.</param>
        /// <returns>The resulting offset to apply the move, without scale applied.</returns>
        public delegate Vector3 OnBeforeNodeMove (BezierNode node, Vector3 previousPosition, Vector3 newPosition, float scale, int index, System.Guid curveId);
        /// <summary>
        /// OnCheckSelectNode delegate definition.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <returns>A selection command.</returns>
        public delegate SelectionCommand OnCheckSelectNode (BezierNode node);
        /// <summary>
        /// Delegate for pivot operations.
        /// </summary>
        /// <param name="position">Position of the pivot.</param>
        /// <param name="pivotPosition">Pivot mode.</param>
        public delegate void OnPivotDelegate (Vector3 position, PivotMode pivotPosition);
        /// <summary>
        /// OnDrawEvent delegate definition.
        /// </summary>
        /// <param name="rect">Rect for the element drawn.</param>
        public delegate void OnDrawEvent (Rect rect);
        #endregion

        #region Events
        /// <summary>
        /// Delegate to call when the editor changes mode.
        /// </summary>
        public OnEditModeChanged onEditModeChanged;
        /// <summary>
        /// Delegate to call before a change has been made to some curves.
        /// </summary>
        public OnCurvesDelegate onBeforeChangeCurves;
        /// <summary>
        /// Delegate to call when a change has been made to some curves.
        /// </summary>
        public OnCurvesDelegate onChangeCurves;
        /// <summary>
        /// Delegate to call when a curve has been registered to be managed.
        /// </summary>
        public OnCurveDelegate onRegisterCurve;
        /// <summary>
        /// Delegate to call when a curve stops being managed by this editor.
        /// </summary>
        public OnCurveDelegate onDeregisterCurve;
        /// <summary>
        /// Delegate for node selection.
        /// </summary>
        public OnMultiNodeIndexDelegate onSelectionChanged;
        /// <summary>
        /// Delegate to call when a node is selected.
        /// </summary>
        public OnNodeDelegate onSelectNode;
        /// <summary>
        /// Delegate to call when a nose is deselected.
        /// </summary>
        public OnNodeDelegate onDeselectNode;
        /// <summary>
        /// Delegate to call right before adding a node.
        /// </summary>
        public OnNodeDelegate onBeforeAddNode;
        /// <summary>
        /// Delegate to call when a node has been added.
        /// </summary>
        public OnNodeIndexPosDelegate onAddNode;
        /// <summary>
        /// Delegate to call before editing a single node.
        /// </summary>
        public OnNodeIndexDelegate onBeforeEditNode;
        /// <summary>
        /// Delegate to call after a node has been edited.
        /// </summary>
        public OnNodeIndexDelegate onEditNode;
        /// <summary>
        /// Delegate to call before editing a node style.
        /// </summary>
        public OnNodeIndexDelegate onBeforeEditNodeStyle;
        /// <summary>
        /// Delegate to call after a node style has been edited.
        /// </summary>
        public OnNodeIndexDelegate onEditNodeStyle;
        /// <summary>
        /// Delegate to call right before a selection of node begin moving.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeginDragNodes;
        /// <summary>
        /// Delegate to call after a selection of nodes stop moving.
        /// </summary>
        public OnMultiNodeIndexDelegate onEndDragNodes;
        /// <summary>
        /// Delegate to call right before moving a node.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeforeDragNodes;
        /// <summary>
        /// Delegate to call when a node has moved.
        /// </summary>
        public OnMultiNodeIndexDelegate onDragNodes;
        /// <summary>
        /// Delegate to call once when the movement of a handle start and before the offset gets applied.
        /// </summary>
        public OnCheckNodeHandlerDelegate onBeginDragHandle;
        /// <summary>
        /// Delegate to call once when the movement of a handle has ended.
        /// </summary>
        public OnCheckNodeHandlerDelegate onEndDragHandle;
        /// <summary>
        /// Delegate to call right before moving a node handle.
        /// </summary>
        public OnCheckNodeHandlerDelegate onBeforeDragHandle;
        /// <summary>
        /// Delegate to call when a node handle has moved.
        /// </summary>
        public OnCheckNodeHandlerDelegate onDragHandle;
        /// <summary>
        /// Delegate to call right before a selection of node begin rotating.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeginRotateNodes;
        /// <summary>
        /// Delegate to call after a selection of nodes stop rotating.
        /// </summary>
        public OnMultiNodeIndexDelegate onEndRotateNodes;
        /// <summary>
        /// Delegate to call right before rotating a node.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeforeRotateNodes;
        /// <summary>
        /// Delegate to call when a node has rotated.
        /// </summary>
        public OnMultiNodeIndexDelegate onRotateNodes;
        /// <summary>
        /// Delegate to call right before a selection of node begin scaling.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeginScaleNodes;
        /// <summary>
        /// Delegate to call after a selection of nodes stop scaling.
        /// </summary>
        public OnMultiNodeIndexDelegate onEndScaleNodes;
        /// <summary>
        /// Delegate to call right before scaling a node.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeforeScaleNodes;
        /// <summary>
        /// Delegate to call when a node has scaled.
        /// </summary>
        public OnMultiNodeIndexDelegate onScaleNodes;
        /// <summary>
        /// Delegate to call to see how selection of a new node is handled.
        /// </summary>
        public OnCheckSelectNode onCheckSelectCmd;
        /// <summary>
        /// Delegate to call when a handle has been selected.
        /// </summary>
        public OnCheckNodeHandlerDelegate onSelectHandle;
        /// <summary>
        /// Delegate to call when a handle loses focus.
        /// </summary>
        public OnCheckNodeHandlerDelegate onDeselectHandle;
        /// <summary>
        /// Delegate to call right before removing nodes.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeforeRemoveNodes;
        /// <summary>
        /// Delegate to call when nodes has been removed.
        /// </summary>
        public OnMultiNodeIndexDelegate onRemoveNodes;
        /// <summary>
        /// Delegate to call when validating if a node should be added based on its position.
        /// </summary>
        public OnValidatePositionDelegate onValidateAddNode;
        /// <summary>
        /// Delegate to call when dragging nodes.
        /// </summary>
        public OnCheckOffsetDelegate onCheckDragNodes;
        /// <summary>
        /// Delegate to call to check if deleting nodes is valid or not.
        /// </summary>
        public OnCheckNodesDelegate onCheckRemoveNodes;
        /// <summary>
        /// Delegate to call to check if a node should be drawn.
        /// </summary>
        public OnCheckNodeDelegate onCheckDrawNode;
        /// <summary>
        /// Delegate to call to check is a node should draw its first handle.
        /// </summary>
        public OnCheckNodeDelegate onCheckDrawFirstHandle;
        /// <summary>
        /// Delegate to call to check is a node should draw its second handle.
        /// </summary>
        public OnCheckNodeDelegate onCheckDrawSecondHandle;
        /// <summary>
        /// Checks the type of controls or mode a node is going to be drawn.
        /// </summary>
        public OnCheckNodeControlType onCheckNodeControls;
        public OnBeforeNodeMove onBeforeFreeDrag;
        public OnBeforeNodeMove onBeforeSliderMove;
        /// <summary>
        /// Delegate to call before setting the pivot mode/position. Sending the new position and new mode.
        /// </summary>
        public OnPivotDelegate onBeforeSetPivot;
        /// <summary>
        /// Delegate to call before setting the pivot mode/position.
        /// </summary>
        public OnPivotDelegate onSetPivot;
        /// <summary>
        /// Called after the on-scene toolbar has been drawn.
        /// </summary>
        public OnDrawEvent onDrawToolbar;
        #endregion

        #region Curves Management 
        /// <summary>
        /// Flag to let this editor handle the management of curves automatically.
        /// It is more performance wise to use the management methods yourself.
        /// </summary>
        public bool autoCurveManagement = true;
        /// <summary>
        /// Flag to debug log curve events.
        /// </summary>
        public bool debugCurveEvents = false;
        /// <summary>
        /// Flaf to debug log editor events.
        /// </summary>
        public bool debugEditorEvents = false;
        /// <summary>
        /// Checks if a ciurve instance is managed by this editor.
        /// </summary>
        /// <param name="managedCurve">Curve to check against.</param>
        /// <returns>True if the curve is managed by this editor.</returns>
        public bool HasCurve (BezierCurve managedCurve) {
            return _curves.Contains (managedCurve);
        }
        /// <summary>
        /// Registers a curve to be managed by this editor.
        /// </summary>
        /// <param name="curveToManage">Curve instance to manage.</param>
        /// <returns>True if the curve is registered to be managed by this editor.</returns>
        public bool RegisterCurve (BezierCurve curveToManage) {
            if (!_curves.Contains (curveToManage)) {
                _curves.Add (curveToManage);
                SetCurveListeners (curveToManage);
                if (debugEditorEvents) Debug.Log ($"OnRegisterCurve: {curveToManage.guid}");
                onRegisterCurve?.Invoke (curveToManage);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Deregisters a curve instance currently managed by this editor.
        /// </summary>
        /// <param name="managedCurve">Curve instance managed by this editor.</param>
        /// <returns>True if the curve was deregistered from management.</returns>
        public bool DeregisterCurve (BezierCurve managedCurve) {
            if (_curves.Contains (managedCurve)) {
                _curves.Remove (managedCurve);
                UnsetCurveListeners (managedCurve);
                if (debugEditorEvents) Debug.Log ($"OnDeregisterCurve: {managedCurve.guid}");
                onDeregisterCurve?.Invoke (managedCurve);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Deregisters all curves managed by this editor. 
        /// </summary>
        public void DeregisterAllCurves () {
            for (int i = 0; i < _curves.Count; i++) {
                UnsetCurveListeners (_curves [i]);
                onDeregisterCurve?.Invoke (_curves [i]);
            }
            _curves.Clear ();
        }
        /// <summary>
        /// Registers listeners for this editor on the target curve.
        /// </summary>
        /// <param name="targetCurve">Target curve.</param>
        private void SetCurveListeners (BezierCurve targetCurve) {
            targetCurve.onBeforeChange += OnBeforeCurveChange;
            targetCurve.onChange += OnCurveChange;
        }
        /// <summary>
        /// Deregister listeners for this editor on the target curve.
        /// </summary>
        /// <param name="targetCurve"></param>
        private void UnsetCurveListeners (BezierCurve targetCurve) {
            targetCurve.onBeforeChange -= OnBeforeCurveChange;
            targetCurve.onChange -= OnCurveChange;
        }
        #endregion

        #region Curve Event Listeners
        /// <summary>
        /// Listener to call before a curve changes.
        /// </summary>
        /// <param name="targetCurve">Curve receiving the change.</param>
        protected void OnBeforeCurveChange (BezierCurve targetCurve) {
            if (debugCurveEvents) {
                Debug.Log ($"OnBeforeCurveChange: {targetCurve.guid}");
            }
        }
        /// <summary>
        /// Listener to call when a curve changes.
        /// </summary>
        /// <param name="targetCurve">Curve receiving the change.</param>
        protected void OnCurveChange (BezierCurve targetCurve) {
            if (debugCurveEvents) {
                Debug.Log ($"OnCurveChange: {targetCurve.guid}");
            }
        }
        #endregion

        #region Editor Event Bridges
        /// <summary>
        /// Listener to call before a curve changes.
        /// </summary>
        /// <param name="targetCurve">Curve receiving the change.</param>
        protected void CallBeforeCurvesChange (List<BezierCurve> targetCurves) {
            if (debugEditorEvents) {
                Debug.Log ($"OnBeforeCurvesChange: {targetCurves.Count}");
            }
            onBeforeChangeCurves?.Invoke (targetCurves);
        }
        /// <summary>
        /// Listener to call before a curve changes.
        /// </summary>
        /// <param name="targetCurve">Curve receiving the change.</param>
        protected void CallBeforeCurvesChange (List<BezierNode> targetNodes) {
            List<BezierCurve> targetCurves = new List<BezierCurve> ();
            foreach (BezierNode node in targetNodes) {
                if (node.curve != null && !targetCurves.Contains (node.curve)) {
                    targetCurves.Add (node.curve);
                }
            }
            if (debugEditorEvents) {
                Debug.Log ($"OnBeforeCurvesChange: {targetCurves.Count}");
            }
            onBeforeChangeCurves?.Invoke (targetCurves);
        }
        /// <summary>
        /// Listener to call when a curve changes.
        /// </summary>
        /// <param name="targetCurve">Curve receiving the change.</param>
        protected void CallCurvesChange (List<BezierCurve> targetCurves) {
            if (debugEditorEvents) {
                Debug.Log ($"OnCurvesChange: {targetCurves.Count}");
            }
            onChangeCurves?.Invoke (targetCurves);
        }
        /// <summary>
        /// Listener to call when a curve changes.
        /// </summary>
        /// <param name="targetCurve">Curve receiving the change.</param>
        protected void CallCurvesChange (List<BezierNode> targetNodes) {
            List<BezierCurve> targetCurves = new List<BezierCurve> ();
            foreach (BezierNode node in targetNodes) {
                if (node.curve != null && !targetCurves.Contains (node.curve)) {
                    targetCurves.Add (node.curve);
                }
            }
            if (debugEditorEvents) {
                Debug.Log ($"OnCurvesChange: {targetCurves.Count}");
            }
            onChangeCurves?.Invoke (targetCurves);
        }
        #endregion

        #region Processing
        /// <summary>
		/// Clear this instance.
		/// </summary>
		public void Clear () {
			ClearSelection (true);
			_curve = null;
		}
        #endregion

        #region Scene and Inspector GUI
        public void OnEnable () {
            editModeOptions = new GUIContent[] {
				new GUIContent ("Selection Mode", "Lets you move, modify and delete nodes"),
				new GUIContent ("Addition Mode", "Lets you add new nodes to the curve")
			};
			editModeIndex = (int)editMode;
			handleStyleOptions = new GUIContent[] {
				new GUIContent ("Connected", "Connected"),
				new GUIContent ("Broken", "Broken"),
				new GUIContent ("None", "None")
			};
			handleStyleIndex = -1;
			removeNodeButtonContent = new GUIContent ("Remove Node", "Remove Node");

            // Load Icons
            LoadGUIIcons ();

            ClearSelection (true, false);

            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnEditorSceneManagerSceneOpened;
			UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnEditorSceneManagerSceneOpened;

            if (!enableOverlayToolbar) {
                #if UNITY_2022_1_OR_NEWER
                showTools = false;
                UnityEditor.Overlays.Overlay overlay = null;
                SceneView.lastActiveSceneView.TryGetOverlay ("Bezier Curve Toolbar", out overlay);
                if (overlay != null) {
                    BezierCurveToolbar unitToolbar = (BezierCurveToolbar)overlay;
                    unitToolbar.displayed = false;
                    unitToolbar.Close ();
                }
                #endif
            }
        }
        public void OnDisable() {
            ClearSprites ();
            UnityEngine.Object.DestroyImmediate (pivotOrigin);
            UnityEngine.Object.DestroyImmediate (pivotCenter);
            UnityEngine.Object.DestroyImmediate (pivotCenterBottom);
            UnityEngine.Object.DestroyImmediate (pivotFirst);
            UnityEngine.Object.DestroyImmediate (pivotLast);
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnEditorSceneManagerSceneOpened;
        }
        /// <summary>
        /// Draws the curve and editor tools to the scene view.
        /// </summary>
        /// <param name="curve">Curve to draw.</param>
        /// <param name="sceneOffset">Offset position for the curve.</param>
        public void OnCurveGUI (BezierCurve curve, Vector3 sceneOffset, bool isSelected = false, bool isEnabled = true) {
            _curve = curve;

            if (autoCurveManagement && !HasCurve (_curve)) {
                RegisterCurve (_curve);
            }

            curveId = curve.guid;
            if (shouldUpdateFocusedData && curveId == _focusedCurveId) { 
                UpdateFocusedCurveData ();
            }

            if (!isEnabled) {
                BezierCurveDraw.DrawCurve (curve, sceneOffset, scale, 
                    disabledCurveColor, (isSelected?selectedCurveWidth:curveWidth), 12);
                return;
            }

            if (_editMode == EditMode.Selection) { // Selection mode.
                if (Event.current.type == EventType.KeyUp) {
                   switch (Event.current.keyCode) {
                        case KeyCode.Delete:
                            bool deleted = RemoveSelectedNodes ();
                            if (deleted) Event.current.Use ();
                        break;
                    }
                }
                selectionHappened = false;
                ControlType ctrlType = ControlType.Move;
                bool _showHandles = showHandles;
                UnityEditor.Tool currentTool = Tools.current;
                if (rotateEnabled && currentTool == UnityEditor.Tool.Rotate) {
                    ctrlType = ControlType.Rotate;
                    _showHandles = false;
                } else if (scaleEnabled && currentTool == UnityEditor.Tool.Scale) {
                    ctrlType = ControlType.Scale;
                    _showHandles = false;
                }
                for (int i = 0; i < curve.nodeCount; i++) {
                    DrawBezierNode (curve[i], i, sceneOffset, scale, (i == curve.nodeCount - 1), ctrlType, _showHandles);
                }
            } else if (_editMode == EditMode.Add) { // Addition mode.
                if (curve.guid == _focusedCurveId) {
                    float handleSize = HandleUtility.GetHandleSize (s_tmpPos) * extraSmallNodeSize;
                    // Draw existing nodes.
                    for (int i = 0; i < _curve.nodeCount; i++) {
                        Handles.DrawSolidDisc ((_curve[i].position * scale) + sceneOffset,
                            Camera.current.transform.forward, 
                            handleSize);
                    }
                    // Draw add node candidates.
                    DrawAddNodeHandles (sceneOffset, scale, Handles.CircleHandleCap);
                }
                
				if (Event.current.keyCode == KeyCode.Escape) {
					editMode = EditMode.Selection;
				}
            }

            // TODO: use cache of nodes for drawing.
            if (debugEnabled && debugDrawSampleLine) {
                BezierCurveDraw.DrawCurveFromSamples (curve, sceneOffset, scale, (isSelected?selectedCurveColor:curveColor));
            } else {
                BezierCurveDraw.DrawCurve (curve, sceneOffset, scale, 
                    (isSelected?selectedCurveColor:curveColor), 
                    (isSelected?selectedCurveWidth:curveWidth), 6);
            }
            if (isSelected) {
                // Draw debug of points.
                if (debugEnabled && debugShowNodes) {
                    BezierCurveDraw.DrawCurveNodes (curve, sceneOffset, scale, Color.yellow);
                }
                // Draw debug of points.
                if (debugEnabled && debugShowPoints) {
                    BezierCurveDraw.DrawCurvePoints (curve, sceneOffset, scale, Color.white, 
                        debugShowPointForward, debugShowPointNormal, debugShowPointUp, debugShowPointTangent);
                }
                if (debugEnabled && debugShowFinePoints) {
                    BezierCurveDraw.DrawCurveFinePoints (curve, sceneOffset, scale, Color.white);
                }
                if (debugEnabled && debugShowCustomPoint && debugCustomPoint != null) {
                    Handles.color = Color.green;
                    float handleSize = HandleUtility.GetHandleSize (s_tmpPos) * nodeSize;
                    Handles.DrawSolidDisc (debugCustomPoint.position * scale + sceneOffset, Camera.current.transform.forward, handleSize);
                }
            }
        }
        /// <summary>
        /// Draws the curve and editor tools to the scene view.
        /// </summary>
        /// <param name="curve">Curve to draw.</param>
        /// <param name="sceneOffset">Offset position for the curve.</param>
        public void OnControlsGUI (Vector3 sceneOffset) {

            // Draw OnSceneGUI toolbar.
            if (showTools) {
                DrawTools ();
            }

            if (_editMode == EditMode.Selection) { // Selection mode.
                ControlType ctrlType = ControlType.Move;
                //bool _showHandles = showHandles;
                UnityEditor.Tool currentTool = Tools.current;
                if (rotateEnabled && currentTool == UnityEditor.Tool.Rotate) {
                    ctrlType = ControlType.Rotate;
                } else if (scaleEnabled && currentTool == UnityEditor.Tool.Scale) {
                    ctrlType = ControlType.Scale;
                }
                if (ctrlType != ControlType.Move && _selectedNodes.Count > 0) {
                    DrawPivot (sceneOffset, scale, ctrlType);
                }
            } else if (_editMode == EditMode.Add) { // Addition mode.
                /*
                if (curve.guid == _focusedCurveId) {
                    float handleSize = HandleUtility.GetHandleSize (s_tmpNode) * extraSmallNodeSize;
                    // Draw existing nodes.
                    for (int i = 0; i < _curve.nodeCount; i++) {
                        Handles.DrawSolidDisc ((_curve[i].position * scale) + sceneOffset,
                            Camera.current.transform.forward, 
                            handleSize);
                    }
                    // Draw add node candidates.
                    DrawAddNodeHandles (sceneOffset, scale, Handles.CircleHandleCap);
                }
                
				if (Event.current.keyCode == KeyCode.Escape) {
					editMode = EditMode.Selection;
				}
                */
            }
        }
        public void ShowToolbar ()
        {
            #if UNITY_2022_1_OR_NEWER
            if (enableOverlayToolbar) {
                showTools = false;
                UnityEditor.Overlays.Overlay overlay = null;
                if (SceneView.lastActiveSceneView != null) {
                    SceneView.lastActiveSceneView.TryGetOverlay ("Bezier Curve Toolbar", out overlay);
                } else {
                    SceneView scene;
                    for (int i = 0; i < SceneView.sceneViews.Count; i++) {
                        scene = (SceneView)SceneView.sceneViews[i];
                        scene.TryGetOverlay ("Bezier Curve Toolbar", out overlay);
                        if (overlay != null) break; 
                    }
                }
                if (overlay != null) { 
                    BezierCurveToolbar unitToolbar = (BezierCurveToolbar)overlay;
                    unitToolbar.displayed = true;
                    unitToolbar.UpdateToolbar (this); 
                }
            } else {
                showTools = true;
            }
            #else
            showTools = true;
			#endif
        }
        public void HideToolbar ()
        {
            #if UNITY_2022_1_OR_NEWER
            showTools = false;
			UnityEditor.Overlays.Overlay overlay = null;
			SceneView.lastActiveSceneView.TryGetOverlay ("Bezier Curve Toolbar", out overlay);
			if (overlay != null) {
				BezierCurveToolbar unitToolbar = (BezierCurveToolbar)overlay;
				unitToolbar.displayed = false;
			}
            #else
            showTools = false;
			#endif
        }
        /// <summary>
		/// Called when a scene opens in the editor.
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="mode"></param>
		void OnEditorSceneManagerSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode) {
            LoadGUIIcons();
		}
        /// <summary>
        /// Loads the GUI icons for this instance.
        /// </summary>
        void LoadGUIIcons () {
            ClearSprites ();
            int theme = EditorGUIUtility.isProSkin?0:24;
            editorModeIconOptions = new GUIContent[] {
                new GUIContent (LoadSprite ("bezier_canvas_GUI", 120, theme, 24, 24), "Selection Mode"),
				new GUIContent (LoadSprite ("bezier_canvas_GUI", 144, theme, 24, 24), "Add Node Mode")
            };
            nodeActionsIconOptions = new GUIContent[] {
                new GUIContent (LoadSprite ("bezier_canvas_GUI", 96, theme, 24, 24), "Remove Node")
            };
            handleStyleIconOptions = new GUIContent[] {
				new GUIContent (LoadSprite ("bezier_canvas_GUI", 0, theme, 24, 24), "Node Mode: Smooth"),
                new GUIContent (LoadSprite ("bezier_canvas_GUI", 24, theme, 24, 24), "Node Mode: Aligned"),
                new GUIContent (LoadSprite ("bezier_canvas_GUI", 48, theme, 24, 24), "Node Mode: Broken"),
                new GUIContent (LoadSprite ("bezier_canvas_GUI", 72, theme, 24, 24), "Node Mode: None")
			};
            pivotOrigin = LoadSprite ("bezier_canvas_GUI", 24 * 7, theme, 24, 24);
            pivotCenter = LoadSprite ("bezier_canvas_GUI", 24 * 8, theme, 24, 24);
            pivotCenterBottom = LoadSprite ("bezier_canvas_GUI", 24 * 9, theme, 24, 24);
            pivotFirst = LoadSprite ("bezier_canvas_GUI", 24 * 10, theme, 24, 24);
            pivotLast = LoadSprite ("bezier_canvas_GUI", 24 * 11, theme, 24, 24);
            pivotActionsIconOptions = new GUIContent[] {
                new GUIContent (pivotOrigin, "Pivot Mode")
            };
        }
        /// <summary>
        /// Updates data for a focused curve.
        /// </summary>
        private void UpdateFocusedCurveData () {
            _curveBounds = _curve.GetBounds();
            _addNodeLowerPoint = _curve.GetPositionAt (addNodeLowerLimit);
            _addNodeUpperPoint = _curve.GetPositionAt (addNodeUpperLimit);
        }
        Rect toolbarRect1 = new Rect (0, 3, 48, 24);
        Rect toolbarRect2 = new Rect (0, 3, 24, 24);
        Rect toolbarRect3 = new Rect (0, 3, 96, 24);
        Rect toolbarRect4 = new Rect (0, 3, 24, 24);
        private const int toolbarWidthSpace = 3;
        /// <summary>
        /// Draw the on-scene toolbar.
        /// </summary>
        private void DrawTools () {
            SceneView sceneView = SceneView.currentDrawingSceneView;
            if (sceneView != null) {
                toolbarContainer = new Rect (0, 0, 0, 24);
                
                toolbarContainer.x = (int)(sceneView.position.width / 2) - (toolbarRect1.width + toolbarRect2.width + toolbarRect3.width + toolbarRect4.width) / 2f;
                toolbarContainer.width = toolbarRect1.width + toolbarRect2.width + toolbarRect3.width + toolbarRect4.width + toolbarWidthSpace * 3;
                toolbarContainer.x += toolbarXOffset;

                bool hasSingleNode = hasSingleSelection;
                bool isLastNode = (selectedNodeIndex == _curve.nodeCount - 1);
                bool guiEnabled = GUI.enabled;
                BezierNode node = selectedNode;
                int handleStyleIndex = -1;
                int editModeIndex = 0;

                Handles.BeginGUI();
                // EDITOR MODE.
                GUI.enabled = enableToolbarModes;
                toolbarRect1.x = toolbarContainer.x;
                switch (editMode) {
                    case EditMode.Selection: editModeIndex = 0; break;
                    case EditMode.Add: editModeIndex = 1; break;
                }
                EditorGUI.BeginChangeCheck ();
                editModeIndex = GUI.Toolbar (toolbarRect1, editModeIndex, editorModeIconOptions);
                if (EditorGUI.EndChangeCheck ()) {
                    switch (editModeIndex) {
                        case 0: editMode = EditMode.Selection; break;
                        case 1: editMode = EditMode.Add; break;
                    }
                }

                // NODE ACTIONS. (delete node)
                GUI.enabled = hasSingleNode && enableToolbarActions && node.relativePosition != 0 && node.relativePosition != 1;
                toolbarRect2.x = toolbarContainer.x + toolbarRect1.width + toolbarWidthSpace;
                int deleteSelected = GUI.Toolbar (toolbarRect2, -1, nodeActionsIconOptions);
                if (deleteSelected != -1) {
                    if (EditorUtility.DisplayDialog ("Delete node",
						"Do you really want to remove this node?", "Yes", "No")) {
                        RemoveSelectedNodes ();
                    }
                    GUIUtility.ExitGUI();
                }

                // NODE HANDLE STYLE.
                GUI.enabled = hasSingleNode && enableToolbarNode;
                if (hasSingleNode && node != null) {
                    switch (node.handleStyle) {
                        case BezierNode.HandleStyle.Auto: handleStyleIndex = 0; break;
                        case BezierNode.HandleStyle.Aligned: handleStyleIndex = 1; break;
                        case BezierNode.HandleStyle.Free: handleStyleIndex = 2; break;
                        case BezierNode.HandleStyle.None: handleStyleIndex = 3; break;
                    }
                }
                toolbarRect3.x = toolbarContainer.x + toolbarRect1.width + toolbarRect2.width + toolbarWidthSpace * 2;
                EditorGUI.BeginChangeCheck ();
                handleStyleIndex = GUI.Toolbar (toolbarRect3, handleStyleIndex, handleStyleIconOptions);
                if (EditorGUI.EndChangeCheck ()) {
                    BezierNode.HandleStyle newHandleStyle = BezierNode.HandleStyle.Auto;
                    switch (handleStyleIndex) {
                        case 0: newHandleStyle = BezierNode.HandleStyle.Auto; break;
                        case 1: newHandleStyle = BezierNode.HandleStyle.Aligned; break;
                        case 2: newHandleStyle = BezierNode.HandleStyle.Free; break;
                        case 3: newHandleStyle = BezierNode.HandleStyle.None; break;
                    }
                    ChangeNodeHandleStyle (selectedNode, selectedNodeIndex, (BezierNode.HandleStyle)newHandleStyle, isLastNode);
                }

                // PIVOT OPS
                GUI.enabled = _selectedNodes.Count > 0;
                toolbarRect4.x = toolbarContainer.x + toolbarRect1.width + toolbarRect2.width + toolbarRect3.width + toolbarWidthSpace * 3;
                int pivotSelected = GUI.Toolbar (toolbarRect4, -1, pivotActionsIconOptions);
                if (pivotSelected != -1) {
                    ShowPivotDropdownMenu (toolbarRect4);
                }

                
                Handles.EndGUI();
                GUI.enabled = guiEnabled;

                onDrawToolbar?.Invoke (toolbarContainer);
            }
        }
        private void ShowPivotDropdownMenu(Rect buttonRect)
        {
            GenericMenu menu = new GenericMenu();

            // Add menu items
            menu.AddItem(new GUIContent("Pivot to Origin"), pivotMode == PivotMode.Origin, () => SetPivotToOrigin ());
            menu.AddItem(new GUIContent("Pivot to Selection Center"), pivotMode == PivotMode.Center, () => SetPivotToSelectionCenter ());
            menu.AddItem(new GUIContent("Pivot to Selection Center-Bottom"), pivotMode == PivotMode.CenterBottom, () => SetPivotToSelectionCenterBottom ());
            menu.AddItem(new GUIContent("Pivot to First Selected Node"), pivotMode == PivotMode.FirstNode, () => SetPivotToFirstNode ());
            menu.AddItem(new GUIContent("Pivot to Last Selected Node"), pivotMode == PivotMode.LastNode, () => SetPivotToLastNode ());
            //menu.AddSeparator(""); // Add a separator
            //menu.AddItem(new GUIContent("Submenu/Option 4"), selectedOption == "Option 4", () => OnOptionSelected("Option 4"));

            // Show the menu.  Crucially, use DropDown() with the button's rect.
            menu.DropDown(buttonRect); // Show *below* the button
        }
        /// <summary>
        /// Draws the curve and editor tools to the scene view.
        /// </summary>
        /// <param name="curve">Curve to draw.</param>
        /// <param name="sceneOffset">Offset position for the curve.</param>
        public void OnSceneGUIDrawSingleNode (BezierCurve curve, int index, Vector3 sceneOffset, bool isSelected = false, ControlType ctrlType = ControlType.Move, bool drawHandles = false) {
            _curve = curve;
            if (_editMode == EditMode.Selection) { // Selection mode.
                selectionHappened = false;
                for (int i = 0; i < curve.nodeCount; i++) {
                    if (i == index) {
                        DrawBezierNode (curve[i], i, sceneOffset, scale, (i == curve.nodeCount - 1), ctrlType, drawHandles && showHandles);
                        break;
                    }
                }
                if (Event.current.type == EventType.KeyUp) {
                    switch (Event.current.keyCode) {
                        case KeyCode.Delete:
                            bool deleted = RemoveSelectedNodes ();
                            if (deleted) Event.current.Use ();
                        break;
                    }
                }
            }
        }
        public bool OnInspectorGUI (BezierCurve curve) {
            bool change = false;

            // Resolution Angle
            float resolutionAngle = EditorGUILayout.FloatField (_curve.resolutionAngle);
            if (resolutionAngle != _curve.resolutionAngle) {
                _curve.Process (resolutionAngle);
            }

            // Edit mode.
			editModeIndex = (int)editMode;
			editModeIndex = GUILayout.SelectionGrid (editModeIndex, editModeOptions, 1);
			if (editModeIndex != (int)editMode) {
				editMode = (BezierCurveEditor.EditMode)editModeIndex;
				Event.current.Use ();
			}

			// Node handle style.
			handleStyleIndex = -1;
			if (hasSingleSelection) {
				handleStyleIndex = (int)selectedNode.handleStyle;
			}
			EditorGUILayout.Separator (); // TODO: have a "lastAction" exposed with an enum value to consult the last action performed on this editor.
			EditorGUI.BeginDisabledGroup (!hasSingleSelection); // TODO. Enable mode buttons that differ to the selected one.
			handleStyleIndex = GUILayout.SelectionGrid (handleStyleIndex, handleStyleOptions, 1 /*num of cols*/);
			if (hasSingleSelection && handleStyleIndex != (int)selectedNode.handleStyle) {
				// TODO
				ChangeNodeHandleStyle (selectedNode, selectedNodeIndex, (BezierNode.HandleStyle)handleStyleIndex, (selectedNodeIndex == _curve.nodeCount - 1));
				Event.current.Use ();
			}
			EditorGUI.EndDisabledGroup ();

            // Snap selected nodes to axis.
            EditorGUILayout.Space ();
            EditorGUI.BeginDisabledGroup (!hasSelection);
            if (GUILayout.Button ("Snap Selection to X Axis")) {
                SnapSelectedNodes (BezierCurve.Axis.X);
            }
            if (GUILayout.Button ("Snap Selection to Y Axis")) {
                SnapSelectedNodes (BezierCurve.Axis.Y);
            }
            if (GUILayout.Button ("Snap Selection to Z Axis")) {
                SnapSelectedNodes (BezierCurve.Axis.Z);
            }
			EditorGUI.EndDisabledGroup ();

			// Remove node button.
			EditorGUILayout.Separator ();
			EditorGUI.BeginDisabledGroup (!removeNodeButtonEnabled || !hasSelection);
			if (GUILayout.Button (removeNodeButtonContent)) {
				RemoveSelectedNodes ();
			}
			EditorGUI.EndDisabledGroup ();

            if (debugEnabled) {
                EditorGUILayout.Space ();
                debugShowPoints = EditorGUILayout.Toggle ("Show Curve Points", debugShowPoints);
                debugShowFinePoints = EditorGUILayout.Toggle ("Show Curve Fine Points", debugShowFinePoints);
                debugShowNodeLabels = EditorGUILayout.Toggle ("Show Node Labels", debugShowNodeLabels);
                debugDrawSampleLine = EditorGUILayout.Toggle ("Draw Noise Line", debugDrawSampleLine);
                if (GUILayout.Button ("Print Debug Info")) {
                    PrintDebugInfo ();
                }
            }

            return change;
        }
        #endregion
        
        #region Draw and Handles
        void DrawPivot (Vector3 sceneOffset, float scale, ControlType controlType)
        {
            s_tmpPos = (Pivot * scale) + sceneOffset;
            float handleSize = HandleUtility.GetHandleSize (s_tmpPos);
            _tmpColor = Handles.color;
            Handles.color = pivotColor;

            // Draw Pivot.
            if (showPivot) {
                Handles.DrawSolidDisc (s_tmpPos,
                    Camera.current.transform.forward, 
                    handleSize * pivotSize);
            }

            int ctrlId = GUIUtility.GetControlID (FocusType.Passive);
            switch (controlType) {
                case ControlType.Rotate:
                    Quaternion newRotation;
                    tmpHotControl = GUIUtility.hotControl;
                    Handles.color = selectedNodeColor;
                    newRotation = Handles.RotationHandle (s_tmpPreviousRotation, s_tmpPos);

                    // Rotation handle lost focus, end of rotation.
                    if (tmpHotControl != GUIUtility.hotControl) {
                        if (GUIUtility.hotControl == 0) {
                            if (_selectedNodesRotating) {
                                onEndRotateNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);      // ON END ROTATE NODES
                                if (debugEditorEvents) Debug.Log ($"OnEndRotateNodes: {_selectedNodes.Count} nodes");
                                ProcessNodesCurves (_selectedNodes);
                                s_tmpPreviousRotation = Quaternion.identity;
                                newRotation = s_tmpPreviousRotation;
                            }
                        }
                        _selectedNodesRotating = false;                        
                    }

                    // If rotation is different to Quaternion.identity or rotation is ongoing.
                    if (newRotation != s_tmpPreviousRotation || _selectedNodesRotating) {
                        Quaternion rotationOffset = Quaternion.Inverse(s_tmpPreviousRotation) * newRotation;
                        
                        if (!_selectedNodesRotating) {
                            CallBeforeCurvesChange (_selectedNodes);
                            onBeginRotateNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);        // ON BEGIN ROTATE NODES
                            if (debugEditorEvents) Debug.Log ($"OnBeginRotateNodes: {_selectedNodes.Count} nodes");
                            _selectedNodesRotating = true;
                        }
                        if (rotationOffset != Quaternion.identity) {
                            RotateSelectedNodes (rotationOffset, Pivot);                                                // ON BEFORE ROTATE, ON ROTATE NODES
                        }
                        s_tmpPreviousRotation = newRotation;
                    }
                    break;
                case ControlType.Scale:
                    Vector3 newScale;
                    tmpHotControl = GUIUtility.hotControl;
                    Handles.color = selectedNodeColor;
                    newScale = Handles.ScaleHandle (s_tmpPreviousScale, s_tmpPos, Quaternion.identity); 

                    // Scale handle lost focus, end of scaling.
                    if (tmpHotControl != GUIUtility.hotControl) {
                        if (GUIUtility.hotControl == 0) {
                            if (_selectedNodesScaling) {
                                onEndScaleNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);       // ON END SCALE NODES
                                if (debugEditorEvents) Debug.Log ($"OnEndScaleNodes: {_selectedNodes.Count} nodes");
                                ProcessNodesCurves (_selectedNodes);
                                s_tmpPreviousScale = Vector3.one;
                                newScale = s_tmpPreviousScale;
                            }
                        }
                        _selectedNodesScaling = false;                        
                    }

                    // If scale is different to Vector3.one or scaling is ongoing.
                    if (newScale != s_tmpPreviousScale || _selectedNodesScaling) {
                        if (s_tmpPreviousScale.x != 0 && s_tmpPreviousScale.y != 0 && s_tmpPreviousScale.z != 0) {
                            scaleOffset.x = newScale.x / s_tmpPreviousScale.x;
                            scaleOffset.y = newScale.y / s_tmpPreviousScale.y;
                            scaleOffset.z = newScale.z / s_tmpPreviousScale.z;
                            if (!_selectedNodesScaling) {
                                CallBeforeCurvesChange (_selectedNodes);
                                onBeginScaleNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);         // ON BEGIN SCALE NODES
                                if (debugEditorEvents) Debug.Log ($"OnBeginScaleNodes: {_selectedNodes.Count} nodes");
                                _selectedNodesScaling = true;
                            }
                            if (scaleOffset != Vector3.one) {
                                ScaleSelectedNodes (scaleOffset, Pivot);                                      // ON BEFORE SCALE, ON SCALE NODES
                            }
                            s_tmpPreviousScale = newScale;
                        }
                    }
                    break;
            }
            Handles.color = _tmpColor;
        }
        /// <summary>
        /// Draws a bezier node.
        /// </summary>
        /// <param name="node">Bezier node to draw.</param>
        /// <param name="index">Index of the bezier node on the curve.</param>
        /// <param name="sceneOffset">Offset of the curve.</param>
        /// <param name="scale">Scale of the curve.</param>
        /// <param name="isLastNode">Node is at the end of the curve.</param>
        void DrawBezierNode (BezierNode node, int index, Vector3 sceneOffset, float scale, bool isLastNode, ControlType? ctrlToDraw = null, bool drawHandles = true) {
            // Check if this node should be drawn.
            bool shouldDraw = true;
            if (ctrlToDraw == null && onCheckDrawNode != null) {
				shouldDraw = onCheckDrawNode (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty));
			}
            if (!shouldDraw) {
                return;
            }

            s_tmpPos = (node.position * scale) + sceneOffset;
            float handleSize = HandleUtility.GetHandleSize (s_tmpPos);
            _tmpColor = Handles.color;
            
            if (debugEnabled && debugShowNodeLabels) {
                Handles.color = nodeLabelColor;
                Handles.Label (node.position + new Vector3 (0, handleSize * labelSize, 0), 
                    "Node " +  index + (debugShowRelativePos?"\n(" + node.relativePosition + ")":"") + (debugShowNodeGUID?"\n(" + node.guid + ")":""));
            }

            // Draw controls for the node
            if (ctrlToDraw == null) {
                ctrlToDraw = ControlType.Move;
                if (onCheckNodeControls != null) {
                    ctrlToDraw = onCheckNodeControls(node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty));
                }
            }
            int ctrlId = GUIUtility.GetControlID (FocusType.Passive);
            switch (ctrlToDraw) {
                case ControlType.Move:
                    Vector3 newPosition;
                    if (node.isSelected) {
                        int hotControl = GUIUtility.hotControl;
                        Handles.color = selectedNodeColor;
                        if (rayDragEnabled) {
                            #if UNITY_2022_1_OR_NEWER
                            newPosition = Handles.FreeMoveHandle (s_tmpPos, handleSize * nodeSize, Vector3.zero, selectedNodeDrawFunction);
                            #else
                            newPosition = Handles.FreeMoveHandle (s_tmpPos, Quaternion.identity, handleSize * nodeSize, Vector3.zero, selectedNodeDrawFunction);
                            #endif
                        } else {
                            newPosition = Handles.PositionHandle (s_tmpPos, Quaternion.identity);
                            FreeMoveHandle (ctrlId, s_tmpPos, Quaternion.identity, handleSize * nodeSize,
                                Vector3.zero, selectedNodeDrawFunction);
                        }
                        if (hotControl != GUIUtility.hotControl) {
                            if (GUIUtility.hotControl == 0) {
                                if (_selectedNodesMoved) {
                                    onEndDragNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds); // ON END DRAG NODES
                                    if (debugEditorEvents) Debug.Log ($"OnEndDragNodes: {_selectedNodes.Count} nodes");
                                    ProcessNodesCurves (_selectedNodes);
                                }
                            } else {
                                //TODO: Call OnSelectNode
                            }
                            _selectedNodesMoved = false;
                        }
                        if (newPosition != s_tmpPos) {
                            Vector3 dragOffset = newPosition - s_tmpPos;
                            // OnBeginFreeDrag
                            if (!_selectedNodesMoved) {
                                lastKnownOffset = dragOffset / scale;
                                offsetStep = lastKnownOffset;
                                CallBeforeCurvesChange (_selectedNodes);
                                onBeginDragNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);         // ON BEGIN DRAG NODES
                                if (debugEditorEvents) Debug.Log ($"OnBeginDragNodes: {_selectedNodes.Count} nodes");
                                _selectedNodesMoved = true;
                            }
                            // OnBeforeFreeDrag
                            if (onBeforeFreeDrag != null) {
                                dragOffset = onBeforeFreeDrag (node, s_tmpPos, newPosition, scale, index, curveId);
                            }
                            if (dragOffset != Vector3.zero) {
                                DragSelectedNodes (dragOffset / scale); // ON BEFORE DRAG, ON DRAG NODES
                            }
                        }
                    } else {
                        Handles.color = nodeColor;
                        newPosition = FreeMoveHandle (ctrlId, s_tmpPos, Quaternion.identity, handleSize * nodeSize,
                            Vector3.zero, nodeDrawFunction);
                    }
                    break;
                /*
                case ControlType.SliderMove:
                    Vector3 newSliderPosition;
                    // TODO: CALL OnEndMoveNode
                    if (node.isSelected) {
                        Handles.color = selectedNodeColor;
                        SliderMoveHandle (ctrlId, s_tmpNode, Quaternion.identity, handleSize * nodeSize,
                                Vector3.zero, nodeDrawFunction);
                        newSliderPosition = Handles.Slider (s_tmpNode, sliderVector);
                        if (newSliderPosition != s_tmpNode) {
                            Vector3 moveOffset = newSliderPosition - s_tmpNode;
                            if (onBeforeSliderMove != null) {
                                moveOffset = onBeforeSliderMove (node, s_tmpNode, newSliderPosition, scale, index, curveId);
                            }
                            if (moveOffset != Vector3.zero) {
                                DragSelectedNodes (moveOffset / scale);
                            }
                        }
                    } else {
                        Handles.color = nodeColor;
                        newSliderPosition = FreeMoveHandle (ctrlId, s_tmpNode, Quaternion.identity, handleSize * nodeSize,
                            Vector3.zero, nodeDrawFunction);
                    }
                    break;
                    */
                case ControlType.Rotate:
                case ControlType.Scale:
                case ControlType.DrawSelectable:
                    if (node.isSelected) {
                        Handles.color = selectedNodeColor;
                        SelectableHandle (ctrlId, s_tmpPos, Quaternion.identity, 
                            handleSize * nodeSize, selectedNodeDrawFunction);
                    } else {
                        Handles.color = nodeColor;
                        SelectableHandle (ctrlId, s_tmpPos, Quaternion.identity, 
                            handleSize * nodeSize, nodeDrawFunction);
                    }
                    break;
                case ControlType.DrawOnly:
                    Handles.color = nodeColor;
                    Handles.DrawSolidDisc (s_tmpPos,
                        Camera.current.transform.forward, 
                        handleSize * nodeSize);
                    break;
                case ControlType.None:
                    break;
                default:
                    break;
            }

            // Draw handles.
            if (drawHandles) {
                s_tmpHandle1Drawn = false;
                s_tmpHandle2Drawn = false;
                if (node.handleStyle != BezierNode.HandleStyle.None) {
                    Handles.color = node.isSelected?selectedNodeHandleColor:nodeHandleColor;
                    // Conditional second handle first drawn.
                    if (isLastNode && (showSecondHandleAlways || _curve.closed)) {
                        bool drawSecond = true;
                        if (onCheckDrawSecondHandle != null) 
                            drawSecond = onCheckDrawSecondHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty));
                        if (drawSecond) {
                            DrawBezierNodeHandle2 (node, index, sceneOffset, scale, isLastNode);
                            s_tmpHandle2Drawn = true;
                        }
                    }
                    // First handle.
                    if (!(index == 0 && node.handleStyle != BezierNode.HandleStyle.Aligned) || showFirstHandleAlways || _curve.closed) {
                        bool drawFirst = true;
                        if (onCheckDrawSecondHandle != null) 
                            drawFirst = onCheckDrawSecondHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty));
                        if (drawFirst) {
                            DrawBezierNodeHandle1 (node, index, sceneOffset, scale, isLastNode);
                            s_tmpHandle1Drawn = true;
                        }
                    }
                    // Second handle.
                    if (!(isLastNode && node.handleStyle != BezierNode.HandleStyle.Aligned)) {
                        bool drawSecond = true;
                        if (onCheckDrawSecondHandle != null) 
                            drawSecond = onCheckDrawSecondHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty));
                        if (drawSecond) {
                            DrawBezierNodeHandle2 (node, index, sceneOffset, scale, isLastNode);
                            s_tmpHandle2Drawn = true;
                        }
                    }

                    Handles.color = nodeHandleLineColor;
                    // First handle line
                    if (s_tmpHandle1Drawn) {
                        Handles.DrawDottedLine(s_tmpPos, s_tmpHandle1, 4f);
                    }
                    // Second handle line
                    if (s_tmpHandle2Drawn) {
                        Handles.DrawDottedLine(s_tmpPos, s_tmpHandle2, 4f);
                    }
                }
            }

            // Node has been selected
            if (selectionHappened) {
                SelectionCommand selectionCmd = SelectionCommand.Select;
                if (onCheckSelectCmd != null) {
                    selectionCmd = onCheckSelectCmd (node);
                    if (debugEditorEvents) Debug.Log ($"onCheckSelectCmd: {selectionCmd}");
                }
                if (selectionCmd != SelectionCommand.DoNotSelect) {
                    selectionHappened = ManageNodeToSelection (node,
                        index,
                        Event.current.control,
                        (selectionCmd == SelectionCommand.SingleSelect?false:Event.current.shift));
                    if (selectionHappened && onSelectionChanged != null) {
                        onSelectionChanged (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                        if (debugEditorEvents) Debug.Log ($"onSelectionChanged: {_selectedNodes.Count} nodes.");
                    }
                }
                selectionHappened = false;
            }

            Handles.color = _tmpColor;
        }
        void DrawBezierNodeHandle1 (BezierNode node, int index, Vector3 sceneOffset, float scale, bool isLastNode) {
            int hotControl = GUIUtility.hotControl;
            s_tmpHandle1 = node.globalHandle1 * scale + sceneOffset;
            #if UNITY_2022_1_OR_NEWER
            Vector3 newGlobal1 = Handles.FreeMoveHandle (
                s_tmpHandle1, 
                HandleUtility.GetHandleSize (s_tmpHandle1) * nodeHandleSize, 
                Vector3.zero, 
                node.isSelected?selectedNodeHandleDrawFunction:nodeHandleDrawFunction);
            #else
            Vector3 newGlobal1 = Handles.FreeMoveHandle (
                s_tmpHandle1, 
                Quaternion.identity, 
                HandleUtility.GetHandleSize (s_tmpHandle1) * nodeHandleSize, 
                Vector3.zero, 
                node.isSelected?selectedNodeHandleDrawFunction:nodeHandleDrawFunction);
            #endif
            if (debugEnabled && debugShowNodeLabels) {
                Handles.color = nodeLabelColor;
                Handles.Label (s_tmpHandle1, "  (1)");
            }
            if (hotControl != GUIUtility.hotControl) {
                if (GUIUtility.hotControl == 0) {
                    // Call OnDeselectHandle
                    onDeselectHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 1);
                    if (debugEditorEvents) Debug.Log ($"onDeselectHandle: {node.guid}, handle1");
                    // Call OnEndMoveHandles
                    if (_selectedHandleHasMoved) {
                        onEndDragHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 1);
                        if (debugEditorEvents) Debug.Log ($"onEndDragHandle: {node.guid}, handle1");
                        ProcessCurve (node.curve);
                    }
                } else {
                    // Call OnSelectHandle
                    onSelectHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 1);
                    if (debugEditorEvents) Debug.Log ($"onSelectHandle: {node.guid}, handle1");
                }
                _selectedHandleHasMoved = false;
            }
            if (s_tmpHandle1 != newGlobal1) {
                // Call OnBeginMoveHandle
                if (!_selectedHandleHasMoved) {
                    CallBeforeCurvesChange (new List<BezierCurve> () {node.curve});
                    onBeginDragHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 1);
                    if (debugEditorEvents) Debug.Log ($"onBeginDragHandle: {node.guid}, handle1");
                    _selectedHandleHasMoved = true;
                }
                // Call OnBeforeMoveHandle
                onBeforeDragHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 1);
                if (debugEditorEvents) Debug.Log ($"onBeforeDragHandle: {node.guid}, handle1");
                _nodeListenEvents = node.listenEvents;
                node.listenEvents = false;
                node.globalHandle1 = (newGlobal1 - sceneOffset) / scale;
                node.listenEvents = _nodeListenEvents;
                // Call OnMoveHandle
                onDragHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 1);
                if (debugEditorEvents) Debug.Log ($"onDragHandle: {node.guid}, handle1");
            }
        }
        void DrawBezierNodeHandle2 (BezierNode node, int index, Vector3 sceneOffset, float scale, bool isLastNode) {
            int hotControl = GUIUtility.hotControl;
            s_tmpHandle2 = node.globalHandle2 * scale + sceneOffset;
            #if UNITY_2022_1_OR_NEWER
            Vector3 newGlobal2 = Handles.FreeMoveHandle(
                s_tmpHandle2, 
                HandleUtility.GetHandleSize (s_tmpHandle2) * nodeHandleSize, 
                Vector3.zero, 
                node.isSelected?selectedNodeHandleDrawFunction:nodeHandleDrawFunction);
            #else
            Vector3 newGlobal2 = Handles.FreeMoveHandle(
                s_tmpHandle2, 
                Quaternion.identity, 
                HandleUtility.GetHandleSize (s_tmpHandle2) * nodeHandleSize, 
                Vector3.zero, 
                node.isSelected?selectedNodeHandleDrawFunction:nodeHandleDrawFunction);
            #endif
            if (debugEnabled && debugShowNodeLabels) {
                Handles.color = nodeLabelColor;
                Handles.Label (s_tmpHandle2, "  (2)");
            }
            if (hotControl != GUIUtility.hotControl) {
                if (GUIUtility.hotControl == 0) {
                    // Call OnDeselectHandle
                    onDeselectHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 2);
                    if (debugEditorEvents) Debug.Log ($"onDeselectHandle: {node.guid}, handle2");
                    // Call OnEndMoveHandle
                    if (_selectedHandleHasMoved) {
                        onEndDragHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 2);
                        if (debugEditorEvents) Debug.Log ($"onEndDragHandle: {node.guid}, handle2");
                        ProcessCurve (node.curve);
                    }
                } else {
                    // Call OnSelectHandle
                    onSelectHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 2);
                    if (debugEditorEvents) Debug.Log ($"onSelectHandle: {node.guid}, handle2");
                }
                _selectedHandleHasMoved = false;
            }
            if (s_tmpHandle2 != newGlobal2) {
                // Call OnBeginMoveHandle
                if (!_selectedHandleHasMoved) {
                    CallBeforeCurvesChange (new List<BezierCurve> () {node.curve});
                    onBeginDragHandle?.Invoke(node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 2);
                    if (debugEditorEvents) Debug.Log ($"onBeginDragHandle: {node.guid}, handle2");
                    _selectedHandleHasMoved = true;
                }
                onBeforeDragHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 2);
                if (debugEditorEvents) Debug.Log ($"onBeforeDragHandle: {node.guid}, handle2");
                _nodeListenEvents = node.listenEvents;
                node.listenEvents = false;
                node.globalHandle2 = (newGlobal2 - sceneOffset) / scale;
                node.listenEvents = _nodeListenEvents;
                onDragHandle?.Invoke (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:System.Guid.Empty), 2);
                if (debugEditorEvents) Debug.Log ($"onDragHandle: {node.guid}, handle2");
            }
        }
		/// <summary>
		/// Free move handle tool based on Unity method.
		/// </summary>
		/// <param name="id">Handle control identifier.</param>
		/// <param name="position">Position in world space.</param>
		/// <param name="rotation">Rotation.</param>
		/// <param name="size">Size of the handle.</param>
		/// <param name="snap">Snap to grid.</param>
		/// <param name="handleFunction"></param>
		/// <returns>The position of the handle, dragged or not.</returns>
        public Vector3 FreeMoveHandle (int id, Vector3 position, Quaternion rotation, float size, Vector3 snap, Handles.CapFunction handleFunction) {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint (position);
            Matrix4x4 origMatrix = Handles.matrix;
            //VertexSnapping.HandleMouseMove(id); TODO: Version
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id)) {
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Layout);
                    Handles.matrix = origMatrix;
                    break;
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0) {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        //HandleUtility.ignoreRaySnapObjects = null; TODO: version...
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        selectionHappened = true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) {
                        bool rayDrag = EditorGUI.actionKey && (evt.shift || rayDragEnabled); 
                        if (rayDrag) {
                            /* TODO: version
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();
                                */
                            object hit = HandleUtility.RaySnap (HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null) {
                                RaycastHit rh = (RaycastHit)hit;
                                float offset = 0;
                                /* TODO: ??
                                if (Tools.pivotMode == PivotMode.Center)
                                {
                                    float geomOffset = HandleUtility.CalcRayPlaceOffset(HandleUtility.ignoreRaySnapObjects, rh.normal);
                                    if (geomOffset != Mathf.Infinity)
                                    {
                                        offset = Vector3.Dot(position, rh.normal) - geomOffset;
                                    }
                                }
                                */
                                position = Handles.inverseMatrix.MultiplyPoint (rh.point + (rh.normal * offset));
                            } else {
                                rayDrag = false;
                            }
                        }
                        if (!rayDrag) {
                            // normal drag
                            s_CurrentMousePosition += new Vector2 (evt.delta.x, -evt.delta.y) * EditorGUIUtility.pixelsPerPoint;
                            Vector3 screenPos = Camera.current.WorldToScreenPoint (Handles.matrix.MultiplyPoint (s_StartPosition));
                            screenPos += (Vector3)(s_CurrentMousePosition - s_StartMousePosition);
                            position = Handles.inverseMatrix.MultiplyPoint (Camera.current.ScreenToWorldPoint (screenPos));

                            // Due to floating node inaccuracies, the back-and-forth transformations used may sometimes introduce
                            // tiny unintended movement in wrong directions. People notice when using a straight top/left/right ortho camera.
                            // In that case, just restrain the movement to the plane.
                            if (Camera.current.transform.forward == Vector3.forward || Camera.current.transform.forward == -Vector3.forward)
                                position.z = s_StartPosition.z;
                            if (Camera.current.transform.forward == Vector3.up || Camera.current.transform.forward == -Vector3.up)
                                position.y = s_StartPosition.y;
                            if (Camera.current.transform.forward == Vector3.right || Camera.current.transform.forward == -Vector3.right)
                                position.x = s_StartPosition.x;

                            /* TODO: ??
                            if (Tools.vertexDragging)
                            {
                                if (HandleUtility.ignoreRaySnapObjects == null)
                                    Handles.SetupIgnoreRaySnapObjects();
                                Vector3 near;
                                if (HandleUtility.FindNearestVertex(evt.mousePosition, null, out near)) {
                                    position = Handles.inverseMatrix.MultiplyNode(near);
                                }
                            }
                            */
                            if (EditorGUI.actionKey && !evt.shift) {
                                Vector3 delta = position - s_StartPosition;
                                delta.x = Handles.SnapValue (delta.x, snap.x);
                                delta.y = Handles.SnapValue (delta.y, snap.y);
                                delta.z = Handles.SnapValue (delta.z, snap.z);
                                position = s_StartPosition + delta;
                            }
                        }
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    int hotcontrol = GUIUtility.hotControl;
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2)) {
                        GUIUtility.hotControl = 0;
                        //HandleUtility.ignoreRaySnapObjects = null; TODO: version
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping (0);
                    }
                    break;
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint ();
                    break;
                case EventType.Repaint:
                    Color temp = Handles.color;

                    if (id == GUIUtility.hotControl) {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    } else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0) {
                        temp = Handles.color;
                        Handles.color = preselectedNodeColor;
                    }

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Repaint);
                    Handles.matrix = origMatrix;

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
            return position;
        }
        /// <summary>
		/// Slider move handle tool based on Unity method.
		/// </summary>
		/// <param name="id">Handle control identifier.</param>
		/// <param name="position">Position in world space.</param>
		/// <param name="rotation">Rotation.</param>
		/// <param name="size">Size of the handle.</param>
		/// <param name="snap">Snap to grid.</param>
		/// <param name="handleFunction"></param>
		/// <returns>The position of the handle, dragged or not.</returns>
        public Vector3 SliderMoveHandle (int id, Vector3 position, Quaternion rotation, float size, Vector3 snap, Handles.CapFunction handleFunction) {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint (position);
            Matrix4x4 origMatrix = Handles.matrix;
            //VertexSnapping.HandleMouseMove(id); TODO: Version
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id)) {
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Layout);
                    Handles.matrix = origMatrix;
                    break;
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0) {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        //HandleUtility.ignoreRaySnapObjects = null; TODO: version...
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        selectionHappened = true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) {
                        bool rayDrag = EditorGUI.actionKey && evt.shift;
                        if (rayDrag) {
                            /* TODO: version
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();
                                */
                            object hit = HandleUtility.RaySnap (HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null) {
                                RaycastHit rh = (RaycastHit)hit;
                                float offset = 0;
                                /* TODO: ??
                                if (Tools.pivotMode == PivotMode.Center)
                                {
                                    float geomOffset = HandleUtility.CalcRayPlaceOffset(HandleUtility.ignoreRaySnapObjects, rh.normal);
                                    if (geomOffset != Mathf.Infinity)
                                    {
                                        offset = Vector3.Dot(position, rh.normal) - geomOffset;
                                    }
                                }
                                */
                                position = Handles.inverseMatrix.MultiplyPoint (rh.point + (rh.normal * offset));
                            } else {
                                rayDrag = false;
                            }
                        }
                        if (!rayDrag) {
                            // normal drag
                            s_CurrentMousePosition += new Vector2 (evt.delta.x, -evt.delta.y) * EditorGUIUtility.pixelsPerPoint;
                            Vector3 screenPos = Camera.current.WorldToScreenPoint (Handles.matrix.MultiplyPoint (s_StartPosition));
                            screenPos += (Vector3)(s_CurrentMousePosition - s_StartMousePosition);
                            position = Handles.inverseMatrix.MultiplyPoint (Camera.current.ScreenToWorldPoint (screenPos));

                            // Due to floating node inaccuracies, the back-and-forth transformations used may sometimes introduce
                            // tiny unintended movement in wrong directions. People notice when using a straight top/left/right ortho camera.
                            // In that case, just restrain the movement to the plane.
                            if (Camera.current.transform.forward == Vector3.forward || Camera.current.transform.forward == -Vector3.forward)
                                position.z = s_StartPosition.z;
                            if (Camera.current.transform.forward == Vector3.up || Camera.current.transform.forward == -Vector3.up)
                                position.y = s_StartPosition.y;
                            if (Camera.current.transform.forward == Vector3.right || Camera.current.transform.forward == -Vector3.right)
                                position.x = s_StartPosition.x;

                            /* TODO: ??
                            if (Tools.vertexDragging)
                            {
                                if (HandleUtility.ignoreRaySnapObjects == null)
                                    Handles.SetupIgnoreRaySnapObjects();
                                Vector3 near;
                                if (HandleUtility.FindNearestVertex(evt.mousePosition, null, out near)) {
                                    position = Handles.inverseMatrix.MultiplyNode(near);
                                }
                            }
                            */
                            if (EditorGUI.actionKey && !evt.shift) {
                                Vector3 delta = position - s_StartPosition;
                                delta.x = Handles.SnapValue (delta.x, snap.x);
                                delta.y = Handles.SnapValue (delta.y, snap.y);
                                delta.z = Handles.SnapValue (delta.z, snap.z);
                                position = s_StartPosition + delta;
                            }
                        }
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2)) {
                        GUIUtility.hotControl = 0;
                        //HandleUtility.ignoreRaySnapObjects = null; TODO: version
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping (0);
                    }
                    break;
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint ();
                    break;
                case EventType.Repaint:
                    Color temp = Handles.color;

                    if (id == GUIUtility.hotControl) {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    } else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0) {
                        temp = Handles.color;
                        Handles.color = preselectedNodeColor;
                    }

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Repaint);
                    Handles.matrix = origMatrix;

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
            return position;
        }
        public void SelectableHandle (int id, Vector3 position, Quaternion rotation, float size, Handles.CapFunction handleFunction) {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint (position);
            Matrix4x4 origMatrix = Handles.matrix;
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id)) {
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Layout);
                    Handles.matrix = origMatrix;
                    break;
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0) {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        selectionHappened = true;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2)) {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping (0);
                    }
                    break;
                case EventType.Repaint:
                    Color temp = Handles.color;

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Repaint);
                    Handles.matrix = origMatrix;

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
        }
		/// <summary>
		/// Draw the node candidates to add to the curve.
		/// </summary>
		/// <param name="sceneOffset">Offset of the curve.</param>
		/// <param name="scale">Scale of the curve.</param>
		/// <param name="handleFunction">Handle function for drawing.</param>
        void DrawAddNodeHandles (Vector3 sceneOffset, float scale, Handles.CapFunction handleFunction) {
            Event  currentEvent = Event.current;
            s_CurrentMousePosition = Vector2.zero;
            s_CurrentMousePosition.x = currentEvent.mousePosition.x;
            s_CurrentMousePosition.y = Camera.current.pixelHeight - currentEvent.mousePosition.y;
            bool isMouseInsideSceneView = SceneView.currentDrawingSceneView == EditorWindow.mouseOverWindow;

            if (isMouseInsideSceneView) {
                if (addNodeLowerLimit < 0f) {
                    Handles.DrawDottedLine (_curve.First ().position * scale + sceneOffset, _addNodeLowerPoint * scale + sceneOffset, 2.5f);
                }
                if (addNodeUpperLimit > 1f) {
                    Handles.DrawDottedLine (_curve.Last ().position * scale + sceneOffset, _addNodeUpperPoint * scale + sceneOffset, 2.5f);
                }
                s_curveRay = Camera.current.ScreenPointToRay (s_CurrentMousePosition);
                s_curvePoint = Vector3.zero;
                Vector3 pNorm = (Vector3.Cross(_curveBounds.min - _curveBounds.center, _curveBounds.max - _curveBounds.center)).normalized;
                if (pNorm == Vector3.zero) {
                    //Debug.Log ("Curve plane is zero...");
                    //pNorm = ((_curve.Last ().position - _curve.First ().position) - Camera.current.transform.forward) / 2f;
                    pNorm = Camera.current.transform.forward;
                    //Debug.Log ("pNorm: " + pNorm);
                }
                //s_curvePlane.SetNormalAndPosition (Camera.current.transform.forward, _curveBounds.center * scale + sceneOffset);
                s_curvePlane.SetNormalAndPosition (pNorm, _curveBounds.center * scale + sceneOffset);
                /*
                Handles.color = new Color (1f,1f,1f,0.3f);
                Handles.DrawSolidDisc (_curveBounds.center * scale + sceneOffset, pNorm, 3f);
                */
                /*
                s_curvePlane.Set3Points (_curveBounds.min * scale + sceneOffset,
                    _curveBounds.center * scale + sceneOffset,
                    _curveBounds.max * scale + sceneOffset);
                    */
                //s_curvePlane.SetNormalAndPosition (s_curvePlane.normal, _curveBounds.center * scale + sceneOffset);
                float enter = 0f;
                if (s_curvePlane.Raycast (s_curveRay, out enter)) {
                    s_curvePoint = s_curveRay.GetPoint (enter);
                }
                float t = 0.5f;
                //s_curvePoint = _curve.FindNearestPointTo ((s_curvePoint -sceneOffset) / scale, out t, addNodeLowerLimit, addNodeUpperLimit);
                s_curvePoint = _curve.FindNearestPointTo ((s_curvePoint - sceneOffset) / scale, out t, addNodeLowerLimit, addNodeUpperLimit);
                //Debug.Log ("Candidate point at t: " + t);
                //Handles.DrawSolidDisc (s_curvePoint, Camera.current.transform.forward, nodeSize * HandleUtility.GetHandleSize (s_curvePoint));
                float _nodeSize = nodeSize * HandleUtility.GetHandleSize (s_curvePoint);
                Handles.DrawSolidDisc (s_curvePoint * scale + sceneOffset,
                    Camera.current.transform.forward, 
                    _nodeSize);
                if (Event.current.type == EventType.MouseDown) {
                    bool validAdd = true;
                    if (onValidateAddNode != null) {
                        validAdd = onValidateAddNode (s_curvePoint);
                    }
                    if (debugEditorEvents) Debug.Log ($"onValidateAddNode: {validAdd}");
                    if (validAdd) {
                        int index = 0;
                        s_curvePoint = _curve.GetPositionAt (t, out index);
                        BezierNode newNode = new BezierNode (s_curvePoint);
                        Vector3 tangent = _curve.GetTangentAt (t);
                        newNode.handle1 = -tangent.normalized * 0.3f;
                        newNode.handle2 = tangent.normalized * 0.3f;
                        newNode.handleStyle = BezierNode.HandleStyle.None;
                        if (t > 1f) index = _curve.nodeCount;
                        else if (t < 0f) index = 0;
                        else index++;
                        onBeforeAddNode?.Invoke (newNode);
                        _curve.InsertNode (index, newNode);
                        Event.current.Use ();
                        onAddNode?.Invoke (newNode, index + 1, t);
                        ManageNodeToSelection (newNode, index);
                    }
                }
            } else {
                if (Event.current.type == EventType.MouseDown) {
                    editMode = EditMode.Selection;
                }
            }
		}
        #endregion

        #region Selection
        /// <summary>
        /// Manage nodes from a curve to be selected.
        /// </summary>
        /// <param name="curve">Curve whose nodes will be managed. When no mask or additive modes are active, the current selection is cleared and all the nodes in the curve become selected.</param>
        /// <param name="mask">If all the nodes from the curve are selected, then these are removed. Otherwise, the whole curve becomes part of the selection.</param>
        /// <param name="additive">If <c>true</c> all the nodes in the curve will be selected.</param>
        /// <returns><c>True</c> if the selection changes</returns>
        public bool ManagerCurveToSelection (BezierCurve curve, bool mask = false, bool additive = false)
        {   bool selectionChanged = false;
            // Mask or Additive selection.
            if (mask || additive) {
                // Check the nodes already in the selection.
                List<bool> isSelected = new List<bool> ();
                bool allSelected = true;
                for (int i = 0; i < curve.nodes.Count; i++) {
                    if (_selectedNodes.Contains (curve.nodes[i])) {
                        isSelected.Add (true);
                    } else {
                        isSelected.Add (false);
                        allSelected = false;
                    }
                }
                // Additive mode.
                if (additive) {
                    if (!allSelected) {
                        // Add those node not selected to complete the selection.
                        BezierNode node;
                        for (int i = 0; i < isSelected.Count; i++) {
                            if (!isSelected [i]) {
                                node = curve.nodes[i];
                                _selectedNodes.Add (node);
                                _selectedNodesIndex.Add (i);
                                _selectedCurveIds.Add (curve.guid);
                                _idToNode.Add (node.guid, node);
                                _nodeToCurve.Add (node.guid, curve.guid);
                                node.isSelected = true;
                            } 
                        }
                        selectionChanged = true;
                        onSelectionChanged (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                        if (debugEditorEvents) Debug.Log ($"onSelectionChanged: {curve.nodes.Count} nodes");
                    }
                }
                // Mask mode.
                else {
                    // If all are selected, remove them from the selection.
                    if (allSelected) {
                        // Create a list for the index of every node selected.
                        List<int> indicesToRemove = new List<int> ();
                        // Iterate all the curve nodes to get their index.
                        int indexToRemove;
                        for (int i = 0; i < curve.nodes.Count; i++) {
                            indexToRemove = _selectedNodes.IndexOf (curve.nodes [i]);
                            if (indexToRemove >= 0) {
                                indicesToRemove.Add (indexToRemove);
                            }
                        }
                        // Sort them in descending order.
                        indicesToRemove.Sort((a, b) => b.CompareTo(a));
                        BezierNode node;
                        foreach (int index in indicesToRemove) {
                            if (index >= 0 && index < _selectedNodes.Count) {
                                node = _selectedNodes[index];
                                _selectedNodes.RemoveAt (index);
                                _selectedNodesIndex.RemoveAt (index);
                                _selectedCurveIds.RemoveAt (index);
                                _idToNode.Remove (node.guid);
                                _nodeToCurve.Remove (node.guid);
                                node.isSelected = false;
                            }
                        }
                        selectionChanged = true;
                        onSelectionChanged (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                        if (debugEditorEvents) Debug.Log ($"onSelectionChanged: {curve.nodes.Count} nodes");
                    }
                    // Add those not added yet.
                    else {
                        BezierNode node;
                        for (int i = 0; i < isSelected.Count; i++) {
                            if (!isSelected [i]) {
                                node = curve.nodes[i];
                                _selectedNodes.Add (node);
                                _selectedNodesIndex.Add (i);
                                _selectedCurveIds.Add (curve.guid);
                                _idToNode.Add (node.guid, node);
                                _nodeToCurve.Add (node.guid, curve.guid);
                                node.isSelected = true;
                            } 
                        }
                        selectionChanged = true;
                        onSelectionChanged (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                        if (debugEditorEvents) Debug.Log ($"onSelectionChanged: {curve.nodes.Count} nodes");
                    }
                }
            }
            // Select only the nodes in the curve. 
            else {
                // Deselect everything
                for (int i = 0; i < _selectedNodes.Count; i++) {
                    _selectedNodes [i].isSelected = false;
                }
                ClearSelection (false, false);

                // Add all the nodes in the curve.
                BezierNode node;
                Guid curveGuid = curve.guid;
                List<int> nodeIndices = new List<int> ();
                List<Guid> curveGuids = new List<Guid> ();
                for (int i = 0; i < curve.nodes.Count; i++) {
                    node = curve.nodes[i];
                    _selectedNodes.Add (node);
                    _selectedNodesIndex.Add (i);
                    _selectedCurveIds.Add (curveGuid);
                    _idToNode.Add (node.guid, node);
                    _nodeToCurve.Add (node.guid, curveGuid);
                    node.isSelected = true;
                    nodeIndices.Add (i);
                    curveGuids.Add (curveGuid);
                }
                
                selectionChanged = true;
                onSelectionChanged?.Invoke (curve.nodes, nodeIndices, curveGuids);
                if (debugEditorEvents) Debug.Log ($"onSelectionChanged: {curve.nodes.Count} nodes");
            }
            if (selectionChanged)
                UpdatePivot ();
            return selectionChanged;
        }
		/// <summary>
		/// Handles the node selection.
		/// </summary>
		/// <param name="node">Node to add to the selection.</param>
		/// <param name="mask">If the selection mode is on to select multiple nodes (usually by pressing shift or ctrl).</param>
		/// <param name="additive"><c>True</c> if the node is to be added to the selection.</param>
		/// <returns><c>True</c> if the selection changes.</returns>
		private bool ManageNodeToSelection (BezierNode node, int index, bool mask = false, bool additive = false) {
			bool selectionChanged = false;
            if (multiselectEnabled && (mask || additive)) { // MULTISELECT
                if (additive) { // Add the node if is not part of the selection already, keep it if it already is.
                    if (!_selectedNodes.Contains (node)) {
                        _selectedNodes.Add (node);
                        _selectedNodesIndex.Add (index);
                        _selectedCurveIds.Add (curveId);
                        _idToNode.Add (node.guid, node);
                        _nodeToCurve.Add (node.guid, curveId);
                        node.isSelected = true;
                        selectionChanged = true;
                        onSelectNode?.Invoke (node);
                        if (debugEditorEvents) Debug.Log ($"onSelectNode: {node.guid}");
                    }
                } else { // Add the node if is not part of the selection already or remove it if it is
                    if (!_selectedNodes.Contains (node)) {
                        _selectedNodes.Add (node);
                        _selectedNodesIndex.Add (index);
                        _selectedCurveIds.Add (curveId);
                        _idToNode.Add (node.guid, node);
                        _nodeToCurve.Add (node.guid, curveId);
                        node.isSelected = true;
                        onSelectNode?.Invoke (node);
                        if (debugEditorEvents) Debug.Log ($"onSelectNode: {node.guid}");
                    } else {
                        int indexToRemove = _selectedNodes.IndexOf (node);
                        if (indexToRemove >= 0) {
                            _selectedNodes.RemoveAt (indexToRemove);
                            _selectedNodesIndex.RemoveAt (indexToRemove);
                            _selectedCurveIds.RemoveAt (indexToRemove);
                            _idToNode.Remove (node.guid);
                            _nodeToCurve.Remove (node.guid);
                            node.isSelected = false;
                            onDeselectNode?.Invoke (node);
                            if (debugEditorEvents) Debug.Log ($"onDeselectNode: {node.guid}");
                        }
                    }
                    selectionChanged = true;
                }
            } else { // SINGLE SELECT
                // Node selection handling with single selection.
                if (_selectedNodes.Count == 1  && _selectedNodes[0] == node) {
                    // Same node selected
                } else {
                    // Deselect everything
                    for (int i = 0; i < _selectedNodes.Count; i++) {
                        _selectedNodes [i].isSelected = false;
                    }
                    ClearSelection (false, false);
                    _selectedNodes.Add (node);
                    _selectedNodesIndex.Add (index);
                    _selectedCurveIds.Add (curveId);
                    _idToNode.Add (node.guid, node);
                    _nodeToCurve.Add (node.guid, curveId);
                    node.isSelected = true;
                    selectionChanged = true;
                    onSelectNode?.Invoke (node);
                    if (debugEditorEvents) Debug.Log ($"onSelectNode: {node.guid}");
                }
            }
            // Update Pivot.
            if (selectionChanged)
                UpdatePivot ();

			return selectionChanged;
		}
        bool _AddNodeToSelection (BezierNode node, int index, System.Guid curveId) {
            if (node != null && !_idToNode.ContainsKey(node.guid)) {
                _selectedNodes.Add (node);
                _selectedNodesIndex.Add (index);
                _selectedCurveIds.Add (curveId);
                _idToNode.Add (node.guid, node);
                _nodeToCurve.Add (node.guid, curveId);
                node.isSelected = true;
                return true;
            }
            return false;
        }
        public bool AddNodeToSelection (BezierNode node, int index, System.Guid curveId)
        {
            bool result = _AddNodeToSelection (node, index, curveId);
            if (result) {
                onSelectionChanged?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                if (debugEditorEvents) Debug.Log ($"onSelectionChanged: {_selectedNodes.Count} nodes.");
            }
            return result;
        }
        public bool AddNodesToSelection (List<BezierNode> nodes, List<int> indexes, List<System.Guid> curveIds)
        {
            bool added = false;
            if (nodes.Count == curveIds.Count) {
                for (int i = 0; i < nodes.Count; i++) {
                    if (_AddNodeToSelection (nodes[i], indexes[i], curveIds[i])) {
                        added = true;
                }
                    }
            }
            if (added) {
                onSelectionChanged?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                if (debugEditorEvents) Debug.Log ($"onSelectionChanged: {_selectedNodes.Count} nodes.");
            }
            return added;
        }
        private void UpdatePivot ()
        {
            if (pivotMode > PivotMode.Origin && pivotMode < PivotMode.Node) {
                Vector3 newPivot = Vector3.zero;
                if (_selectedNodes.Count > 0) {
                    if (pivotMode < PivotMode.FirstNode) {
                        newPivot = GetSelectionCenter (pivotMode == PivotMode.CenterBottom);
                    } else if (pivotMode == PivotMode.FirstNode) {
                        newPivot = _selectedNodes[0].position;
                    } else {
                        newPivot = _selectedNodes[_selectedNodes.Count - 1].position;
                    }
                }
                onBeforeSetPivot?.Invoke (newPivot, pivotMode);
                Pivot = newPivot;
                onSetPivot?.Invoke (newPivot, pivotMode);
            }
        }
        /// <summary>
        /// Gets the average position of all the nodes in the selection.
        /// </summary>
        /// <param name="toCenter">If true the center is placed at the bottom most Y position.</param>
        /// <returns>Average position for the selection.</returns>
        public Vector3 GetSelectionCenter (bool toCenter = false)
        {
            Vector3 totalPosition = Vector3.zero;
            float minY = float.NaN;
            foreach (BezierNode node in _selectedNodes) {
                totalPosition += node.position;
                if (toCenter && (float.IsNaN (minY) || node.position.y < minY)) {
                    minY = node.position.y;
                }
            }
            totalPosition = totalPosition / (float)_selectedNodes.Count;
            if (!float.IsNaN (minY)) totalPosition.y = minY;
            return totalPosition;
        }
		#endregion

        #region Pivot Methods
        /// <summary>
        /// Sets the pivot to the origin (Vector3.zero).
        /// </summary>
        public void SetPivotToOrigin ()
        {
            onBeforeSetPivot?.Invoke (Vector3.zero, PivotMode.Origin);
            Pivot = Vector3.zero;
            pivotMode = PivotMode.Origin;
            onSetPivot?.Invoke (Vector3.zero, PivotMode.Origin);
            pivotActionsIconOptions[0] = new GUIContent (pivotOrigin, "Pivot Mode");
        }
        /// <summary>
        /// Sets the pivot to the center of the current selection of nodes.
        /// If the selection changes, then the pivot updates its position.
        /// </summary>
        public void SetPivotToSelectionCenter ()
        {
            Vector3 selectionCenter = GetSelectionCenter ();
            onBeforeSetPivot?.Invoke (selectionCenter, PivotMode.Center);
            Pivot = selectionCenter;
            pivotMode = PivotMode.Center;
            onSetPivot?.Invoke (selectionCenter, PivotMode.Center);
            pivotActionsIconOptions[0] = new GUIContent (pivotCenter, "Pivot Mode");
        }
        /// <summary>
        /// Sets the pivot to the center of the current selection of nodes.
        /// If the selection changes, then the pivot updates its position.
        /// </summary>
        public void SetPivotToSelectionCenterBottom ()
        {
            Vector3 selectionCenter = GetSelectionCenter (true);
            onBeforeSetPivot?.Invoke (selectionCenter, PivotMode.CenterBottom);
            Pivot = selectionCenter;
            pivotMode = PivotMode.CenterBottom;
            onSetPivot?.Invoke (selectionCenter, PivotMode.CenterBottom);
            pivotActionsIconOptions[0] = new GUIContent (pivotCenterBottom, "Pivot Mode");
        }
        /// <summary>
        /// Sets the pivot to the position of a node used as reference.
        /// </summary>
        /// <param name="referenceNode">Reference node whose position will be used to place the pivot.</param>
        public void SetPivot (BezierNode referenceNode)
        {
            onBeforeSetPivot?.Invoke (referenceNode.position, PivotMode.Node);
            Pivot = referenceNode.position;
            pivotMode = PivotMode.Node;
            onSetPivot?.Invoke (referenceNode.position, PivotMode.Node);
            pivotActionsIconOptions[0] = new GUIContent (pivotCenter, "Pivot Mode");
        }
        /// <summary>
        /// Sets the pivot to an assigned position in the curves space.
        /// </summary>
        /// <param name="pivotPosition">Free position for the pivot.</param>
        public void SetPivot (Vector3 pivotPosition)
        {
            onBeforeSetPivot?.Invoke (pivotPosition, PivotMode.Free);
            Pivot = pivotPosition;
            pivotMode = PivotMode.Free;
            onSetPivot?.Invoke (pivotPosition, PivotMode.Free);
            pivotActionsIconOptions[0] = new GUIContent (pivotCenter, "Pivot Mode");
        }
        /// <summary>
        /// Sets the pivot to the first selected node.
        /// </summary>
        public void SetPivotToFirstNode ()
        {
            if (_selectedNodes.Count > 0) {
                onBeforeSetPivot?.Invoke (_selectedNodes[0].position, PivotMode.FirstNode);
                Pivot = _selectedNodes[0].position;
                pivotMode = PivotMode.FirstNode;
                onSetPivot?.Invoke (_selectedNodes[0].position, PivotMode.FirstNode);
                pivotActionsIconOptions[0] = new GUIContent (pivotFirst, "Pivot Mode");
            }
        }
        /// <summary>
        /// Sets the pivot to the last selected node.
        /// </summary>
        public void SetPivotToLastNode ()
        {
            if (_selectedNodes.Count > 0) {
                Vector3 position = _selectedNodes[_selectedNodes.Count - 1].position;
                onBeforeSetPivot?.Invoke (position, PivotMode.LastNode);
                Pivot = position;
                pivotMode = PivotMode.LastNode;
                onSetPivot?.Invoke (position, PivotMode.LastNode);
                pivotActionsIconOptions[0] = new GUIContent (pivotLast, "Pivot Mode");
            }
        }
        #endregion

		#region Node Methods
		/// <summary>
		/// Moves the selection of nodes using an offset.
		/// </summary>
		/// <param name="offset">Offset to move.</param>
		public void DragSelectedNodes (Vector3 offset)
        {
			if (onCheckDragNodes != null) {
				offset = onCheckDragNodes (offset);
                if (debugEditorEvents) Debug.Log ($"OnCheckDragNodes: {_selectedNodes.Count} nodes");
			}
            offsetStep = offset - lastKnownOffset;
			if (offset != Vector3.zero) {
				onBeforeDragNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
				for (int i = 0; i < _selectedNodes.Count; i++) {
                    _nodeListenEvents = _selectedNodes[i].listenEvents;
                    _selectedNodes[i].listenEvents = false;
					_selectedNodes[i].position += offset;
                    _selectedNodes[i].listenEvents = _nodeListenEvents;
				}
				onDragNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
				if (debugEditorEvents) Debug.Log ($"OnDragNodes: {_selectedNodes.Count} nodes");
			}
            lastKnownOffset = offset;
		}
        /// <summary>
		/// Rotates the selection of nodes using an quaternion difference.
		/// </summary>
		/// <param name="offset">Offset to rotate.</param>
		public void RotateSelectedNodes (Quaternion rotation, Vector3 pivot)
        {
			if (rotation != Quaternion.identity) {
				onBeforeRotateNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                Vector3 translatedPoint, rotatedPoint;
				for (int i = 0; i < _selectedNodes.Count; i++) {
                    _nodeListenEvents = _selectedNodes[i].listenEvents;
                    _selectedNodes[i].listenEvents = false;
                    translatedPoint = _selectedNodes[i].position - pivot;
                    rotatedPoint = rotation * translatedPoint;
                    rotatedPoint += pivot;
					_selectedNodes[i].position = rotatedPoint;
                    _selectedNodes[i].handle1 = rotation * _selectedNodes[i].handle1;
                    if (_selectedNodes[i].handleStyle != BezierNode.HandleStyle.Auto && 
                        _selectedNodes[i].handleStyle != BezierNode.HandleStyle.Aligned)
                    {
                        _selectedNodes[i].handle2 = rotation * _selectedNodes[i].handle2;
                    }
                    _selectedNodes[i].listenEvents = _nodeListenEvents;
				}
				onRotateNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
				if (debugEditorEvents) Debug.Log ($"OnRotateNodes: {_selectedNodes.Count} nodes");
			}
		}
        /// <summary>
		/// Scales the selection of nodes using an Vector3 scale value.
		/// </summary>
		/// <param name="scale">Offset to scale.</param>
		public void ScaleSelectedNodes (Vector3 scale, Vector3 pivot)
        {
			if (scale != Vector3.one) {
				onBeforeScaleNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                Vector3 translatedPoint, scaledPoint;
				for (int i = 0; i < _selectedNodes.Count; i++) {
                    _nodeListenEvents = _selectedNodes[i].listenEvents;
                    _selectedNodes[i].listenEvents = false;
                    translatedPoint = _selectedNodes[i].position - pivot;
                    scaledPoint = Vector3.Scale (translatedPoint, scale);
                    scaledPoint += pivot;
					_selectedNodes[i].position = scaledPoint;
                    _selectedNodes[i].handle1 = Vector3.Scale (_selectedNodes[i].handle1, scale);
                    if (_selectedNodes[i].handleStyle != BezierNode.HandleStyle.Auto && 
                        _selectedNodes[i].handleStyle != BezierNode.HandleStyle.Aligned)
                    {
                        _selectedNodes[i].handle2 = Vector3.Scale (_selectedNodes[i].handle2, scale);
                    }
                    _selectedNodes[i].listenEvents = _nodeListenEvents;
				}
				onScaleNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
				if (debugEditorEvents) Debug.Log ($"OnScaleNodes: {_selectedNodes.Count} nodes");
			}
		}
        /// <summary>
		/// Move the selection of nodes using an offset.
		/// </summary>
		/// <param name="offset">Offset to move.</param>
		public void SnapSelectedNodes (BezierCurve.Axis axis, BezierNode referenceNode = null)
        {
            if (referenceNode == null) {
                referenceNode = _selectedNodes[_selectedNodes.Count - 1];
            }
            onBeforeDragNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
            _curve.SnapNodesToAxis (_selectedNodesIndex, axis, referenceNode);
            onDragNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
            //CallCurveChangedDelegate ();
		}
		/// <summary>
		/// Remove nodes in the selection.
		/// </summary>
		/// <returns>True if the nodes were removed.</returns>
		public bool RemoveSelectedNodes ()
        {
			bool canRemove = true;
			if (onCheckRemoveNodes != null) {
				canRemove = onCheckRemoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
			}
			if (canRemove) {
				onBeforeRemoveNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
				for (int i = 0; i < _selectedNodes.Count; i++) {
                    if (!deleteTerminalNodesEnabled && (_selectedNodes [i].isFirstNode ||_selectedNodes [i].isLastNode)) {
                        Debug.LogWarning ("Deleting curve terminal node " + _selectedNodes [i].guid + " is not allowed. Skipped.");
                    } else {
					    _selectedNodes[i].curve.RemoveNode (_selectedNodesIndex[i]);
                    }
				}
				onRemoveNodes?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
				ClearSelection ();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Clears the selection of nodes.
		/// </summary>
		public void ClearSelection (bool force = false, bool callOnSelectionChanged = true) {
            for (int i = 0; i < _selectedNodes.Count; i++) {
                _selectedNodes [i].isSelected = false;
            }
            _selectedNodes.Clear ();
            _selectedNodesIndex.Clear ();
            _selectedCurveIds.Clear ();
            if (callOnSelectionChanged) {
                onSelectionChanged?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                if (debugEditorEvents) Debug.Log ($"onSelectionChanged: {_selectedNodes.Count} nodes.");
            }
            _idToNode.Clear ();
            _nodeToCurve.Clear ();
		}
        /// <summary>
        /// Update the selected nodes from a curve.
        /// </summary>
        public void UpdateSelection () {
            _selectedNodes.Clear ();
            _selectedNodesIndex.Clear ();
            _selectedCurveIds.Clear ();
            onSelectionChanged?.Invoke (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
            if (debugEditorEvents) Debug.Log ($"onSelectionChanged: {_selectedNodes.Count} nodes.");
            _idToNode.Clear ();
            _nodeToCurve.Clear ();
            if (_curve != null) {
                for (int i = 0; i < _curve.nodes.Count; i++) {
                    if (_curve.nodes[i].isSelected) {
                        _AddNodeToSelection (_curve.nodes[i], i, System.Guid.Empty); // TODO: need to set curve id.
                    }
                }
            }
        }
		/// <summary>
		/// Changes the style of a handle of a node.
		/// </summary>
		/// <param name="node">Node owner of the handles.</param>
		/// <param name="index">Node index in the curve.</param>
		/// <param name="handleStyle">Handle style to change to.</param>
        /// <param name="isLastNode">Node is at the end of the curve.</param>
		/// <returns><c>True</c> if the handle style was changed.</returns>
		public bool ChangeNodeHandleStyle (BezierNode node, int index, BezierNode.HandleStyle handleStyle, bool isLastNode) {
			if (node.handleStyle != handleStyle) {
                onBeforeEditNodeStyle?.Invoke (node, index);
                if (debugEditorEvents) Debug.Log ($"onBeforeEditNodeStyle: {node.guid}, {handleStyle}");
				onBeforeEditNode?.Invoke (node, index);
                if (debugEditorEvents) Debug.Log ($"onBeforeEditNode: {node.guid}");
                CallBeforeCurvesChange (new List<BezierCurve> () {node.curve});
				node.handleStyle = handleStyle;
                // Set handle values to reset mirrored positions if handle style is connected.
                if (handleStyle == BezierNode.HandleStyle.Aligned || handleStyle == BezierNode.HandleStyle.Auto) {
                    if (isLastNode) {
                        node.handle1 = node.handle1;
                    } else {
                        node.handle2 = node.handle2;
                    }
                }
                CallCurvesChange (new List<BezierCurve> () {node.curve});
                onEditNodeStyle?.Invoke (node, index);
                if (debugEditorEvents) Debug.Log ($"onEditNodeStyle: {node.guid}, {handleStyle}");
				onEditNode?.Invoke (node, index);
                if (debugEditorEvents) Debug.Log ($"onEditNode: {node.guid}");
				return true;
			}
			return false;
		}
        /// <summary>
        /// Process a curve.
        /// </summary>
        /// <param name="targetCurve">Curve instance to process.</param>
        public void ProcessCurve (BezierCurve targetCurve) {
            List<BezierCurve> modifiedCurves = new List<BezierCurve> ();
            curve.Process ();
            modifiedCurves.Add (targetCurve);
            onChangeCurves?.Invoke (modifiedCurves);
            if (debugEditorEvents) Debug.Log ($"OnCurvesChange: {modifiedCurves.Count}");
        }
        /// <summary>
        /// Process all the curves nodes belong to.
        /// </summary>
        /// <param name="targetNodes">List of nodes to process their curves.</param>
        public void ProcessNodesCurves (List<BezierNode> targetNodes) {
            List<BezierCurve> modifiedCurves = new List<BezierCurve> ();
            foreach (BezierNode node in targetNodes) {
                if (node != null && !modifiedCurves.Contains (node.curve)) {
                    node.curve.Process ();
                    modifiedCurves.Add (node.curve);
                }
            }
            onChangeCurves?.Invoke (modifiedCurves);
            if (debugEditorEvents) Debug.Log ($"OnCurvesChange: {modifiedCurves.Count}");
        }
		#endregion

        #region Debug
        /// <summary>
        /// Sets GUI options for this editor.
        /// </summary>
        public void DrawDebugGUI () {
            // Debug Options
            bool isDebugEnabled = EditorGUILayout.Toggle ("Debug Enabled", debugEnabled);
            if (isDebugEnabled != debugEnabled) {
                debugEnabled = isDebugEnabled;
            }
            if (isDebugEnabled) {
                bool changed = false;
                bool isDebugShowPoints = EditorGUILayout.Toggle ("Show Points", debugShowPoints);
                if (isDebugShowPoints != debugShowPoints) {
                    debugShowPoints = isDebugShowPoints;
                    changed = true;
                }
                if (isDebugShowPoints) {
                    bool isDebugForward = EditorGUILayout.Toggle (" Show Point Forward", debugShowPointForward);
                    if (isDebugForward != debugShowPointForward) {
                        debugShowPointForward = isDebugForward;
                        changed = true;
                    }
                    bool isDebugNormal = EditorGUILayout.Toggle (" Show Point Normal", debugShowPointNormal);
                    if (isDebugNormal != debugShowPointNormal) {
                        debugShowPointNormal = isDebugNormal;
                        changed = true;
                    }
                    bool isDebugUp = EditorGUILayout.Toggle (" Show Point Up", debugShowPointUp);
                    if (isDebugUp != debugShowPointUp) {
                        debugShowPointUp = isDebugUp;
                        changed = true;
                    }
                    bool isDebugTangent = EditorGUILayout.Toggle (" Show Point Tangent", debugShowPointTangent);
                    if (isDebugTangent != debugShowPointTangent) {
                        debugShowPointTangent = isDebugTangent;
                        changed = true;
                    }
                    bool isDebugNodeLabels = EditorGUILayout.Toggle (" Show Point NodeLabels", debugShowNodeLabels);
                    if (isDebugNodeLabels != debugShowNodeLabels) {
                        debugShowNodeLabels = isDebugNodeLabels;
                        changed = true;
                    }
                    bool isDebugRelPos = EditorGUILayout.Toggle (" Show Point Relative Pos", debugShowRelativePos);
                    if (isDebugRelPos != debugShowRelativePos) {
                        debugShowRelativePos = isDebugRelPos;
                        changed = true;
                    }
                    bool isDebugNodeGUID = EditorGUILayout.Toggle (" Show Point NodeGUID", debugShowNodeGUID);
                    if (isDebugNodeGUID != debugShowNodeGUID) {
                        debugShowNodeGUID = isDebugNodeGUID;
                        changed = true;
                    }
                }
                bool isDebugShowFinePoints = EditorGUILayout.Toggle ("Show Fine Points", debugShowFinePoints);
                if (isDebugShowFinePoints != debugShowFinePoints) {
                    debugShowFinePoints = isDebugShowFinePoints;
                    changed = true;
                }
                bool isDebugShowCustomPoint = EditorGUILayout.Toggle ("Show Custom Point", debugShowCustomPoint);
                if (isDebugShowCustomPoint != debugShowCustomPoint) {
                    debugShowCustomPoint = isDebugShowCustomPoint;
                    changed = true;
                }
                if (isDebugShowCustomPoint) {
                    float customPointPos = EditorGUILayout.FloatField (" Custom Point Pos", debugCustomPointPosition);
                    if (customPointPos != debugCustomPointPosition) {
                        debugCustomPointPosition = customPointPos;
                        UpdateDebugCustomPoint ();
                        changed = true;
                    }
                }
                bool isDebugDrawSampleLine = EditorGUILayout.Toggle ("Draw Samples Line", debugDrawSampleLine);
                if (isDebugDrawSampleLine != debugDrawSampleLine) {
                    debugDrawSampleLine = isDebugDrawSampleLine;
                    changed = true;
                }

                if (changed) {
                    SceneView.RepaintAll ();
                }
            }
            EditorGUILayout.Space ();

            // Edit Mode
            EditMode current = _editMode;
            current = (EditMode)EditorGUILayout.EnumPopup ("Editor Mode", current);
            if (current != _editMode) {
                editMode = current;
            }
        }
        public void PrintDebugInfo () {
            int samplesTotal = 0;
            int pointsTotal = _curve.points.Count;
            for (int i = 0; i < _curve.bezierCurves.Count; i++) {
                samplesTotal += _curve.bezierCurves[i].samples.Count;
            }
            Debug.LogFormat ("Curve has {0} nodes, {1} samples, {2} points.", 
                _curve.nodes.Count,
                samplesTotal,
                pointsTotal);
            for (int i = 0; i < _curve.nodes.Count; i++) {
                Debug.LogFormat ("Node {0}:\t {1}, handle1({2}), handle2({3})", 
                    i, _curve.nodes[i].position, _curve.nodes[i].handle1, _curve.nodes[i].handle2);
            }
            int sampleCount = 0;
            for (int i = 0; i < _curve.bezierCurves.Count; i++) {
                for (int j = 0; j < _curve.bezierCurves[i].samples.Count; j++) {
                    Debug.LogFormat ("  Sample {0}, {1}", sampleCount, _curve.bezierCurves[i].samples[j].position);
                    sampleCount++;
                }
            }
        }
        public void DrawDebugInfo () {
            DrawDebugInfo (curve);
        }
        public void DrawDebugInfo (BezierCurve curve) {
            string nodesDesc = string.Empty;
            if (curve == null) {
                // No curve selected.
                nodesDesc += "No curve is selected.";
            } else {
                // Curve description.
                nodesDesc += string.Format ("Curve guid: {0}\n", curve.guid);
                nodesDesc += string.Format ("Curve auto process: {0}\n", curve.autoProcess);
                nodesDesc += string.Format ("Curve length: {0}, resolution: {1}\n", curve.length, curve.resolution);
                nodesDesc += string.Format ("  points: {0}\n", curve.points.Count);
                nodesDesc += string.Format ("  Noise Scale at Base: {0:0.000}, at Top: {1:0.000}\n", curve.noiseScaleAtFirstNode, curve.noiseScaleAtLastNode);
                nodesDesc += string.Format ("  Noise Factor at Base: {0:0.000}, at Top: {1:0.000}\n", curve.noiseFactorAtFirstNode, curve.noiseFactorAtLastNode);
                nodesDesc += string.Format ("  Noise Length Offset: {0:0.000}, Length Scale: {1:0.000}, Spares First Node: {2}\n", curve.noiseLengthOffset, curve.noiseLengthScale, curve.spareNoiseOffsetAtFirstPoint);
                nodesDesc += string.Format ("{0} nodes\n\n", curve.nodeCount);
                // Nodes description.
                if (_selectedNodes.Count == 0) {
                    nodesDesc += "No selected nodes.";
                } else {
                    nodesDesc += string.Format ("Selected Nodes: {0}\n", _selectedNodes.Count);
                    BezierNode node = selectedNode;
                    nodesDesc += string.Format ("Node ({0}) in curve ({1})\n", node.guid, node.curve.guid);
                    nodesDesc += string.Format ("  selected: {0}, connected: {1}\n", node.isSelected, node.isConnected);
                    nodesDesc += string.Format ("  at pos: {0}, at length: {1}\n", node.relativePosition, node.lengthPosition);
                    nodesDesc += string.Format ("  space position: {0}\n\n", node.position);
                }
                // Custom point descriptor
                if (debugShowCustomPoint && debugCustomPoint != null) {
                    nodesDesc += string.Format ("Custom point at pos {0} and length {1}\n", debugCustomPoint.relativePosition, debugCustomPoint.lengthPosition);
                }
            }
            EditorGUILayout.HelpBox (nodesDesc, MessageType.None);
        }
        private void UpdateDebugCustomPoint () {
            if (_curve != null) {
                debugCustomPoint = _curve.GetPointAt (debugCustomPointPosition);
            }
        }
        #endregion

        #region GUI Icons (contained)
		/// <summary>
		/// Sprite sheets loaded to get sprites from them.
		/// </summary>
		/// <typeparam name="string">Path to the sprite sheet file.</typeparam>
		/// <typeparam name="Texture2D">Texture of the sprite sheet.</typeparam>
		/// <returns></returns>
		private static Dictionary<string, Texture2D> loadedSpriteSheets = new Dictionary<string, Texture2D> ();
        /// <summary>
        /// Clears all loaded sprites on this instance.
        /// </summary>
        private static void ClearSprites () {
            foreach (KeyValuePair<string, Texture2D> sprite in loadedSpriteSheets) {
                UnityEngine.Object.DestroyImmediate (sprite.Value);
            }
            loadedSpriteSheets.Clear ();
        }
        /// <summary>
		/// Loads a sprite sheet from a path.
		/// </summary>
		/// <param name="path">Path to texture.</param>
		/// <returns>Sprite sheet texture.</returns>
		private static Texture2D LoadSpriteSheet (string path) {
			Texture2D texture = null;
			if (loadedSpriteSheets.ContainsKey (path)) {
				texture = loadedSpriteSheets [path];
			} else {
                texture = Resources.Load (path) as Texture2D;
			}
			return texture;
		}
        /// <summary>
		/// Loads and crop a texture.
		/// </summary>
		/// <returns>The texture.</returns>
		/// <param name="path">Path.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="width">Width crop.</param>
		/// <param name="height">Height crop.</param>
		public static Texture2D LoadSprite (string path, int x, int y, int width, int height) {
			Texture2D texture = null;
			#if UNITY_EDITOR 
			texture = LoadSpriteSheet (path);
			texture = CropTexture (texture, x, y, width, height);
			#endif
			return texture;
		}
        /// <summary>
		/// Crops a texture using pixel coordinates.
		/// </summary> 
		/// <returns>The resulting texture.</returns>
		/// <param name="tex">Texture to crop.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public static Texture2D CropTexture (Texture2D tex, int x, int y, int width, int height) {
			if (tex == null)
				return null;
			x = Mathf.Clamp (x, 0, tex.width);
			y = Mathf.Clamp (y, 0, tex.height);
			if (x + width > tex.width)
				width = tex.width - x;
			else if (x + width < 1)
				width = 1;
			if (y + height > tex.height)
				height = tex.height - y;
			else if (y + height < 1)
				height = 1;
			Texture2D cropTex = new Texture2D (width, height);
			Color[] origCol = tex.GetPixels ();
			Color[] cropCol = new Color[width * height];
			int origPos = y * tex.width;
			int cropPos = 0;
			for (int j = 0; j < height; j++) {
				origPos += x;
				for (int i = 0; i < width; i++) {
					cropCol [cropPos] = origCol [origPos];
					origPos++;
					cropPos++;
				}
				origPos += tex.width - x - width;
			}
			cropTex.SetPixels (cropCol);
			cropTex.Apply (true, false);
			return cropTex;
		}
        #endregion
    }
}
