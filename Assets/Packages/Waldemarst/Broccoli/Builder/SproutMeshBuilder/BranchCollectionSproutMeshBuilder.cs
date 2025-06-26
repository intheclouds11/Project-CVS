using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Pipe;
using Broccoli.Manager;

namespace Broccoli.Builder
{
    public class BranchCollectionSproutMeshBuilder : BaseSproutMeshBuilder
    {
        #region Vars
		public BranchDescriptorCollection branchDescriptorCollection = null;
        int snapshotIndex = 0;
        int variationIndex = 0;
        int lod = 0;
        public Vector3 meshScale = Vector3.one;
        public Vector3 meshPivot = Vector3.zero;
        public Quaternion meshOrientation = Quaternion.identity;
        private static int _id = 0;
        public Dictionary<Hash128, Mesh> _meshes = new Dictionary<Hash128, Mesh> ();
        #endregion

        #region Abstract
        public override void SetParams (string jsonParams){
            throw new System.NotImplementedException();
        }
        public override Mesh GetMesh () {
            Hash128 hash = new Hash128 ();
            if (branchDescriptorCollection.descriptorImplId == BranchDescriptorCollection.VARIATION_COLLECTION) {
                hash = GetMeshHash (branchDescriptorCollection, variationIndex, lod, meshScale, meshPivot, meshOrientation);
            } else if (branchDescriptorCollection.descriptorImplId == BranchDescriptorCollection.SNAPSHOT_COLLECTION ||
                branchDescriptorCollection.descriptorImplId == BranchDescriptorCollection.BASE_COLLECTION) {
                hash = GetMeshHash (branchDescriptorCollection, snapshotIndex, lod, meshScale, meshPivot, meshOrientation);
            }
            
            if (_meshes.ContainsKey (hash)) {
                return _meshes [hash];
            } else {
                Mesh mesh = null;
                if (branchDescriptorCollection.descriptorImplId == BranchDescriptorCollection.VARIATION_COLLECTION) {
                    mesh = GetVariationMesh (branchDescriptorCollection, variationIndex, lod, meshScale, meshPivot, meshOrientation);
                } else if (branchDescriptorCollection.descriptorImplId == BranchDescriptorCollection.SNAPSHOT_COLLECTION ||
                    branchDescriptorCollection.descriptorImplId == BranchDescriptorCollection.BASE_COLLECTION) {
                    mesh = GetSnapshotMesh (branchDescriptorCollection, snapshotIndex, lod, meshScale, meshPivot, meshOrientation);
                }
                if (mesh != null) {
                    _meshes.Add (hash, mesh);
                }
                return mesh;
            }
        }
        public override void Clear () {
            _meshes.Clear ();
        }
        #endregion

