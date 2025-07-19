using UnityEngine;
using System.Collections.Generic;

using Broccoli.Base;

/// <summary>
/// Contains manager classes for the Broccoli Tree Engine.
/// </summary>
namespace Broccoli.Manager
{
    /// <summary>
    /// Manages the creation and lifecycle of child GameObjects under a specified parent.
    /// This class handles adding, updating, removing, and transforming GameObjects
    /// that are primarily defined by a mesh and materials.
    /// </summary>
    public class GameObjectManager
    {
        #region Properties and Variables
        /// <summary>
        /// The parent GameObject under which all children will be managed.
        /// </summary>
        private GameObject _parent;

        /// <summary>
        /// Internal dictionary to keep track of managed child GameObjects by their names.
        /// </summary>
        private Dictionary<string, GameObject> _managedGameObjects = new Dictionary<string, GameObject>();

        /// <summary>
        /// Public flag to control the visibility of newly created GameObjects in the hierarchy.
        /// If true, created GameObjects will be hidden.
        /// </summary>
        public bool hideChildrenInHierarchy = !GlobalSettings.showPreviewTreeInHierarchy;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new GameObjectManager instance.
        /// A parent GameObject must be set using the SetParent method before use.
        /// </summary>
        public GameObjectManager()
        {
            // The parent is intentionally left null until SetParent is called.
            _parent = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the parent GameObject for this manager. All subsequent GameObjects
        /// will be created as children of this parent.
        /// </summary>
        /// <param name="parentGameObject">The GameObject to use as the parent.</param>
        public void SetParent(GameObject parentGameObject)
        {
            _parent = parentGameObject;
        }

        /// <summary>
        /// Adds or finds a child GameObject with the given name.
        /// If the GameObject does not exist, it's created as an empty object.
        /// </summary>
        /// <param name="name">The name of the child GameObject. This will be its key.</param>
        /// <returns>The created or found GameObject instance, or null if the parent is not set.</returns>
        public GameObject AddMeshGameObject(string name)
        {
            if (_parent == null)
            {
                Debug.LogError("GameObjectManager: Cannot add GameObject. Parent has not been set. Call SetParent() first.");
                return null;
            }

            GameObject childGO;
            
            // 1. Check internal dictionary first for speed.
            if (_managedGameObjects.TryGetValue(name, out childGO) && childGO != null)
            {
                return childGO;
            }

            // 2. If not found, check the actual scene hierarchy.
            Transform childTransform = _parent.transform.Find(name);
            if (childTransform != null)
            {
                childGO = childTransform.gameObject;
                _managedGameObjects[name] = childGO; // Add found object to the manager.
                return childGO;
            }
            
            // 3. If it doesn't exist anywhere, create it.
            return CreateEmptyGameObject(name);
        }

        /// <summary>
        /// Adds a new child GameObject with a mesh and materials, or updates an existing one.
        /// </summary>
        /// <param name="name">The name of the child GameObject. This will be its key.</param>
        /// <param name="mesh">The Mesh to assign.</param>
        /// <param name="material">A single Material to assign.</param>
        /// <returns>The created or updated GameObject instance, or null if the parent is not set.</returns>
        public GameObject AddMeshGameObject(string name, Mesh mesh, Material material)
        {
            if (material == null) return AddMeshGameObject(name, mesh, new List<Material>());
            return AddMeshGameObject(name, mesh, new List<Material> { material });
        }

        /// <summary>
        /// Adds a new child GameObject with a mesh and materials, or updates an existing one.
        /// </summary>
        /// <param name="name">The name of the child GameObject. This will be its key.</param>
        /// <param name="mesh">The Mesh to assign.</param>
        /// <param name="materials">A list of Materials to assign.</param>
        /// <returns>The created or updated GameObject instance, or null if the parent is not set.</returns>
        public GameObject AddMeshGameObject(string name, Mesh mesh, List<Material> materials)
        {
            if (_parent == null)
            {
                Debug.LogError("GameObjectManager: Cannot add GameObject. Parent has not been set. Call SetParent() first.");
                return null;
            }

            GameObject childGO;

            // 1. Check internal dictionary first for speed.
            if (_managedGameObjects.TryGetValue(name, out childGO) && childGO != null)
            {
                // Found in dictionary, so just update it.
                UpdateGameObject(childGO, mesh, materials);
                return childGO;
            }

            // 2. If not found in dictionary, check the actual scene hierarchy.
            Transform childTransform = _parent.transform.Find(name);
            if (childTransform != null)
            {
                childGO = childTransform.gameObject;
                _managedGameObjects[name] = childGO; // Add found object to the manager.
                UpdateGameObject(childGO, mesh, materials); // Update it.
                return childGO;
            }
            
            // 3. If it truly doesn't exist anywhere, create a new one.
            return CreateGameObjectWithMesh(name, mesh, materials);
        }

        /// <summary>
        /// Checks if a GameObject with the given name is being managed.
        /// </summary>
        /// <param name="name">The name of the GameObject to check.</param>
        /// <returns>True if the GameObject exists, false otherwise.</returns>
        public bool HasGameObject(string name)
        {
            return _managedGameObjects.ContainsKey(name) && _managedGameObjects[name] != null;
        }

        /// <summary>
        /// Sets the local position, rotation (Euler angles), and scale of a managed child GameObject.
        /// </summary>
        /// <param name="name">The name of the child GameObject to transform.</param>
        /// <param name="position">The local position relative to the parent.</param>
        /// <param name="rotation">The local rotation as Euler angles.</param>
        /// <param name="scale">The local scale.</param>
        /// <returns>True if the GameObject was found and transformed, false otherwise.</returns>
        public bool SetTransform(string name, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            if (HasGameObject(name))
            {
                GameObject childGO = _managedGameObjects[name];
                if (childGO != null)
                {
                    childGO.transform.localPosition = position;
                    childGO.transform.localEulerAngles = rotation;
                    childGO.transform.localScale = scale;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes and destroys a managed child GameObject by its name.
        /// </summary>
        /// <param name="name">The name of the child GameObject to remove.</param>
        /// <returns>True if the GameObject was found and removed, false otherwise.</returns>
        public bool RemoveGameObject(string name)
        {
            if (HasGameObject(name))
            {
                GameObject childGO = _managedGameObjects[name];
                if (childGO != null)
                {
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(childGO);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(childGO);
                    }
                }
                _managedGameObjects.Remove(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes and destroys all managed child GameObjects.
        /// </summary>
        public void Clear()
        {
            foreach (var pair in _managedGameObjects)
            {
                if (pair.Value != null)
                {
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(pair.Value);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(pair.Value);
                    }
                }
            }
            _managedGameObjects.Clear();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates a new empty child GameObject. Assumes parent is set.
        /// </summary>
        private GameObject CreateEmptyGameObject(string name)
        {
            GameObject childGO = new GameObject(name);
            childGO.transform.SetParent(_parent.transform);

            childGO.transform.localPosition = Vector3.zero;
            childGO.transform.localRotation = Quaternion.identity;
            childGO.transform.localScale = Vector3.one;

            if (hideChildrenInHierarchy)
            {
                childGO.hideFlags = HideFlags.HideInHierarchy;
            }

            _managedGameObjects.Add(name, childGO);

            return childGO;
        }
        
        /// <summary>
        /// Creates a new child GameObject with mesh components. Assumes parent is set.
        /// </summary>
        private GameObject CreateGameObjectWithMesh(string name, Mesh mesh, List<Material> materials)
        {
            GameObject childGO = new GameObject(name);
            childGO.transform.SetParent(_parent.transform);

            childGO.transform.localPosition = Vector3.zero;
            childGO.transform.localRotation = Quaternion.identity;
            childGO.transform.localScale = Vector3.one;

            if (hideChildrenInHierarchy)
            {
                childGO.hideFlags = HideFlags.HideInHierarchy;
            }

            UpdateGameObject(childGO, mesh, materials);

            _managedGameObjects.Add(name, childGO);

            return childGO;
        }

        /// <summary>
        // Updates the MeshFilter and MeshRenderer components on a GameObject.
        /// </summary>
        private void UpdateGameObject(GameObject targetGO, Mesh mesh, List<Material> materials)
        {
            MeshFilter meshFilter = targetGO.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = targetGO.AddComponent<MeshFilter>();
            }
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = targetGO.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = targetGO.AddComponent<MeshRenderer>();
            }
            if (materials != null)
            {
                meshRenderer.sharedMaterials = materials.ToArray();
            }
        }
        #endregion
    }
}