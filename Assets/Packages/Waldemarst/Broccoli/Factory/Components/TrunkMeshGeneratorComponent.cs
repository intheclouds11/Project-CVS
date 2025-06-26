using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

using Broccoli.Factory;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.Pipe;
using Broccoli.Model;

namespace Broccoli.Component
{
	/// <summary>
	/// Trunk mesh generator component.
	/// </summary>
	public class TrunkMeshGeneratorComponent : TreeFactoryComponent {
		#region Vars
		TrunkMeshGeneratorElement trunkMeshGeneratorElement;
		NativeArray<Vector3> m_Vertices;
		NativeArray<Vector3> m_Normals;

		Vector3[] m_ModifiedVertices;
		Vector3[] m_ModifiedNormals;
		#endregion

		#region Job
		struct TrunkJob : IJobParallelFor {
			public NativeArray<Vector3> vertices;
			public NativeArray<Vector3> normals;
			/// <summary>
			/// UV information of the mesh.
			/// x: mapping U component.
			/// y: mapping V component.
			/// z: radial position.
			/// w: girth.
			/// </summary>
			public NativeArray<Vector4> uvs;

			/// <summary>
			/// UV2 information of the mesh.
			/// x: accumulated length.
			/// y: phase.
			/// z: phase position.
			/// w: is root = 1.
			/// </summary>
			public NativeArray<Vector4> uv2s;

			/// <summary>
			/// UV5 information of the mesh.
			/// x: id of the branch.
			/// y: id of the branch skin.
			/// z: id of the struct.
			/// w: tuned
			/// </summary>
			public NativeArray<Vector4> uv5s;

			/// <summary>
			/// UV6 information of the mesh.
			/// x, y, z: center.
			/// w: unallocated.
			/// </summary>
			public NativeArray<Vector4> uv6s;

			/// <summary>
			/// UV7 information of the mesh.
			/// x, y, z: direction.
			/// w: unallocated.
			/// </summary>
			public NativeArray<Vector4> uv7s;

			/*
			/// <summary>
			/// UV5 information of the mesh.
			/// x: radial position.
			/// y: global length position.
			/// z: girth.
			/// w: unallocated.
			/// </summary>
			public NativeArray<Vector4> uv5s;
			/// <summary>
			/// UV6 information of the mesh.
			/// x: id of the branch.
			/// y: id of the branch skin.
			/// z: id of the struct.
			/// w: tuned.
			/// </summary>
			public NativeArray<Vector4> uv6s;
			/// <summary>
			/// UV7 information of the mesh.
			/// x, y, z: center.
			/// w: unallocated.
			/// </summary>
			public NativeArray<Vector4> uv7s;
			/// <summary>
			/// UV8 information of the mesh.
			/// x, y, z: direction.
			/// w: unallocated.
			/// </summary>
			public NativeArray<Vector4> uv8s;
			*/

			public int branchSkinId;
			public float maxLength;
			public float minLength;
			public float scaleAtBase;
			[NativeDisableParallelForRestriction]
			public NativeArray<float> baseRadialPositions;
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector2> basePositions;
			[NativeDisableParallelForRestriction]
			public NativeArray<float> scalePoints;
			[NativeDisableParallelForRestriction]
			public NativeArray<float> scalePointVals;
			public float sinTime;
			public float cosTime;
			public float strength;
			public float branchRoll;
			public float twirl;

