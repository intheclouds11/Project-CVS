using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Broccoli.Model
{
    [System.Serializable]
    public class TrunkMeshDescriptor
    {
        #region Vars
        [SerializeField]
        private string _guid;
        public string Guid { get { return _guid; } private set { _guid = value; } }

        public bool enabled = true;

        [SerializeField]
        private GameObject _gameObject;
        public GameObject GameObject {
            get { return _gameObject; }
            set {
                if (_gameObject != value) {
                    _gameObject = value;
                    ExtractMainMeshAndMaterial();
                }
            }
        }

        [SerializeField]
        private Mesh _mesh;
        public Mesh Mesh {
            get { return _mesh; }
            private set { _mesh = value; } // Primarily set via GameObject
        }

        public bool hasLOD = false;

        [SerializeField]
        private GameObject _gameObjectLOD1;
        public GameObject GameObjectLOD1 {
            get { return _gameObjectLOD1; }
            set {
                if (_gameObjectLOD1 != value) {
                    _gameObjectLOD1 = value;
                    ExtractLOD1Mesh();
                }
            }
        }

        [SerializeField]
        private Mesh _meshLOD1;
        public Mesh MeshLOD1 {
            get { return _meshLOD1; }
            private set { _meshLOD1 = value; } // Primarily set via GameObjectLOD1
        }

        [SerializeField]
        private GameObject _gameObjectLOD2;
        public GameObject GameObjectLOD2 {
            get { return _gameObjectLOD2; }
            set {
                if (_gameObjectLOD2 != value) {
                    _gameObjectLOD2 = value;
                    ExtractLOD2Mesh();
                }
            }
        }

        [SerializeField]
        private Mesh _meshLOD2;
        public Mesh MeshLOD2 {
            get { return _meshLOD2; }
            private set { _meshLOD2 = value; } // Primarily set via GameObjectLOD2
        }

        [SerializeField]
        private Material _material;
        public Material Material {
            get { return _material; }
            private set { _material = value; } // Primarily set via GameObject
        }

        /// <summary>
        /// Local position offset to apply to the mesh.
        /// </summary>
        public Vector3 position = Vector3.zero;
        /// <summary>
        /// Local rotation offset to apply to the mesh.
        /// </summary>
        public Quaternion rotation = Quaternion.identity;
        /// <summary>
        /// Local scale to apply to the mesh.
        /// </summary>
        public Vector3 scale = Vector3.one;

        /// <summary>
        /// Local position offset of the mesh, extracted from the prefab hierarchy.
        /// </summary>
        public Vector3 meshPosition = Vector3.zero;
        /// <summary>
        /// Local rotation offset of the mesh, extracted from the prefab hierarchy.
        /// </summary>
        public Quaternion meshRotation = Quaternion.identity;
        /// <summary>
        /// Local scale of the mesh, extracted from the prefab hierarchy.
        /// </summary>
        public Vector3 meshScale = Vector3.one;
        /// <summary>
        /// Combined position.
        /// </summary>
        public Vector3 combinedPosition {
            get { return meshPosition + position; }
        }
        /// <summary>
        /// Combined rotation.
        /// </summary>
        public Quaternion combinedRotation {
            get { return rotation * meshRotation; }
        }
        /// <summary>
        /// Combined scale.
        /// </summary>
        public Vector3 combinedScale {
            get { return Vector3.Scale (meshScale, scale); }
        }

        public bool IsValid {
            get { return Mesh != null && Material != null; }
        }

        public string albedoTexProp = "_MainTex";
        public bool hasNormalTex = false;
        public string normalTexProp = "_BumpMap"; // Unity's default normal map property
        public bool hasExtraTex = false;
        public string extraTexProp = "_ExtraTex"; // Custom, example name

        public List<TrunkDef> trunkDefs = new List<TrunkDef>();
        #endregion

        #region Constructor
        public TrunkMeshDescriptor() {
            Guid = System.Guid.NewGuid().ToString();
        }
        #endregion

        #region Extraction Methods
        public void ExtractMainMeshAndMaterial() {
            // Reset transform offsets before extraction.
            meshPosition = Vector3.zero;
            meshRotation = Quaternion.identity;
            meshScale = Vector3.one;

            if (_gameObject != null) {
                Renderer renderer = null;
                MeshFilter meshFilter = _gameObject.GetComponentInChildren<MeshFilter>();
                
                if (meshFilter != null && meshFilter.sharedMesh != null) {
                    Mesh = meshFilter.sharedMesh;
                    renderer = meshFilter.gameObject.GetComponent<Renderer>();
                } else {
                    SkinnedMeshRenderer skinnedMeshRenderer = _gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null) {
                        Mesh = skinnedMeshRenderer.sharedMesh;
                        renderer = skinnedMeshRenderer;
                    } else {
                        Mesh = null;
                    }
                }

                // Extract Material
                if (renderer != null && renderer.sharedMaterials.Length > 0) {
                    Material = renderer.sharedMaterial; // Gets the first shared material
                } else {
                    Material = null;
                }

                // --- ADDED FOR FBX TRANSFORM ---
                // If a renderer was found and its transform is a child of the assigned GameObject root,
                // store its local transform values. This captures offsets from FBX imports.
                if (renderer != null) {
                    meshPosition = renderer.transform.localPosition;
                    meshRotation = renderer.transform.localRotation;
                    meshScale = renderer.transform.localScale;
                }

            } else {
                Mesh = null;
                Material = null;
            }
        }

        public void ExtractLOD1Mesh() {
            if (_gameObjectLOD1 != null) {
                MeshFilter meshFilter = _gameObjectLOD1.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null) {
                    MeshLOD1 = meshFilter.sharedMesh;
                } else {
                    SkinnedMeshRenderer skinnedMeshRenderer = _gameObjectLOD1.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null) {
                        MeshLOD1 = skinnedMeshRenderer.sharedMesh;
                    } else {
                        MeshLOD1 = null;
                    }
                }
            } else {
                MeshLOD1 = null;
            }
        }

        public void ExtractLOD2Mesh() {
            if (_gameObjectLOD2 != null) {
                MeshFilter meshFilter = _gameObjectLOD2.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null) {
                    MeshLOD2 = meshFilter.sharedMesh;
                } else {
                    SkinnedMeshRenderer skinnedMeshRenderer = _gameObjectLOD2.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null) {
                        MeshLOD2 = skinnedMeshRenderer.sharedMesh;
                    } else {
                        MeshLOD2 = null;
                    }
                }
            } else {
                MeshLOD2 = null;
            }
        }
        #endregion

        #region Clone
        public TrunkMeshDescriptor Clone() {
            TrunkMeshDescriptor clone = new TrunkMeshDescriptor();
            clone.Guid = this.Guid;
            clone.enabled = this.enabled;
            clone._gameObject = this._gameObject;
            clone._mesh = this._mesh;
            clone._material = this._material;

            clone.hasLOD = this.hasLOD;
            clone._gameObjectLOD1 = this._gameObjectLOD1;
            clone._meshLOD1 = this._meshLOD1;
            clone._gameObjectLOD2 = this._gameObjectLOD2;
            clone._meshLOD2 = this._meshLOD2;

            clone.position = this.position;
            clone.rotation = this.rotation;
            clone.scale = this.scale;

            clone.meshPosition = this.meshPosition;
            clone.meshRotation = this.meshRotation;
            clone.meshScale = this.meshScale;

            clone.albedoTexProp = this.albedoTexProp;
            clone.hasNormalTex = this.hasNormalTex;
            clone.normalTexProp = this.normalTexProp;
            clone.hasExtraTex = this.hasExtraTex;
            clone.extraTexProp = this.extraTexProp;

            clone.trunkDefs = new List<TrunkDef>();
            if (this.trunkDefs != null) {
                foreach (var def in this.trunkDefs) {
                    if (def != null) {
                        clone.trunkDefs.Add(def.Clone());
                    }
                }
            }
            return clone;
        }
        #endregion

        #region Nested TrunkDef Class
        [System.Serializable]
        public class TrunkDef {
            public BezierCurve curve;
            public float girthScale = 1f;
            public float transitionLength;
            public float transitionRadius;

            public TrunkDef() {
                curve = new BezierCurve();
                curve.AddNode (new BezierNode (Vector3.zero, BezierNode.HandleStyle.Auto));
                curve.AddNode (new BezierNode (Vector3.one, BezierNode.HandleStyle.Auto));
            }

            public TrunkDef Clone() {
                TrunkDef clone = new TrunkDef();
                if (this.curve != null) {
                    clone.curve = this.curve.Clone();
                } else {
                    clone.curve = null;
                }
                clone.girthScale = this.girthScale;
                clone.transitionLength = this.transitionLength;
                clone.transitionRadius = this.transitionRadius;
                return clone;
            }
        }
        #endregion
    }
}