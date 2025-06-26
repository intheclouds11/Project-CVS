using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Model;

namespace Broccoli.Pipe {
    /// <summary>
    /// Composite variation container class.
    /// </summary>
    [System.Serializable]
    public class VariationDescriptor {
        #region Variation Group Cluster
        [System.Serializable]
        public class VariationUnit {
            public float random = 0f;
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 orientation = Vector3.right;
            public Vector3 anchorPosition = Vector3.zero;
            public bool flip = false;
            public float scale = 1f;
            public float fBending = 0f;
            public float sBending = 0f;
            public float fBendingScale = 1f;
            public float sBendingScale = 1f;
            public Vector3 offset = Vector3.zero;
            public int snapshotIndex = -1;
            public int snapshotId = -1;
            public int snapshotLods = 0;
            public BezierCurve curve = null;
        }
        [System.Serializable]
        public class VariationGroupCluster {
            #region Vars
            public int groupId = 0;
            public float radius = 0f;
            public float centerFactor = 0f;
            public List<VariationUnit> variationUnits = new List<VariationUnit> ();
            #endregion
        }
        [System.Serializable]
        /// <summary>
        /// Class to save data to construct a mesh for a Variation at a given LOD.
        /// </summary>
        public class VariationMesh {
            #region Vars
            /// <summary>
            /// Composite identifier.
            /// </summary>
            [SerializeField]
            private int _id = 0;
            /// <summary>
            /// Variation id.
            /// </summary>
            [SerializeField]
            private int _variationId = 0;
            /// <summary>
            /// LOD for the mesh.
            /// </summary>
            [SerializeField]
            private int _lod = 0;
            public int id {
                get { return _id; }
            }
            public int variationId {
                get { return _variationId; }
            }
            public int lod {
                get { return _lod; }
            }
            /// <summary>
            /// Vertices on the variation. Filled only when exporting the variation mesh.
            /// </summary>
            public List<Vector3> vertices = new List<Vector3> ();
            /// <summary>
            /// Normals on the variation. Filled only when exporting the variation mesh.
            /// </summary>
            public List<Vector3> normals = new List<Vector3> ();
            /// <summary>
            /// Tangents on the variation. Filled only when exporting the variation mesh.
            /// </summary>
            public List<Vector4> tangents = new List<Vector4> ();
            /// <summary>
            /// Triangles on the variation. Filled only when exporting the variation mesh.
            /// </summary>
            public List<int> tris = new List<int> ();
            /// <summary>
            /// UV0s on the variation for UV mapping. Filled only when exporting the variation mesh.
            /// </summary>
            public List<Vector2> uv0s = new List<Vector2> ();
            /// <summary>
            /// UV1s on the variation for unwrapped UV mapping. Filled only when exporting the variation mesh.
            /// </summary>
            public List<Vector2> uv1s = new List<Vector2> ();
            #endregion

            #region Methods
            /// <summary>
            /// Sets the composite id for this variation mesh.
            /// </summary>
            /// <param name="variationId">Variation id.</param>
            /// <param name="lod">LOD for the mesh.</param>
            public void SetId (int variationId, int lod) {
                _variationId = variationId;
                _lod = lod;
                _id = variationId * 100000 + lod * 10000;
            }
            #endregion

            #region Clone
            /// <summary>
            /// Clone this instance.
            /// </summary>
            public VariationMesh Clone () {
                VariationMesh clone = new VariationMesh ();
                clone.SetId (variationId, lod);
                clone.vertices = new List<Vector3> (vertices);
                clone.normals = new List<Vector3> (normals);
                clone.tangents = new List<Vector4> (tangents);
                clone.tris = new List<int> (tris);
                clone.uv0s = new List<Vector2> (uv0s);
                clone.uv1s = new List<Vector2> (uv1s);
                return clone;
            }
            #endregion
        }
        #endregion