        #region Mesh Processing
        /// <summary>
        /// Prepares this builder to begin building meshes.
        /// </summary>
        public static void PrepareMeshBuilding () {
            SproutCompositeManager.Current ().Clear ();
        }
        /// <summary>
        /// Gets a Mesh for a Snapshot belonging to a BranchCollection.
        /// </summary>
        /// <param name="branchDescriptorCollection">Branch Collection instance.</param>
        /// <param name="index">Index of the snapshot to get the mesh from.</param>
        /// <param name="lod">LOD level for the mesh (from 0 to 2).</param>
        /// <param name="meshScale">Scale to apply to the final mesh.</param>
        /// <param name="meshPivot">Pivot to move the mesh to.</param>
        /// <param name="meshOrientation">Rotation to apply to the mesh before moving to the pivot point.</param>
        /// <returns>Mesh for a Snapshot.</returns>
        public static Mesh GetSnapshotMesh (
            BranchDescriptorCollection branchDescriptorCollection,
            int index,
            int lod,
            Vector3 meshScale,
            Vector3 meshPivot,
            Quaternion meshOrientation,
            bool skipNormalization = false)
        {
            //Validation,
            if (branchDescriptorCollection == null) return null;

            BranchDescriptor snapshotDescriptor = branchDescriptorCollection.snapshots [index];
            SproutCompositeManager sproutCompositeManager = SproutCompositeManager.Current ();
            
            for (int j = 0; j < snapshotDescriptor.polygonAreas.Count; j++) {
                PolygonAreaBuilder.SetPolygonAreaMesh (snapshotDescriptor.polygonAreas [j]);
                sproutCompositeManager.ManagePolygonArea (snapshotDescriptor.polygonAreas [j], snapshotDescriptor);
            }

            Mesh collectionMesh;
            if (skipNormalization) {
                collectionMesh = sproutCompositeManager.GetSnapshotMesh (snapshotDescriptor.id, lod, 1, false);
            } else {
                bool shouldNormalize = !sproutCompositeManager.HasSnapshotMesh (snapshotDescriptor.id, lod);
                collectionMesh = sproutCompositeManager.GetSnapshotMesh (snapshotDescriptor.id, lod, 1);
                if (shouldNormalize)
                    NormalizeBranchCollectionTransform (collectionMesh, 1f, Quaternion.Euler (0, 90, 90));
            }

            return collectionMesh;
        }
        /// <summary>
        /// Gets a Mesh for a Variation belonging to a BranchCollection.
        /// </summary>
        /// <param name="branchDescriptorCollection">Branch Collection instance.</param>
        /// <param name="variationIndex">Index of the variatio to get the mesh from.</param>
        /// <param name="lod">LOD level for the mesh (from 0 to 2).</param>
        /// <param name="meshScale">Scale to apply to the final mesh.</param>
        /// <param name="meshPivot">Pivot to move the mesh to.</param>
        /// <param name="meshOrientation">Rotation to apply to the mesh before moving to the pivot point.</param>
        /// <returns>Mesh for a Variation.</returns>
        public static Mesh GetVariationMesh (
            BranchDescriptorCollection branchDescriptorCollection,
            int variationIndex,
            int lod,
            Vector3 meshScale,
            Vector3 meshPivot,
            Quaternion meshOrientation,
            bool skipNormalization = false)
        {
            //Validation,
            if (branchDescriptorCollection == null) return null;

            VariationDescriptor variationDescriptor = branchDescriptorCollection.variations [variationIndex];
            SproutCompositeManager sproutCompositeManager = SproutCompositeManager.Current ();

            Mesh collectionMesh;
            if (skipNormalization) {
                collectionMesh = sproutCompositeManager.GetVariationMesh (variationDescriptor, lod, 0, false);
            } else {
                bool shouldNormalize = !sproutCompositeManager.HasVariationMesh (variationDescriptor.id, lod);
                collectionMesh = sproutCompositeManager.GetVariationMesh (variationDescriptor, lod);
                if (shouldNormalize)
                    NormalizeBranchCollectionTransform (collectionMesh, 1f, Quaternion.Euler (0, 90, 90));
            }

            return collectionMesh;
        }
        public static void SetIdData (int id) {
            _id = id;
        }
        /// <summary>
        /// Get the hash128 for a mesh given its parameters.
        /// </summary>
        /// <param name="branchDescriptorCollection">Branch Collection instance.</param>
        /// <param name="index">Index of the snapshot or variation to get the mesh from.</param>
        /// <param name="lod">LOD level for the mesh (from 0 to 2).</param>
        /// <param name="scale">Scale to apply to the final mesh.</param>
        /// <param name="pivot">Pivot to move the mesh to.</param>
        /// <param name="orientation">Rotation to apply to the mesh before moving to the pivot point.</param>
        /// <returns>Hash for the mesh.</returns>
        private Hash128 GetMeshHash (BranchDescriptorCollection branchDescriptorCollection, 
            int index, int lod, Vector3 scale, Vector3 pivot, Quaternion orientation)
        {
            int implId = branchDescriptorCollection.descriptorImplId;
            string paramsStr = string.Format ("collection_{0}_{1}_{2}_{3}_{4}_{5}_{6}",
                implId, branchDescriptorCollection.ToString (), index, lod, scale.ToString (), meshPivot.ToString (), orientation.ToString ());
            return Hash128.Compute (paramsStr);
        }
        /// <summary>
		/// Applies scale and rotation to meshes coming from SproutLab's branch descriptor collection.
		/// </summary>
		/// <param name="mesh">Mesh to appy the transformation.</param>
		/// <param name="scale">Scale transformation.</param>
		/// <param name="rotation">Rotation transformation.</param>
		private static void NormalizeBranchCollectionTransform (Mesh mesh, float scale, Quaternion rotation) {
			Vector3[] _vertices = mesh.vertices;
			Vector3[] _normals = mesh.normals;
			Vector4[] _tangents = mesh.tangents;
            Vector4[] _uv2s = new Vector4[mesh.vertexCount];
            Vector4[] _uv3s = new Vector4[mesh.vertexCount];
            float maxLength = 0f;
            float maxSide = 0f;
			for (int i = 0; i < _vertices.Length; i++) {
				_vertices [i] = rotation * _vertices [i] * scale;
				_normals [i] = rotation * _normals [i];
				_tangents [i] = rotation * _tangents [i];
                if (Mathf.Abs (_vertices [i].z) > maxLength) {
                    maxLength = Mathf.Abs (_vertices [i].z);
                }
                if (Mathf.Abs (_vertices [i].x) > maxSide) {
                    maxSide = Mathf.Abs (_vertices [i].x);
                }
                _uv2s [i] = new Vector4 (_vertices [i].z, _vertices [i].x, 0f, _id);
                _uv3s [i] = _vertices [i].normalized;
			}
            for (int i = 0; i < _uv2s.Length; i++) {
                _uv2s [i].x = Mathf.Abs (_uv2s [i].x) / maxLength;
                _uv2s [i].y = Mathf.Abs (_uv2s [i].y) / maxSide;
            }
			mesh.vertices = _vertices;
			mesh.normals = _normals;
			mesh.tangents = _tangents;
            mesh.SetUVs (1, _uv2s);
            mesh.SetUVs (2, _uv3s);
			mesh.RecalculateBounds ();
		}
        #endregion
    }
}
