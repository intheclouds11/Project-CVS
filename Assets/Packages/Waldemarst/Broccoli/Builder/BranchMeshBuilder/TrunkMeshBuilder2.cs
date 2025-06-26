using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Broccoli.Base;
using Broccoli.Model;

namespace Broccoli.Builder {
	/// <summary>
	/// Gives methods to help create mesh segments using BranchSkin instances.
	/// </summary>
	public class TrunkMeshBuilder2 : IBranchMeshBuilder {
		#region Class BranchInfo
		/// <summary>
		/// Class containing the information to process the mesh of a branch.
		/// </summary>
		public class BranchInfo {
			/// <summary>
			/// Modes on how to integrate roots to the trunk mesh.
			/// </summary>
			public enum RootMode {
				/// <summary>
				/// Roots are simulated at the trunk surface.
				/// </summary>
				Pseudo,
				/// <summary>
				/// The trunk mesh integrates with existing roots from the tree structure.
				/// </summary>
				Integration
			}
			/// <summary>
			/// Root mode to integrate to the trunk mesh.
			/// </summary>
			public RootMode rootMode = RootMode.Pseudo;
			public int displacementPoints = 3;
			public float girthAtBase;
			public float girthAtTop;
			public float minScaleAtBase;
			public float maxScaleAtBase;
			public float scaleAtBase;
			public float minAngleVariance;
			public float maxAngleVariance;
			public float range;
			public float rangeLength;
			public float twirl;
			public float strength;
			public float branchRoll = 0f;
			public int numberOfSegments = 5;
			public float trunkBranchSkinEndPosition = 0f;
			public AnimationCurve scaleCurve;
		}
		#endregion

		#region Vars
		public Dictionary<int, BranchInfo> branchInfos = new Dictionary<int, BranchInfo> ();
		public Dictionary<int, BezierCurve> baseCurves = new Dictionary<int, BezierCurve> ();
		public Dictionary<int, BezierCurve> topCurves = new Dictionary<int, BezierCurve> ();

		public float angleTolerance = 200f;
		//float segmentPosition = 0f;
		//float tTwirlAngle = 0f;
		float globalScale = 1f;
		/// <summary>
		/// The minimum polygon sides to use for meshing.
		/// </summary>
		public int minPolygonSides = 6;
		/// <summary>
		/// The maximum polygon sides to use for meshing.
		/// </summary>
		public int maxPolygonSides = 18;
		/*
		public float lengthPosResolution = 0.1f;
		*/
		#endregion