			public void Execute(int i) {
				/*
				if (uv6s[i].y == branchSkinId && 
					uv5s[i].y + 0.01f >= minLength && uv5s[i].y - 0.01f <= maxLength) {
					float absPos = 1f - ((uv5s[i].y - minLength) / (maxLength - minLength));
					float pos = EvalPos (absPos);
					Vector3 branchNormal = Quaternion.AngleAxis (branchRoll * Mathf.Rad2Deg, uv8s[i]) * Vector3.forward;
					Vector2 radialPos = GetRadialPoint (uv5s[i].x);
					Vector3 newVertex = Vector3.Lerp (vertices[i], (Vector3)uv7s[i] + (Quaternion.LookRotation (uv8s[i], branchNormal) * radialPos), pos);
					Quaternion axisRotation = Quaternion.AngleAxis (twirl * Mathf.Rad2Deg * absPos, uv8s[i]);
					vertices[i] = axisRotation * (newVertex - (Vector3)uv7s[i]) + (Vector3)uv7s[i];
					normals[i] = axisRotation * normals[i];
				}
				*/
				if (uv5s[i].y == branchSkinId && 
					uv2s[i].x + 0.01f >= minLength && uv2s[i].x - 0.01f <= maxLength) {
					float absPos = 1f - ((uv2s[i].x - minLength) / (maxLength - minLength));
					float pos = EvalPos (absPos);
					Vector3 branchNormal = Quaternion.AngleAxis (branchRoll * Mathf.Rad2Deg, uv7s[i]) * Vector3.forward;
					Vector2 radialPos = GetRadialPoint (uvs[i].z);
					Vector3 newVertex;
					if ((Vector3)uv7s[i] != Vector3.zero) {
						newVertex = Vector3.Lerp (vertices[i], (Vector3)uv6s[i] + (Quaternion.LookRotation (uv7s[i], branchNormal) * radialPos), pos);
					} else {
						newVertex = Vector3.Lerp (vertices[i], (Vector3)uv6s[i], pos);
					}
					Quaternion axisRotation = Quaternion.AngleAxis (twirl * Mathf.Rad2Deg * absPos, uv7s[i]);
					vertices[i] = axisRotation * (newVertex - (Vector3)uv6s[i]) + (Vector3)uv6s[i];
					normals[i] = axisRotation * normals[i];
				}
			}
			public Vector2 GetRadialPoint (float radialPosition) {
				if (radialPosition > 0 && radialPosition < 1) {
					int i;
					for (i = 0; i < baseRadialPositions.Length; i++) {
						if (radialPosition < baseRadialPositions [i]) {
							break;
						}
					}
					return basePositions [i];
				} else if (radialPosition == 1) {
					return basePositions [baseRadialPositions.Length - 1];
				} else {
					return basePositions [0];
				}
			}
			public float EvalPos (float pos) {
				for (int i = 0; i < scalePoints.Length; i++) {
					if (pos <= scalePoints [i]) return scalePointVals [i];
				}
				return pos;
			}
		}
		#endregion

