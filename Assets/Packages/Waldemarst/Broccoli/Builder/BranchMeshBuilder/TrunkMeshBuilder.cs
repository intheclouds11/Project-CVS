using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Broccoli.Base;
using Broccoli.Model;

namespace Broccoli.Builder {
	/// <summary>
	/// Gives methods to help create mesh segments using BranchSkin instances.
	/// </summary>
	public class TrunkMeshBuilder : IBranchMeshBuilder {
		#region Class BranchInfo
		/// <summary>
		/// Class containing the information to process the mesh of a BranchSkinRange.
		/// For a given BranchSkin, the number of BranchSkinRangeInfo instance must match
		/// the number or BranchSkinRange instances.
		/// </summary>
		public class BranchSkinRangeInfo {
			/// <summary>
			/// Segment positions relative to the total BranchSkin length (0 to 1).
			/// </summary>
			public List<float> segPos = new List<float> ();
			/// <summary>
			/// Radial positions for each BranchSkin segment.
			/// </summary>
			public List<List<float>> radialPos = new List<List<float>> ();
			/// <summary>
			/// Points for each BranchSkin segment.
			/// </summary>
			public List<List<Vector3>> points = new List<List<Vector3>> ();
			/// <summary>
			/// Normals for each BranchSkin segment.
			/// </summary>
			public List<List<Vector3>> normals = new List<List<Vector3>> ();
		}
		#endregion

		#region Vars
		/// <summary>
		/// Saves all the BranchSkinRangeInfo instances. The compund id containts the
		/// id of the Branch/BranchSkin and the index of the BranchSkinRange.
		/// </summary>
		public Dictionary<(int, int), BranchSkinRangeInfo> branchSkinRangeInfos = 
			new Dictionary<(int, int), BranchSkinRangeInfo> ();
		/// <summary>
		/// Saves for each BranchSkin how many BranchSkinRangeInfo have been registered.
		/// </summary>
		public Dictionary<int, int> branchSkinRangeInfosCount = new Dictionary<int, int> ();
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
		/// <summary>
		/// Angle tolerance to create new points on a range.
		/// </summary>
		public float branchAngleToleranceAtBase = 24f;
		#endregion

		#region Interface
		public virtual void SetAngleTolerance (float angleTolerance) {}
		public virtual float GetAngleTolerance () {
			// Dummy value, it has no effect on this kind of MeshBuilder.
			return 200;
		}
		public virtual void SetGlobalScale (float globalScale) { this.globalScale = globalScale; }
		public virtual float GetGlobalScale () { return this.globalScale; }
		/// <summary>
		/// Get the branch mesh builder type.
		/// </summary>
		/// <returns>Branch mesh builder type.</returns>
		public virtual BranchMeshBuilder.BuilderType GetBuilderType () {
			return BranchMeshBuilder.BuilderType.Custom;
		}
		/// <summary>
		/// Called right after a BranchSkin is created. Registers CustomShapers to all the branches belonging to the 
		/// BranchSkin. If a range encloses two or more branches on the BranchSkin, segments at the transition position
		/// between branches MUST BE REGISTERED.
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
			bool result = true;
			if (!firstBranch.hasShaper) {
				// Assigns a custom shaper to all the branches on the BranchSkin.
				// Check if this BranchSkin has custom segments.
				if (branchSkinRangeInfosCount.ContainsKey (branchSkin.id)) {
					int customRangesCount = branchSkinRangeInfosCount[branchSkin.id];
					BranchSkinRangeInfo bsrInfo;
					BroccoTree.Branch branch;
					CustomBranchShaper cbShaper = null;
					for (int i = 0; i < customRangesCount; i++) {
						if (branchSkinRangeInfos.ContainsKey ((branchSkin.id, i))) {
							// Iterate through all the segments on all the ranged custom segments.
							bsrInfo = branchSkinRangeInfos[(branchSkin.id, i)];
							// The branch containing the segment will be selected.
							int currBranchId = -1;
							// The position in the branch for the segment will also be selected.
							float branchPos;
							CustomBranchShaper prevCbShaper = null;
							for (int bsrInfoIndex = 0; bsrInfoIndex < bsrInfo.segPos.Count; bsrInfoIndex++) {
								// Add relevant position to BranchSkin.
								branchSkin.AddRelevantPosition (bsrInfo.segPos [bsrInfoIndex], 0.01f);
								// Translate to position in branch, get branch.
								branchPos = branchSkin.TranslateToPositionAtBranch 
									(bsrInfo.segPos [bsrInfoIndex], firstBranch, out branch);
								// If a new branch has been detected
								if (branch.id != currBranchId) {
									currBranchId = branch.id;
									prevCbShaper = cbShaper;
									// Create a shaper for it.
									if (branch.shaper == null) {
										cbShaper = new CustomBranchShaper ();
										branch.SetShaper (cbShaper, 0f);
									}
									// or get the existing one. Type must be CustomBranchShaper.
									else {
										cbShaper = (CustomBranchShaper)branch.shaper;
									}
								}
								// Add the segment to the custom shaper. If the segment is near 0, snap it.
								if (branchPos < 0.01f) {
									branchPos = 0f;
								}
								// Snap it if close to 1
								if (branchPos > 1f - 0.01f) {
									branchPos = 1f;
								}
								// Add the segment.
								cbShaper.AddSegment (branchPos, bsrInfo.radialPos[bsrInfoIndex], 
									bsrInfo.points[bsrInfoIndex], bsrInfo.normals[bsrInfoIndex]);
								// If  the position is 0 and there is a previous shaper, add it to the top of it.
								if (branchPos == 0 && prevCbShaper != null) {
									prevCbShaper.AddSegment (1f, bsrInfo.radialPos[bsrInfoIndex], 
										bsrInfo.points[bsrInfoIndex], bsrInfo.normals[bsrInfoIndex]);
								}
							}
						}
					}
					// Iterate though each Branch in the BranchSkin and add Caps if there are no 0 and 1 positions.
					branch = firstBranch;
					CustomBranchShaper prevShaper = null;
					while (branch != null) {
						cbShaper = (CustomBranchShaper)branch.shaper;
						if (cbShaper == null) {
							cbShaper = new CustomBranchShaper ();
							if (prevShaper != null && prevShaper.HasSegment (1f)) {
								cbShaper.AddSegment (0f, prevShaper.GetRadialPos (1f), prevShaper.GetPoints (1f), prevShaper.GetNormals (1f));
							}
							branch.SetShaper (cbShaper, 0f);
						}
						//Add cap at pos 0 and 1 if not registered by the custom segments.
						if (!cbShaper.HasSegment (0f)) {
							cbShaper.AddDefaultSegment (0f);
						}
						if (!cbShaper.HasSegment (1f)) {
							cbShaper.AddDefaultSegment (1f);
						}
						// Must assign a custom shaper.
						
						prevShaper = cbShaper;
						branch = branch.followUp;
					}
				}
			}
			return result;
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

