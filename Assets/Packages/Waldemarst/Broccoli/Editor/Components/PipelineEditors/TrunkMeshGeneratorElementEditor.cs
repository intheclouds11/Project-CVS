using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Model;
using Broccoli.Utils;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Trunk mesh generator node editor.
	/// </summary>
	[CustomEditor(typeof(TrunkMeshGeneratorElement))]
	public class TrunkMeshGeneratorElementEditor : PipelineElementEditor {
		#region Vars
		/// <summary>
		/// The trunk mesh generator node.
		/// </summary>
		public TrunkMeshGeneratorElement trunkMeshGeneratorElement;
		SerializedProperty propIntegrationMode;
		SerializedProperty propMinSpread;
		SerializedProperty propMaxSpread;
		SerializedProperty propMinRootsCount;
		SerializedProperty propMaxRootsCount;
		SerializedProperty propMinRootScaleAtBase;
		SerializedProperty propMaxRootScaleAtBase;
		SerializedProperty propMinRootScaleAtTop;
		SerializedProperty propMaxRootScaleAtTop;
		SerializedProperty propMinRootExposureAtBase;
		SerializedProperty propMaxRootExposureAtBase;
		SerializedProperty propMinRootExposureAtTop;
		SerializedProperty propMaxRootExposureAtTop;
		SerializedProperty propMinRootReach;
		SerializedProperty propMaxRootReach;
		SerializedProperty propMinAngleVariance;
		SerializedProperty propMaxAngleVariance;
		SerializedProperty propMinTwirl;
		SerializedProperty propMaxTwirl;
		SerializedProperty propMinTrunkScaleAtBase;
		SerializedProperty propMaxTrunkScaleAtBase;
		SerializedProperty propScaleCurve;
		SerializedProperty propRadialResolutionFactor;
		SerializedProperty propLengthResolutionFactor;
		/// <summary>
		/// The scale curve range.
		/// </summary>
		private static Rect scaleCurveRange = new Rect (0f, 0f, 1f, 1f);
		#endregion

		#region Messages
		private static string MSG_INTEGRATION_MODE = "Mode to mesh branches and root on the tree trunk.\n1. None: No children branches and roots integration to the trunk mesh." +
			"\n2. Simulate Roots: Crests for roots are simulated on the trunk mesh." +
			"\n3. Adaptative: Dinamycally integrates branches and roots from the tree structure into the trunk mesh.";
		private static string MSG_MIN_MAX_SPREAD = "Range along the trunk the mesh will take.";
		private static string MSG_MIN_MAX_ROOTS = "Number of roots to simulate around the trunk.";
		private static string MSG_MIN_MAX_ROOT_SCALE_AT_BASE = "Scale to apply to the simulated roots from the trunk girth.";
		private static string MSG_MIN_MAX_ROOT_SCALE_AT_TOP = "Scale to apply to the base girth of roots at the top of the trunk.";
		private static string MSG_MIN_MAX_ROOT_EXPOSURE_AT_BASE = "How much the crest of each root will expose from the trunk surface at the base of the trunk.";
		private static string MSG_MIN_MAX_ROOT_EXPOSURE_AT_TOP = "How much the crest of each root will expose from the trunk surface at the top trunk limit of the root.";
		private static string MSG_MIN_MAX_ROOT_REACH = "How long along the custom trunk length each root will create a crest.";
		private static string MSG_MIN_MAX_ANGLE_VARIANCE = "Angle variance between displacement points.";
		private static string MSG_MIN_MAX_TWIRL = "Twirl fo the displacement points around the trunk.";
		private static string MSG_MIN_MAX_SCALE_AT_BASE = "Scale applied at the girth of the base of the trunk.";
		private static string MSG_SCALE_CURVE = "Distribution of the girth scaling along the trunk.";
		private static string MSG_RADIAL_RESOLUTION_FACTOR = "Factor to multiply the number of radial segment at the trunk.";
		private static string MSG_LENGTH_RESOLUTION_FACTOR = "Factory to multiply the number of cross-sections along the trunk length.";
		#endregion

		#region Events
		/// <summary>
		/// Actions to perform on the concrete class when the enable event is raised.
		/// </summary>
		protected override void OnEnableSpecific () {
			trunkMeshGeneratorElement = target as TrunkMeshGeneratorElement;

			propIntegrationMode = GetSerializedProperty ("integrationMode");
			propMinSpread = GetSerializedProperty ("minSpread");
			propMaxSpread = GetSerializedProperty ("maxSpread");
			propMinRootsCount = GetSerializedProperty ("minRootsCount");
			propMaxRootsCount = GetSerializedProperty ("maxRootsCount");
			propMinRootScaleAtBase = GetSerializedProperty ("minRootScaleAtBase");
			propMaxRootScaleAtBase = GetSerializedProperty ("maxRootScaleAtBase");
			propMinRootScaleAtTop = GetSerializedProperty ("minRootScaleAtTop");
			propMaxRootScaleAtTop = GetSerializedProperty ("maxRootScaleAtTop");
			propMinRootExposureAtBase = GetSerializedProperty ("minRootExposureAtBase");
			propMaxRootExposureAtBase = GetSerializedProperty ("maxRootExposureAtBase");
			propMinRootExposureAtTop = GetSerializedProperty ("minRootExposureAtTop");
			propMaxRootExposureAtTop = GetSerializedProperty ("maxRootExposureAtTop");
			propMinRootReach = GetSerializedProperty ("minRootReach");
			propMaxRootReach = GetSerializedProperty ("maxRootReach");
			propMinAngleVariance = GetSerializedProperty ("minAngleVariance");
			propMaxAngleVariance = GetSerializedProperty ("maxAngleVariance");
			propMinTwirl = GetSerializedProperty ("minTwirl");
			propMaxTwirl = GetSerializedProperty ("maxTwirl");
			propMinTrunkScaleAtBase = GetSerializedProperty ("minTrunkScaleAtBase");
			propMaxTrunkScaleAtBase = GetSerializedProperty ("maxTrunkScaleAtBase");
			propScaleCurve = GetSerializedProperty ("scaleCurve");
			propRadialResolutionFactor = GetSerializedProperty ("radialResolutionFactor");
			propLengthResolutionFactor = GetSerializedProperty ("lengthResolutionFactor");
		}
		/// <summary>
		/// Raises the inspector GUI event.
		/// </summary>
		protected override void OnInspectorGUISpecific () {
			CheckUndoRequest ();

			UpdateSerialized ();

			EditorGUI.BeginChangeCheck ();

			EditorGUILayout.PropertyField (propIntegrationMode);
			ShowHelpBox (MSG_INTEGRATION_MODE);
			EditorGUILayout.Space ();

			if (propIntegrationMode.enumValueIndex != (int)TrunkMeshGeneratorElement.IntegrationMode.None) {
				EditorGUILayout.LabelField ("Trunk Shape", EditorStyles.boldLabel);
				// Spread
				FloatRangePropertyField (propMinSpread, propMaxSpread, 0f, 1f, "Spread");
				ShowHelpBox (MSG_MIN_MAX_SPREAD);
				EditorGUILayout.Space ();

				if (propIntegrationMode.enumValueIndex == (int)TrunkMeshGeneratorElement.IntegrationMode.SimulateRoots) {
					// Roots Count.
					IntRangePropertyField (propMinRootsCount, propMaxRootsCount, 1, 15, "Roots");
					ShowHelpBox (MSG_MIN_MAX_ROOTS);
					FloatRangePropertyField (propMinRootScaleAtBase, propMaxRootScaleAtBase, 0.1f, 1f, "Root Scale at Base");
					ShowHelpBox (MSG_MIN_MAX_ROOT_SCALE_AT_BASE);
					FloatRangePropertyField (propMinRootScaleAtTop, propMaxRootScaleAtTop, 0.1f, 1f, "Root Scale at Top");
					ShowHelpBox (MSG_MIN_MAX_ROOT_SCALE_AT_TOP);
					EditorGUILayout.Space ();

					FloatRangePropertyField (propMinRootReach, propMaxRootReach, 0.2f, 1f, "Root Reach"); 
					ShowHelpBox (MSG_MIN_MAX_ROOT_REACH);
					FloatRangePropertyField (propMinRootExposureAtBase, propMaxRootExposureAtBase, 0.1f, 0.9f, "Root Exposure at Base");
					ShowHelpBox (MSG_MIN_MAX_ROOT_EXPOSURE_AT_BASE);
					FloatRangePropertyField (propMinRootExposureAtTop, propMaxRootExposureAtTop, 0.1f, 0.9f, "Root Exposure at Top");
					ShowHelpBox (MSG_MIN_MAX_ROOT_EXPOSURE_AT_TOP);
					EditorGUILayout.Space ();

					// Displacement point angle variance
					FloatRangePropertyField (propMinAngleVariance, propMaxAngleVariance, 0f, 0.5f, "Angle Variance");
					ShowHelpBox (MSG_MIN_MAX_ANGLE_VARIANCE);
					// Twirl
					FloatRangePropertyField (propMinTwirl, propMaxTwirl, -2.5f, 2.5f, "Twirl");
					ShowHelpBox (MSG_MIN_MAX_TWIRL);
					EditorGUILayout.Space ();

					// Scale at Base
					FloatRangePropertyField (propMinTrunkScaleAtBase, propMaxTrunkScaleAtBase, 0.75f, 2f, "Trunk Scale");
					ShowHelpBox (MSG_MIN_MAX_SCALE_AT_BASE);

					// Scale Curve
					EditorGUILayout.CurveField (propScaleCurve, Color.green, scaleCurveRange);
					ShowHelpBox (MSG_SCALE_CURVE);
					EditorGUILayout.Space ();
				}
				else {
					// Roots Count.
					FloatRangePropertyField (propMinRootScaleAtBase, propMaxRootScaleAtBase, 0.1f, 1f, "Root Scale at Base");
					ShowHelpBox (MSG_MIN_MAX_ROOT_SCALE_AT_BASE);
					FloatRangePropertyField (propMinRootScaleAtTop, propMaxRootScaleAtTop, 0.1f, 1f, "Root Scale at Top");
					ShowHelpBox (MSG_MIN_MAX_ROOT_SCALE_AT_TOP);
					EditorGUILayout.Space ();

					FloatRangePropertyField (propMinRootReach, propMaxRootReach, 0.2f, 1f, "Root Reach"); 
					ShowHelpBox (MSG_MIN_MAX_ROOT_REACH);
					FloatRangePropertyField (propMinRootExposureAtBase, propMaxRootExposureAtBase, 0.1f, 0.9f, "Root Exposure at Base");
					ShowHelpBox (MSG_MIN_MAX_ROOT_EXPOSURE_AT_BASE);
					FloatRangePropertyField (propMinRootExposureAtTop, propMaxRootExposureAtTop, 0.1f, 0.9f, "Root Exposure at Top");
					ShowHelpBox (MSG_MIN_MAX_ROOT_EXPOSURE_AT_TOP);
					EditorGUILayout.Space ();

					// Displacement point angle variance
					/*
					FloatRangePropertyField (propMinAngleVariance, propMaxAngleVariance, 0f, 0.5f, "Angle Variance");
					ShowHelpBox (MSG_MIN_MAX_ANGLE_VARIANCE);
					*/
					// Twirl
					FloatRangePropertyField (propMinTwirl, propMaxTwirl, -2.5f, 2.5f, "Twirl");
					ShowHelpBox (MSG_MIN_MAX_TWIRL);
					EditorGUILayout.Space ();

					// Scale at Base
					FloatRangePropertyField (propMinTrunkScaleAtBase, propMaxTrunkScaleAtBase, 0.75f, 2f, "Trunk Scale");
					ShowHelpBox (MSG_MIN_MAX_SCALE_AT_BASE);

					// Scale Curve
					EditorGUILayout.CurveField (propScaleCurve, Color.green, scaleCurveRange);
					ShowHelpBox (MSG_SCALE_CURVE);
					EditorGUILayout.Space ();
				}

				EditorGUILayout.LabelField ("Mesh Resolution", EditorStyles.boldLabel);
				// Min Max Polygon Sides.
				EditorGUILayout.Slider (propRadialResolutionFactor, 1f, 4f, "Radial Resolution Factor");
				ShowHelpBox (MSG_RADIAL_RESOLUTION_FACTOR);

				EditorGUILayout.Slider (propLengthResolutionFactor, 1f, 5f, "Length Resolution Factor");
				ShowHelpBox (MSG_LENGTH_RESOLUTION_FACTOR);
			}
			EditorGUILayout.Space ();

			// Seed options.
			DrawSeedOptions ();

			if (EditorGUI.EndChangeCheck () &&
				propMinSpread.floatValue <= propMaxSpread.floatValue) {
				ApplySerialized ();
				UpdatePipeline (GlobalSettings.processingDelayHigh);
				RepaintCanvas ();
				SetUndoControlCounter ();
			}

			// Field descriptors option.
			DrawFieldHelpOptions ();
		}
		/// <summary>
		/// Raises the scene GUI event.
		/// </summary>
		/// <param name="sceneView">Scene view.</param>
		protected override void OnSceneGUI (SceneView sceneView) {
			/*
			BranchMeshBuilder branchMeshBuilder = BranchMeshBuilder.GetInstance ();
			TrunkMeshBuilder trunkMeshBuilder = (TrunkMeshBuilder) branchMeshBuilder.GetBranchMeshBuilder (BranchMeshBuilder.BuilderType.Trunk);
			BezierCurve bezierCurve = null;
			var enumerator = trunkMeshBuilder.baseCurves.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				bezierCurve = enumerator.Current.Value;
				break;
			}
			BezierCurveDraw.DrawCurve (bezierCurve, Vector3.zero, 3, Color.white, 2);
			//BezierCurveDraw.DrawCurvePoints (bezierCurve, Vector3.zero, 3, Color.white);
			BezierCurveDraw.DrawCurvePoints (bezierCurve, Vector3.zero, 3, Color.white);
			//BezierCurveDraw.DrawCurveFinePoints (bezierCurve, Vector3.zero, 3, Color.white);
			*/
		}
		#endregion
	}
}