		#region Interface
		public virtual void SetAngleTolerance (float angleTolerance) {
			//this.angleTolerance = angleTolerance * 2.5f;
		}
		public virtual float GetAngleTolerance () {
			return angleTolerance;
		}
		public virtual void SetGlobalScale (float globalScale) { this.globalScale = globalScale; }
		public virtual float GetGlobalScale () { return this.globalScale; }
		/// <summary>
		/// Get the branch mesh builder type.
		/// </summary>
		/// <returns>Branch mesh builder type.</returns>
		public virtual BranchMeshBuilder.BuilderType GetBuilderType () {
			return BranchMeshBuilder.BuilderType.Trunk;
		}
		/// <summary>
		/// Called right after a BranchSkin is created.
		/// </summary>
		/// <param name="rangeIndex">Index of the branch skin range to process.</param>
		/// <param name="branchSkin">BranchSkin instance to process.</param>
		/// <param name="firstBranch">The first branch instance on the BranchSkin instance.</param>
		/// <param name="parentBranchSkin">Parent BranchSkin instance to process.</param>
		/// <param name="parentBranch">The parent branch of the first branch of the BranchSkin instance.</param>
		/// <returns>True if any processing gets done.</returns>
		public virtual bool PreprocessBranchSkinRange (
			int rangeIndex, 
			BranchMeshBuilder.BranchSkin branchSkin, 
			BroccoTree.Branch firstBranch, 
			BranchMeshBuilder.BranchSkin parentBranchSkin = null, 
			BroccoTree.Branch parentBranch = null)
		{
			// Adds relevant points to the branch skin, based on length resolution.
			for (int i = 0; i < branchSkin.ranges.Count; i++) {
				if (branchSkin.ranges[i].builderType == BranchMeshBuilder.BuilderType.Trunk &&
					branchSkin.ranges[i].to > branchSkin.ranges[i].from)
				{
					int subdivisions = branchSkin.ranges [i].subdivisions;
					float subdivisionStep = (branchSkin.ranges [i].to - branchSkin.ranges [i].from) / subdivisions;
					float relativeRelevantPosition = 0f;
					for (int j = 1; j <= subdivisions; j++) {
						relativeRelevantPosition = branchSkin.ranges [i].from + subdivisionStep * j;
						branchSkin.AddRelevantPosition (relativeRelevantPosition, subdivisionStep);
					}
					BranchInfo branchInfo = GetBranchInfo (firstBranch.id);
					if (branchInfo != null) {
						branchInfo.trunkBranchSkinEndPosition = branchSkin.ranges[rangeIndex].to;
					}
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Called per branchskin after the main mesh has been processed. Modifies an additional mesh to merge it with the one processed.
		/// </summary>
		/// <param name="mesh">Mesh to process.</param>
		/// <param name="rangeIndex">Index of the branch skin range to process.</param>
		/// <param name="branchSkin">BranchSkin instance to process.</param>
		/// <param name="firstBranch">The first branch instance on the BranchSkin instance.</param>
		/// <param name="parentBranchSkin">Parent BranchSkin instance to process.</param>
		/// <param name="parentBranch">The parent branch of the first branch of the BranchSkin instance.</param>
		/// <returns>True if any processing gets done.</returns>
		public virtual Mesh PostprocessBranchSkinRange (Mesh mesh, int rangeIndex, BranchMeshBuilder.BranchSkin branchSkin, BroccoTree.Branch firstBranch, BranchMeshBuilder.BranchSkin parentBranchSkin = null, BroccoTree.Branch parentBranch = null) {
			return null;
		}
		public virtual Vector3 GetBranchSkinPositionOffset (float positionAtBranch, BroccoTree.Branch branch, float rollAngle, Vector3 forward, BranchMeshBuilder.BranchSkin branchSkin) {
			return Vector3.zero;
		}
		#endregion

		#region Branches
		/// <summary>
		/// Registers values to process the mesh of a branch given its id.
		/// </summary>
		/// <param name="branchId"></param>
		/// <param name="scaleAtBase"></param>
		/// <param name="range"></param>
		/// <param name="strength"></param>
		/// <param name="scaleCurve"></param>
		public void RegisterPseudoTrunk (
			BroccoTree.Branch branch, 
			BranchMeshBuilder.BranchSkin branchSkin,
			int displacementPoints,
			float range,
			float minScaleAtBase,
			float maxScaleAtBase,
			float minAngleVariance,
			float maxAngleVariance,
			float twirl,
			float strength, 
			AnimationCurve scaleCurve,
			float radialStep)
		{
			// Return if the branch is already registered.
			if (branchInfos.ContainsKey (branch.id)) {
				return;
			}

			// Save the parameters for the branch.
			BranchInfo branchInfo = new BranchInfo ();
			branchInfo.displacementPoints = displacementPoints;
			branchInfo.girthAtBase = branchSkin.GetGirthAtPosition (0f, branch);
			branchInfo.girthAtTop = branchSkin.GetGirthAtPosition (range, branch);
			branchInfo.minScaleAtBase = minScaleAtBase;
			branchInfo.maxScaleAtBase = maxScaleAtBase;
			branchInfo.scaleAtBase = Random.Range (minScaleAtBase, maxScaleAtBase);
			branchInfo.minAngleVariance = minAngleVariance;
			branchInfo.maxAngleVariance = maxAngleVariance;
			branchInfo.twirl = twirl;
			branchInfo.range = range;
			branchInfo.rangeLength = range * branchSkin.length;
			branchInfo.strength = strength;
			branchInfo.scaleCurve = new AnimationCurve (scaleCurve.keys);
			branchInfo.branchRoll = branch.rollAngle;
			branchInfos.Add (branch.id, branchInfo);

			// Create curve at base of branch.
			BezierCurve baseCurve = GetBezierCircle (
				displacementPoints, 
				branchInfo.girthAtBase * globalScale, 
				minAngleVariance, 
				maxAngleVariance);
			AddDisplacement (baseCurve, minScaleAtBase, maxScaleAtBase);
			baseCurve.simplifyBias = BezierCurve.SimplifyBias.Distance;
			baseCurve.distanceStep = radialStep;
			baseCurve.Process ();
			baseCurves.Add (branch.id, baseCurve);
			branchInfo.numberOfSegments = baseCurve.points.Count;
		}
		#endregion

		#region Vertices
		public virtual Vector3[] GetPolygonAt (
			BranchMeshBuilder.BranchSkin branchSkin,
			int segmentIndex,
			ref List<float> radialPositions,
			float scale,
			float radiusScale = 1f)
		{
			return null;
		}
		/// <summary>
		/// Gets the number of segments (like polygon sides) as resolution for a branch position.
		/// </summary>
		/// <param name="branch">Branch containing the position and belonging to the BranchSkin instance.</param>
		/// <param name="branchPosition">Branch position.</param>
		/// <param name="branchSkin">BranchSkin instance.</param>
		/// <param name="branchSkinPosition">Position along the BranchSkin instance.</param>
		/// <param name="branchAvgGirth">Branch average girth.</param>
		/// <returns>The number polygon sides.</returns>
		public virtual int GetNumberOfSegments (
			BroccoTree.Branch branch,
			float branchPosition,
			BranchMeshBuilder.BranchSkin branchSkin, 
			float branchSkinPosition, 
			float branchAvgGirth)
		{
			//BranchMeshBuilder.BranchSkinRange range;
			//float tPosition = branchSkin.TranslateToPositionAtBuilderRange (branchSkinPosition, out range);
			//int numberOfSegments = (int)Mathf.Lerp (maxPolygonSides, minPolygonSides + 1, tPosition);
			BranchInfo branchInfo = branchInfos [branchSkin.id];
			return branchInfo.numberOfSegments;
			/*
			// Returns 0 for every segment except for the last welding segment.
			// This way the default mesh builder starts building right after the welding range.
			// The welding branch builder builds the welding mesh right after all branch meshes has been build.
			BranchInfo branchInfo = branchInfos [branchSkin.id];
			if (branchInfo != null && 
				Mathf.Approximately (branchSkinPosition, branchInfo.trunkBranchSkinEndPosition))
			{
				float girthPosition = (branchAvgGirth - branchSkin.minAvgGirth) / (branchSkin.maxAvgGirth - branchSkin.minAvgGirth);
				branchSkin.polygonSides = Mathf.Clamp (
					Mathf.RoundToInt (
						Mathf.Lerp (
							branchSkin.minPolygonSides,
							branchSkin.maxPolygonSides,
							girthPosition)), 
							branchSkin.minPolygonSides,
							branchSkin.maxPolygonSides);
				return branchSkin.polygonSides;
			}
			return 0;
			*/
		}
		#endregion

		#region Bezier Curve
		/// <summary>
		/// Get a circle bezier curve.
		/// </summary>
		/// <param name="pointyPoints">Half the number of nodes the curve will have.</param>
		/// <param name="radius">Rdius of the circle.</param>
		/// <param name="minAngleVariation">Minimum variation on the position of the nodes along the circle circumference.</param>
		/// <param name="maxAngleVariation">Maximum variation on the position of the nodes along the circle circumference.</param>
		/// <returns>Circular bezier curve.</returns>
		public static BezierCurve GetBezierCircle (int pointyPoints, float radius, float minAngleVariation, float maxAngleVariation) {
			// https://stackoverflow.com/questions/1734745/how-to-create-circle-with-b%C3%A9zier-curves
			// Handle length = (4/3)*tan(pi/(2n))
			BezierCurve curve = new BezierCurve ();
			pointyPoints *= 2;
			float stepAngle = Mathf.PI * 2 / (float)pointyPoints;
			float nodeX = 0f;
			float nodeY = 0f;
			BezierNode lastNode = null;
			float[] angles = new float[pointyPoints];
			float[] anglesDiff = new float[pointyPoints];
			float angleVariance = Random.Range (minAngleVariation, maxAngleVariation);
			float angleCorrection = Mathf.PI / 2f;

			// Get randomized angles.
			//for (int i = 0; i <= pointyPoints; i++) {
			for (int i = 0; i <= pointyPoints; i++) {
				if (i < pointyPoints) {
					angles[i] = i * stepAngle + Random.Range (-angleVariance / 2f, angleVariance / 2f);
				}
				if (i > 0) {
					anglesDiff[i - 1] = (i==pointyPoints?Mathf.PI * 2 + angles[0]:angles[i]) - angles[i - 1];
				}
			}
			for (int i = 0; i < pointyPoints; i++) {
				nodeX = Mathf.Cos (angles[i] + angleCorrection) * radius;
				nodeY = Mathf.Sin (angles[i] + angleCorrection) * radius;
				BezierNode node = new BezierNode (new Vector2 (nodeX, nodeY));
				if (i == 0) {
					lastNode = node;
				}
				float tan = (4/3) * Mathf.Tan(Mathf.PI/ (float)(2 * pointyPoints));
				//node.handle1 = new Vector3 (nodeY, -nodeX) * tan * 0.5f;
				node.handle1 = new Vector3 (nodeY, -nodeX) * tan;
				node.handle2 = -node.handle1;
				node.handleStyle = BezierNode.HandleStyle.Auto;
				curve.AddNode (node);
			}
			curve.AddNode (lastNode);
			return curve;
		}
		/// <summary>
		/// Add scaled displacement for even nodes fron the center of the circle.
		/// </summary>
		/// <param name="bezierCurve">Circular bezier curve.</param>
		/// <param name="minScale">Minimum scale to apply with the displacement.</param>
		/// <param name="maxScale">Maximum scale to apply with the displacement.</param>
		public void AddDisplacement (BezierCurve bezierCurve, float minScale, float maxScale) {
			bool applyDisplacement = false;
			float scale = 1f;
			for (int i = 0; i < bezierCurve.nodeCount; i++) {
				if (applyDisplacement) {
					scale = Random.Range (minScale, maxScale);
					bezierCurve.nodes [i].position *= scale;
					bezierCurve.nodes [i].handle1 *= scale;
					bezierCurve.nodes [i].handle2 *= scale;
				}
				applyDisplacement = !applyDisplacement;
			}
		}
		/// <summary>
		/// Return a branch info registered for a given branch.
		/// </summary>
		/// <param name="branch">Branch using the branch info object.</param>
		/// <returns>Branch info object or null if not found.</returns>
		public BranchInfo GetBranchInfo (int branchId) {
			BranchInfo branchInfo = null;
			if (branchInfos.ContainsKey (branchId)) {
				return branchInfos [branchId];
			}
			return branchInfo;
		}
		#endregion
	}
}