		#region BranchSkinRangeInfo
		public void AddCustomSegmentToRange (
			BranchMeshBuilder.BranchSkin branchSkin,
			BranchMeshBuilder.BranchSkinRange branchSkinRange,
			int branchSkinRangeIndex, 
			float rangePosition,
			List<float> radialPos,
			List<Vector3> segments,
			List<Vector3> normals)
		{
			/// Instance holding the segment information.
			BranchSkinRangeInfo bsrInfo;

			// Get the existing BranchSkingRangeInfo if it exists.
			if (branchSkinRangeInfos.ContainsKey ((branchSkin.id, branchSkinRangeIndex))) {
				bsrInfo = branchSkinRangeInfos [(branchSkin.id, branchSkinRangeIndex)];
			}
			// if not, create it. 
			else {
				bsrInfo = new BranchSkinRangeInfo ();
				branchSkinRangeInfos.Add ((branchSkin.id, branchSkinRangeIndex), bsrInfo);
			}

			// Converts the range position to a BranchSkin position.
			float branchSkinPos = 
				branchSkinRange.from + rangePosition * (branchSkinRange.to - branchSkinRange.from);

			// Fill information on the BranchSkinRangeInfo
			int insertionIndex = bsrInfo.segPos.BinarySearch (branchSkinPos);
			if (insertionIndex < 0) {
				insertionIndex = ~insertionIndex; // Bitwise complement for insertion point
			}
			bsrInfo.segPos.Insert (insertionIndex, branchSkinPos);
			bsrInfo.radialPos.Insert (insertionIndex, radialPos);
			bsrInfo.points.Insert (insertionIndex, segments);
			bsrInfo.normals.Insert (insertionIndex, normals);
			if (!branchSkinRangeInfosCount.ContainsKey (branchSkin.id)) {
				branchSkinRangeInfosCount[branchSkin.id] = 1;
			} else {
				branchSkinRangeInfosCount[branchSkin.id]++;
			}
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
			int numberOfSegments = -1;
			if (branch.hasShaper) {
				if (branch.shaper is CustomBranchShaper) {
					CustomBranchShaper cbShaper = (CustomBranchShaper)branch.shaper;
					numberOfSegments = cbShaper.GetNumberOfSegments (branchPosition);
				}
			}
			if (numberOfSegments < 0) {
				float girthPosition = (branchAvgGirth - branchSkin.minAvgGirth) / (branchSkin.maxAvgGirth - branchSkin.minAvgGirth);
				branchSkin.polygonSides = Mathf.Clamp (
					Mathf.RoundToInt (
						Mathf.Lerp (
							branchSkin.minPolygonSides,
							branchSkin.maxPolygonSides,
							girthPosition)), 
							branchSkin.minPolygonSides,
							branchSkin.maxPolygonSides);
				numberOfSegments = branchSkin.polygonSides;
			}
			return numberOfSegments;
		}
		#endregion
	}
}