        #region Structure Vars
        public int id = 0;
        public int seed = 0;
        [System.NonSerialized]
        public Dictionary<int,VariationGroupCluster> variationGroupClusters = new Dictionary<int,VariationGroupCluster> ();
        public List<VariationGroup> variationGroups = new List<VariationGroup> ();
        /// <summary>
        /// Id to VariationGroup instance.
        /// </summary>
        [System.NonSerialized]
        public Dictionary<int, VariationGroup> idToVariationGroup = new Dictionary<int, VariationGroup> ();
        /// <summary>
        /// List of snapshot ids used by this variation.
        /// </summary>
        public List<int> snapshotIds = new List<int> ();
        /// <summary>
        /// List of snapshot ids and lods used by this variation.
        /// </summary>
        [System.NonSerialized]
        public List<(int, int)> snapshotIdsLods = new List<(int, int)> ();
        /// <summary>
        /// List of texture hashes per submesh.
        /// </summary>
        /// <typeparam name="string">Hash of texture.</typeparam>
        /// <returns>List of hashes, with index as submesh.</returns>
        [System.NonSerialized]
        public List<Hash128> hashes = new List<Hash128> ();
        /// <summary>
        /// Canvas offset.
        /// </summary>
        public Vector2 canvasOffset = Vector2.zero;
        /// <summary>
        /// Mesh data for the LODs on this Variation Descriptor. Only saved if 
        /// </summary>
        public List<VariationMesh> variationMeshes = new List<VariationMesh> ();
        public int lodCount {
            get { return variationMeshes.Count; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Variation Descriptor constructor.
        /// </summary>
        public VariationDescriptor () {}
        #endregion

        #region Clone
        /// <summary>
        /// Clone this instance.
        /// </summary>
        public VariationDescriptor Clone () {
            VariationDescriptor clone = new VariationDescriptor ();
            clone.id = id;
            clone.seed = seed;
            // Clone Groups.
            for (int i = 0; i < variationGroups.Count; i++) {
                clone.variationGroups.Add (variationGroups [i].Clone ());
            }
            // Clone meshes.
            for (int i = 0; i < variationMeshes.Count; i++) {
                clone.variationMeshes.Add (variationMeshes [i].Clone ());
            }
            clone.canvasOffset = canvasOffset;
            return clone;
        }
        #endregion

        #region Groups Management
        public void BuildGroupTree () {
            // Populate dictionary.
            idToVariationGroup.Clear ();
            for (int i = 0; i < variationGroups.Count; i++) {
                if (variationGroups [i] != null) {
                    if (!idToVariationGroup.ContainsKey (variationGroups [i].id)) {
                        idToVariationGroup.Add (variationGroups [i].id, variationGroups [i]);
                    }
                }
            }
            // Build groups hierarchy.
            for (int i = 0; i < variationGroups.Count; i++) {
                if (variationGroups [i].parentId >= 0 && idToVariationGroup.ContainsKey (variationGroups [i].parentId)) {
                    variationGroups [i].parent = idToVariationGroup [variationGroups [i].parentId];
                } else {
                    variationGroups [i].parent = null;
                }
            }
        }
        /// <summary>
        /// Adds a Variation Group to this Variation Descriptor.
        /// </summary>
        /// <param name="groupToAdd"></param>
        public void AddGroup (VariationGroup groupToAdd) {
            groupToAdd.id = GetGroupId ();
            variationGroups.Add (groupToAdd);
            idToVariationGroup.Add (groupToAdd.id, groupToAdd);
            if (groupToAdd.parentId >= 0 && idToVariationGroup.ContainsKey (groupToAdd.parentId)) {
                groupToAdd.parent = idToVariationGroup [groupToAdd.parentId];
            } else {
                groupToAdd.parent = null;
            }
        }
        public bool RemoveGroup (int groupId) {
            if (idToVariationGroup.ContainsKey (groupId)) {
                VariationGroup groupToRemove = idToVariationGroup [groupId];
                if (variationGroups.Contains (groupToRemove)) {
                    variationGroups.Remove (groupToRemove);
                }
                groupToRemove.parent = null;
                idToVariationGroup.Remove (groupId);
                return true;
            }
            return false;
        }
        int GetGroupId () {
            int id = 0;
            for (int i = 0; i < variationGroups.Count; i++) {
                if (variationGroups [i] != null) {
                    if (variationGroups [i].id >= id) {
                        id = variationGroups [i].id + 1;
                    }
                }
            }
            return id;
        }
        #endregion
    }
}