		#region Configuration
		/// <summary>
		/// Prepares the parameters to process with this component.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="useCache">If set to <c>true</c> use cache.</param>
		/// <param name="useLocalCache">If set to <c>true</c> use local cache.</param>
		/// <param name="processControl">Process control.</param>
		protected override void PrepareParams (TreeFactory treeFactory,
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl processControl = null) 
		{
			base.PrepareParams (treeFactory, useCache, useLocalCache, processControl);
		}
		/// <summary>
		/// Gets the changed aspects on the tree for this component.
		/// </summary>
		/// <returns>The changed aspects.</returns>
		public override int GetChangedAspects () {
			return (int)TreeFactoryProcessControl.ChangedAspect.Mesh;
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public override void Clear ()
		{
			base.Clear ();
		}
		#endregion

		#region Processing
		/// <summary>
		/// Process the tree according to the pipeline element.
		/// </summary>
		/// <param name="treeFactory">Parent tree factory.</param>
		/// <param name="useCache">If set to <c>true</c> use cache.</param>
		/// <param name="useLocalCache">If set to <c>true</c> use local cache.</param>
		/// <param name="processControl">Process control.</param>
		public override bool Process (TreeFactory treeFactory, 
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl processControl = null) {
			if (pipelineElement != null && treeFactory != null) {
				// Get the trunk element.
				trunkMeshGeneratorElement = pipelineElement as TrunkMeshGeneratorElement;
				// Prepare the parameters.
				PrepareParams (treeFactory, useCache, useLocalCache, processControl);
				return true;
			}
			return false;
		}
		#endregion

		#region Builders
		public void RegisterCustomTrunkMesh (
			CustomTrunkMeshBuilder customTrunkMeshBuilder,
			BroccoTree.Branch firstBranch,
			BranchMeshBuilder.BranchSkin branchSkin,
			BranchMeshBuilder.BranchSkinRange branchSkinRange,
			TrunkMeshGeneratorElement trunkMeshGeneratorElement) 
		{
			float trunkLength = (branchSkinRange.to - branchSkinRange.from) * branchSkin.length;
			float trunkScale = UnityEngine.Random.Range (trunkMeshGeneratorElement.minTrunkScaleAtBase, trunkMeshGeneratorElement.maxTrunkScaleAtBase);
			float trunkGirthAtBase = branchSkin.GetGirthAtPosition (0f, firstBranch) * trunkScale;
			float trunkGirthAtTop = branchSkin.GetGirthAtPosition (branchSkinRange.to, firstBranch);
			
			TrunkBuilder trunkBuilder = 
				new TrunkBuilder (
					trunkLength,
					trunkGirthAtBase,
					trunkGirthAtTop,
					trunkMeshGeneratorElement.scaleCurve,
					1f);
					
			if (trunkMeshGeneratorElement.integrationMode == TrunkMeshGeneratorElement.IntegrationMode.SimulateRoots) {
				int rootsCount = Random.Range (trunkMeshGeneratorElement.minRootsCount, trunkMeshGeneratorElement.maxRootsCount + 1);
				float angleVarianceAtBase = Random.Range (trunkMeshGeneratorElement.minAngleVariance, trunkMeshGeneratorElement.maxAngleVariance);
				float angleVarianceAtTop = Random.Range (trunkMeshGeneratorElement.minAngleVariance, trunkMeshGeneratorElement.maxAngleVariance);
				trunkBuilder.InitSimulateRootsMode (
					firstBranch, 
					rootsCount, 
					angleVarianceAtBase,
					angleVarianceAtTop,
					new Vector2 (trunkMeshGeneratorElement.minRootScaleAtBase, trunkMeshGeneratorElement.maxRootScaleAtBase),
					new Vector2 (trunkMeshGeneratorElement.minRootScaleAtTop, trunkMeshGeneratorElement.maxRootScaleAtTop),
					new Vector2 (trunkMeshGeneratorElement.minRootExposureAtBase, trunkMeshGeneratorElement.maxRootExposureAtBase),
					new Vector2 (trunkMeshGeneratorElement.minRootExposureAtTop, trunkMeshGeneratorElement.maxRootExposureAtTop),
					new Vector2 (trunkMeshGeneratorElement.minRootReach, trunkMeshGeneratorElement.maxRootReach),
					Random.Range (trunkMeshGeneratorElement.minTwirl, trunkMeshGeneratorElement.maxTwirl));
			} else {
				float angleVarianceAtTop = Random.Range (trunkMeshGeneratorElement.minAngleVariance, trunkMeshGeneratorElement.maxAngleVariance);
				trunkBuilder.InitRootsMode (
					firstBranch,
					angleVarianceAtTop,
					new Vector2 (trunkMeshGeneratorElement.minRootScaleAtBase, trunkMeshGeneratorElement.maxRootScaleAtBase),
					new Vector2 (trunkMeshGeneratorElement.minRootScaleAtTop, trunkMeshGeneratorElement.maxRootScaleAtTop),
					new Vector2 (trunkMeshGeneratorElement.minRootExposureAtBase, trunkMeshGeneratorElement.maxRootExposureAtBase),
					new Vector2 (trunkMeshGeneratorElement.minRootExposureAtTop, trunkMeshGeneratorElement.maxRootExposureAtTop),
					new Vector2 (trunkMeshGeneratorElement.minRootReach, trunkMeshGeneratorElement.maxRootReach),
					Random.Range (trunkMeshGeneratorElement.minTwirl, trunkMeshGeneratorElement.maxTwirl));
			}

			int numStepsAtBase = customTrunkMeshBuilder.maxPolygonSides;
			int numStepsAtTop = customTrunkMeshBuilder.minPolygonSides;

			// Add the Segments
			float bsrLength = branchSkinRange.to - branchSkinRange.from;
			branchSkinRange.subdivisions = 
				(int)Mathf.Lerp (24f * trunkMeshGeneratorElement.lengthResolutionFactor * bsrLength, 
					12f * trunkMeshGeneratorElement.lengthResolutionFactor * bsrLength, Mathf.InverseLerp (5, 45, customTrunkMeshBuilder.branchAngleToleranceAtBase));
			branchSkinRange.radialResolutionFactor = trunkMeshGeneratorElement.radialResolutionFactor;

			float step = 1f / (float)branchSkinRange.subdivisions;
			for (int i = 0; i <= branchSkinRange.subdivisions; i++) {
				List<float> radialPos = new List<float>();
				List<Vector3> points = new List<Vector3>();
				List<Vector3> normals = new List<Vector3>();
				int lerpSteps = Mathf.RoundToInt (Mathf.Lerp (numStepsAtBase, numStepsAtTop, i * step));
				//trunkBuilder.GetSegmentPlain (firstBranch, branchSkin, i * step * trunkLength, lerpSteps, ref points, ref normals, ref radialPos);
				trunkBuilder.GetSegment (i * step * trunkLength, lerpSteps, ref points, ref normals, ref radialPos);
				customTrunkMeshBuilder.AddCustomSegmentToRange (branchSkin, branchSkinRange, 0, i * step, radialPos, points, normals);
			}
		}
		#endregion
	}
}