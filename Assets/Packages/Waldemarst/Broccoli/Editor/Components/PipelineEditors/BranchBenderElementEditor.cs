using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Broccoli.Base;
using Broccoli.Pipe;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Branch bender node editor.
	/// </summary>
	[CustomEditor(typeof(BranchBenderElement))]
	public class BranchBenderElementEditor : PipelineElementEditor {
		#region Vars
		/// <summary>
		/// The branch bender node.
		/// </summary>
		public BranchBenderElement branchBenderElement;
		SerializedProperty propApplyJointSmoothing;
		SerializedProperty propApplyDirectionalBending;
		SerializedProperty propApplyNoise;
		SerializedProperty propSmoothJointStrength;
		SerializedProperty propForceAtTips;
		SerializedProperty propForceAtTrunk;
		SerializedProperty propHierarchyDistributionCurve;
		SerializedProperty propHorizontalAlignAtBase;
		SerializedProperty propHorizontalAlignStrength;
		SerializedProperty propHorizontalAlignHierarchyDistributionCurve;
		SerializedProperty propVerticalAlignAtTop;
		SerializedProperty propVerticalAlignStrength;
		SerializedProperty propVerticalAlignHierarchyDistributionCurve;
		SerializedProperty propNoiseType;
		SerializedProperty propNoiseAtTop;
		SerializedProperty propNoiseAtBase;
		SerializedProperty propNoiseScaleAtTop;
		SerializedProperty propNoiseScaleAtBase;
		#if BROCCOLI_DEVEL
		SerializedProperty propNoiseStrength;
		SerializedProperty propNoiseResolution;
		#endif
		SerializedProperty propSmoothRootJointStrength;
		SerializedProperty propForceAtRootTips;
		SerializedProperty propForceAtRootTrunk;
		SerializedProperty propRootHierarchyDistributionCurve;
		SerializedProperty propNoiseAtRootBase;
		SerializedProperty propNoiseAtRootBottom;
		SerializedProperty propNoiseScaleAtRootBase;
		SerializedProperty propNoiseScaleAtRootBottom;
		SerializedProperty propHasRandomNoiseOffset;
		SerializedProperty propNoiseOffset;
		private static Rect curveRange = new Rect (0f, 0f, 1f, 1f);
		int selectedToolbar = 0;
		#endregion

		#region Messages and GUI
		private static GUIContent noiseTypeLabel = new GUIContent ("Noise Type", "Type of noise to apply to branches and roots.");
		private static GUIContent noiseAtBaseLabel = new GUIContent ("Noise Strength at Base", "Amount of noise displacement to apply to branches near the base of the tree.");
		private static GUIContent noiseAtTopLabel = new GUIContent ("Noise Strength at Top", "Amount of noise displacement to apply to branches near the top of the tree.");
		private static GUIContent noiseScaleAtBaseLabel = new GUIContent ("Noise Scale at Base", "Resolution of the noise to apply to branches near the base of the tree.");
		private static GUIContent noiseScaleAtTopLabel = new GUIContent ("Noise Scale at Top", "Resolution of the noise to apply to branches near the top of the tree.");
		private static GUIContent noiseAtRootBaseLabel = new GUIContent ("Noise Strength at Base", "Amount of noise displacement to apply to roots near the base of the tree.");
		private static GUIContent noiseAtRootBottomLabel = new GUIContent ("Noise Strength at Bottom", "Amount of noise displacement to apply to roots far from the base of the tree.");
		private static GUIContent noiseScaleAtRootBaseLabel = new GUIContent ("Noise Scale at Base", "Resolution of the noise to apply to roots near the base of the tree.");
		private static GUIContent noiseScaleAtRootBottomLabel = new GUIContent ("Noise Scale at Bottom", "Resolution of the noise to apply to roots far from the base of the tree.");
		private static GUIContent hasRandomNoiseOffsetLabel = new GUIContent ("Random Noise Offset", "Applies a random offset to the noise.");
		private static GUIContent noiseOffsetLabel = new GUIContent ("Noise Offset", "Random offset applied to the noise.");
		private static string MSG_APPLY_SMOOTHING = "Check to smooth the transition areas between parent and child branches or roots.";
		private static string MSG_APPLY_DIRECTIONAL_BENDING = "Check to bend branches and roots relative to gravity.";
		private static string MSG_APPLY_NOISE = "Check to apply Perlin noise to branches and roots.";
		private static string MSG_SMOOTHING_STRENGTH = "Strength applied to the curve between a parent and a followup branch.";
		private static string MSG_FORCE_AT_TIPS = "Amount of bending force applied to terminal branches.";
		private static string MSG_FORCE_AT_TRUNK = "Amount of bending force applied to branches near the trunk.";
		private static string MSG_HIERARCHY_DISTRIBUTION_CURVE = "Distribution of bending from branches closer to the trunk and those afar.";
		private static string MSG_HORIZONTAL_ALIGN_AT_BASE = "How much the base of the branches are align to the horizon plane.";
		private static string MSG_HORIZONTAL_ALIGN_STRENGTH = "Horizontal alignment strength used to lerp from the original direction to the horizon plane.";
		private static string MSG_HORIZONTAL_ALIGN_HIERARCHY_CURVE = "Distribution of the horizontal alignment along the tree hierarchy.";
		private static string MSG_VERTICAL_ALIGN_AT_TOP = "How much the top of the branches are align with the gravity direction.";
		private static string MSG_VERTICAL_ALIGN_STRENGTH = "Vertical alignment strength used to lerp from the original direction to the gravity direction.";
		private static string MSG_VERTICAL_ALIGN_HIERARCHY_CURVE = "Distribution of the vertical alignment along the tree hierarchy.";
		private static string MSG_NOISE = "Amount of offset noise to apply to branches.";
		private static string MSG_NOISE_SCALE = "Perlin noise scale. Lower values gives more resolution to the noise.";
		private static string MSG_ROOT_SMOOTHING_STRENGTH = "Strength applied to the curve between a parent and a followup root.";
		private static string MSG_FORCE_AT_ROOT_TIPS = "Amount of bending force applied to terminal roots.";
		private static string MSG_FORCE_AT_ROOT_TRUNK = "Amount of bending force applied to roots near the trunk.";
		private static string MSG_ROOT_HIERARCHY_DISTRIBUTION_CURVE = "Distribution of bending from roots closer to the trunk and those afar.";
		private static string MSG_ROOT_NOISE = "Amount of offset noise to apply to roots.";
		private static string MSG_ROOT_NOISE_SCALE = "Perlin noise scale. Lower values gives more resolution to the noise.";
		private static string MSG_HAS_RANDOM_NOISE_OFFSET = "Applies random noise offset to the base of the branches.";
		private static string MSG_NOISE_OFFSET = "Random noise offset to the base of the branches.";
		#endregion

		#region Events
		/// <summary>
		/// Actions to perform on the concrete class when the enable event is raised.
		/// </summary>
		protected override void OnEnableSpecific () {
			branchBenderElement = target as BranchBenderElement;

			propApplyJointSmoothing = GetSerializedProperty ("applyJointSmoothing");
			propApplyDirectionalBending  = GetSerializedProperty ("applyDirectionalBending");
			propApplyNoise = GetSerializedProperty ("applyNoise");

			propSmoothJointStrength = GetSerializedProperty ("smoothJointStrength");
			propForceAtTips = GetSerializedProperty ("forceAtTips");
			propForceAtTrunk = GetSerializedProperty ("forceAtTrunk");
			propHierarchyDistributionCurve = GetSerializedProperty ("hierarchyDistributionCurve");
			propHorizontalAlignAtBase = GetSerializedProperty ("horizontalAlignAtBase");
			propHorizontalAlignStrength = GetSerializedProperty ("horizontalAlignStrength");
			propHorizontalAlignHierarchyDistributionCurve = GetSerializedProperty ("horizontalAlignHierarchyDistributionCurve");
			propVerticalAlignAtTop = GetSerializedProperty ("verticalAlignAtTop");
			propVerticalAlignStrength = GetSerializedProperty ("verticalAlignStrength");
			propVerticalAlignHierarchyDistributionCurve = GetSerializedProperty ("verticalAlignHierarchyDistributionCurve");

			propNoiseType = GetSerializedProperty ("noiseType");
			propNoiseAtBase = GetSerializedProperty ("noiseAtBase");
			propNoiseAtTop = GetSerializedProperty ("noiseAtTop");
			propNoiseScaleAtBase = GetSerializedProperty ("noiseScaleAtBase");
			propNoiseScaleAtTop = GetSerializedProperty ("noiseScaleAtTop");
			#if BROCCOLI_DEVEL
			propNoiseStrength = GetSerializedProperty ("noiseStrength");
			propNoiseResolution = GetSerializedProperty ("noiseResolution");
			#endif

			propSmoothRootJointStrength = GetSerializedProperty ("smoothRootJointStrength");
			propForceAtRootTips = GetSerializedProperty ("forceAtRootTips");
			propForceAtRootTrunk = GetSerializedProperty ("forceAtRootTrunk");
			propRootHierarchyDistributionCurve = GetSerializedProperty ("rootHierarchyDistributionCurve");
			propNoiseAtRootBase = GetSerializedProperty ("noiseAtRootBase");
			propNoiseAtRootBottom = GetSerializedProperty ("noiseAtRootBottom");
			propNoiseScaleAtRootBase = GetSerializedProperty ("noiseScaleAtRootBase");
			propNoiseScaleAtRootBottom = GetSerializedProperty ("noiseScaleAtRootBottom");

			propHasRandomNoiseOffset = GetSerializedProperty ("hasRandomNoiseOffset");
			propNoiseOffset = GetSerializedProperty ("noiseOffset");
		}
		/// <summary>
		/// Raises the inspector GUI event.
		/// </summary>
		protected override void OnInspectorGUISpecific () {
			CheckUndoRequest ();

			UpdateSerialized ();

			EditorGUI.BeginChangeCheck ();

			// Joint smoothing
			EditorGUILayout.PropertyField (propApplyJointSmoothing);
			ShowHelpBox (MSG_APPLY_SMOOTHING);
			// Directional bending
			EditorGUILayout.PropertyField (propApplyDirectionalBending);
			ShowHelpBox (MSG_APPLY_DIRECTIONAL_BENDING);
			EditorGUILayout.PropertyField (propApplyNoise);
			ShowHelpBox (MSG_APPLY_NOISE);
			EditorGUILayout.Space ();

			if (propApplyJointSmoothing.boolValue || propApplyDirectionalBending.boolValue || propApplyNoise.boolValue) {
				selectedToolbar = GUILayout.Toolbar (selectedToolbar, new string[] {"Branches", "Roots"});
				EditorGUILayout.Space ();
				if (selectedToolbar == 0) {
					// BRANCH JOINT SMOOTHING
					if (propApplyJointSmoothing.boolValue) {
						branchBenderElement.jointSmoothingFoldout = EditorGUILayout.Foldout (branchBenderElement.jointSmoothingFoldout, "Joint Smoothing");
						if (branchBenderElement.jointSmoothingFoldout) {
							EditorGUILayout.LabelField ("Branch Joint Smoothing", EditorStyles.boldLabel);
							EditorGUILayout.Slider (propSmoothJointStrength, 0f, 1f, "Smoothing Strength");
							ShowHelpBox (MSG_SMOOTHING_STRENGTH);
							EditorGUILayout.Space ();
						}
					}
					// BRANCH DIRECTIONAL BENDING
					if (propApplyDirectionalBending.boolValue) {
						branchBenderElement.directionalBendingFoldout = EditorGUILayout.Foldout (branchBenderElement.directionalBendingFoldout, "Directional Bending");
						if (branchBenderElement.directionalBendingFoldout) {
							EditorGUILayout.LabelField ("Branch Directional Bending", EditorStyles.boldLabel);
							EditorGUILayout.Slider (propForceAtTips, -1f, 1f, "Force at Tips");
							ShowHelpBox (MSG_FORCE_AT_TIPS);
							EditorGUILayout.Slider (propForceAtTrunk, -1f, 1f, "Force at Trunk");
							ShowHelpBox (MSG_FORCE_AT_TRUNK);
							EditorGUILayout.CurveField (propHierarchyDistributionCurve, Color.green, curveRange);
							ShowHelpBox (MSG_HIERARCHY_DISTRIBUTION_CURVE);
							EditorGUILayout.Space ();
							EditorGUILayout.LabelField ("Branch Directional Alignment", EditorStyles.boldLabel);
							EditorGUILayout.Slider (propHorizontalAlignAtBase, -1f, 1f, "H Align at Base");
							ShowHelpBox (MSG_HORIZONTAL_ALIGN_AT_BASE);
							EditorGUILayout.Slider (propHorizontalAlignStrength, 0f, 1f, "H Align Strength");
							ShowHelpBox (MSG_HORIZONTAL_ALIGN_STRENGTH);
							EditorGUILayout.CurveField (propHorizontalAlignHierarchyDistributionCurve, Color.green, curveRange);
							ShowHelpBox (MSG_HORIZONTAL_ALIGN_HIERARCHY_CURVE);
							EditorGUILayout.Slider (propVerticalAlignAtTop, -1f, 1f, "V Align at Base");
							ShowHelpBox (MSG_VERTICAL_ALIGN_AT_TOP);
							EditorGUILayout.Slider (propVerticalAlignStrength, 0f, 1f, "V Align Strength");
							ShowHelpBox (MSG_VERTICAL_ALIGN_STRENGTH);
							EditorGUILayout.CurveField (propVerticalAlignHierarchyDistributionCurve, Color.green, curveRange);
							ShowHelpBox (MSG_VERTICAL_ALIGN_HIERARCHY_CURVE);
							EditorGUILayout.Space ();
						}
					}
					// BRANCH NOISE
					if (propApplyNoise.boolValue) {
						branchBenderElement.noiseFoldout = EditorGUILayout.Foldout (branchBenderElement.noiseFoldout, "Noise");
						if (branchBenderElement.noiseFoldout) {
							EditorGUILayout.LabelField ("Branch Noise", EditorStyles.boldLabel);
							if (GlobalSettings.experimentalFastNoise) {
								EditorGUILayout.PropertyField (propNoiseType, noiseTypeLabel);
							}
							EditorGUILayout.Slider (propNoiseAtBase, 0f, 2f, noiseAtBaseLabel);
							EditorGUILayout.Slider (propNoiseAtTop, 0f, 4f, noiseAtTopLabel);
							ShowHelpBox (MSG_NOISE);
							EditorGUILayout.Slider (propNoiseScaleAtBase, 0f, 1.5f, noiseScaleAtBaseLabel);
							EditorGUILayout.Slider (propNoiseScaleAtTop, 0f, 1.5f, noiseScaleAtTopLabel);
							ShowHelpBox (MSG_NOISE_SCALE);
							EditorGUILayout.PropertyField (propHasRandomNoiseOffset, hasRandomNoiseOffsetLabel);
							ShowHelpBox (MSG_HAS_RANDOM_NOISE_OFFSET);
							if (!branchBenderElement.hasRandomNoiseOffset) {
								EditorGUI.indentLevel++;
								EditorGUILayout.Slider (propNoiseOffset, -1f, 1f, noiseOffsetLabel);
								ShowHelpBox (MSG_NOISE_OFFSET);
								EditorGUI.indentLevel--;
							}
							EditorGUILayout.Space ();
						}
					}
				} else {
					// ROOT JOINT SMOOTHING
					if (propApplyJointSmoothing.boolValue) {
						branchBenderElement.jointSmoothingFoldout = EditorGUILayout.Foldout (branchBenderElement.jointSmoothingFoldout, "Joint Smoothing");
						if (branchBenderElement.jointSmoothingFoldout) {							
							EditorGUILayout.LabelField ("Root Joint Smoothing", EditorStyles.boldLabel);
							EditorGUILayout.Slider (propSmoothRootJointStrength, 0f, 1f, "Smoothing Strength");
							ShowHelpBox (MSG_ROOT_SMOOTHING_STRENGTH);
							EditorGUILayout.Space ();
						}
					}
					if (propApplyDirectionalBending.boolValue) {
						branchBenderElement.directionalBendingFoldout = EditorGUILayout.Foldout (branchBenderElement.directionalBendingFoldout, "Directional Bending");
						if (branchBenderElement.directionalBendingFoldout) {
							EditorGUILayout.LabelField ("Root Directional Bending", EditorStyles.boldLabel);
							EditorGUILayout.Slider (propForceAtRootTips, -1f, 1f, "Force at Tips");
							ShowHelpBox (MSG_FORCE_AT_ROOT_TIPS);
							EditorGUILayout.Slider (propForceAtRootTrunk, -1f, 1f, "Force at Trunk");
							ShowHelpBox (MSG_FORCE_AT_ROOT_TRUNK);
							EditorGUILayout.CurveField (propRootHierarchyDistributionCurve, Color.green, curveRange);
							ShowHelpBox (MSG_ROOT_HIERARCHY_DISTRIBUTION_CURVE);
							EditorGUILayout.Space ();
						}
					}
					if (propApplyNoise.boolValue) {
						branchBenderElement.noiseFoldout = EditorGUILayout.Foldout (branchBenderElement.noiseFoldout, "Noise");
						if (branchBenderElement.noiseFoldout) {
							EditorGUILayout.LabelField ("Root Noise", EditorStyles.boldLabel);
							if (GlobalSettings.experimentalFastNoise) {
								EditorGUILayout.PropertyField (propNoiseType, noiseTypeLabel);
							}
							EditorGUILayout.Slider (propNoiseAtRootBase, 0f, 2f, noiseAtRootBaseLabel);
							EditorGUILayout.Slider (propNoiseAtRootBottom, 0f, 4f, noiseAtRootBottomLabel);
							ShowHelpBox (MSG_ROOT_NOISE);
							EditorGUILayout.Slider (propNoiseScaleAtRootBase, 0f, 1.5f, noiseScaleAtRootBaseLabel);
							EditorGUILayout.Slider (propNoiseScaleAtRootBottom, 0f, 1.5f, noiseScaleAtRootBottomLabel);
							ShowHelpBox (MSG_ROOT_NOISE_SCALE);
							EditorGUILayout.Space ();
						}
					}
				}
			}
			DrawSeparator ();

			// Seed options.
			DrawSeedOptions ();

			if (EditorGUI.EndChangeCheck ()) {
				ApplySerialized ();
				UpdatePipeline (GlobalSettings.processingDelayHigh);
				branchBenderElement.Validate ();
				SetUndoControlCounter ();
			}

			// Field descriptors option.
			DrawFieldHelpOptions ();
		}
		#endregion

		#region Debug
		#if BROCCOLI_DEVEL
		/// <summary>
		/// Displays specific debug information for the type of element selected on the pipeline.
		/// </summary>
		override public void DrawDebugInfoSpecific () {
			string debugInfo = pipelineElement.elementName;
			// Noise Info
			debugInfo += $"Apply Noise: {(branchBenderElement.applyNoise?"True":"False")}\n";
			if (branchBenderElement.applyNoise) {
				debugInfo += $"\tNoise Type: {branchBenderElement.noiseType}\n";
				debugInfo += $"\tNoise Resolution: {branchBenderElement.noiseResolution}\n";
				debugInfo += $"\tNoise Strength: {branchBenderElement.noiseStrength}\n";
				debugInfo += $"\tNoise at Base: {branchBenderElement.noiseAtBase}\n";
				debugInfo += $"\tNoise at Top: {branchBenderElement.noiseAtTop}\n";
				debugInfo += $"\tNoise Scale at Base: {branchBenderElement.noiseScaleAtBase}\n";
				debugInfo += $"\tNoise Scale at Top: {branchBenderElement.noiseScaleAtTop}\n";
				debugInfo += $"\tRandom Noise Offset: {branchBenderElement.hasRandomNoiseOffset}\n";
				debugInfo += $"\tNoise Offset: {branchBenderElement.noiseOffset}\n";
			}
			EditorGUILayout.HelpBox (debugInfo, MessageType.None);
			/*
			if (propApplyNoise.boolValue) {
				UpdateSerialized ();

				EditorGUILayout.Slider (propNoiseStrength, 0f, 30f, "Basic Noise Strength");
				EditorGUILayout.Slider (propNoiseResolution, 0f, 60f, "Basic Noise Resolution");
				EditorGUILayout.Space ();
				EditorGUILayout.Slider (propNoiseAtBase, 0f, 20f, noiseAtBaseLabel);
				EditorGUILayout.Slider (propNoiseAtTop, 0f, 40f, noiseAtTopLabel);
				EditorGUILayout.Slider (propNoiseScaleAtBase, 0f, 20f, noiseScaleAtBaseLabel);
				EditorGUILayout.Slider (propNoiseScaleAtTop, 0f, 30f, noiseScaleAtTopLabel);

				if (EditorGUI.EndChangeCheck ()) {
					ApplySerialized ();
					UpdatePipeline (GlobalSettings.processingDelayHigh);
					branchBenderElement.Validate ();
					SetUndoControlCounter ();
				}
			}
			*/
		}
		#endif
		#endregion
	}
}