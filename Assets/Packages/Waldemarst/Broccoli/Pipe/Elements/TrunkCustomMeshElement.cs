using System.Collections;
using System.Collections.Generic;
using Broccoli.Model;
using UnityEngine;
using UnityEngine.Serialization;

namespace Broccoli.Pipe {
    /// <summary>
    /// Takes custom meshes to used them as trunks.
    /// </summary>
    [System.Serializable]
    public class TrunkCustomMeshElement : PipelineElement {
        #region Vars
        /// <summary>
        /// Gets the type of the connection.
        /// </summary>
        /// <value>The type of the connection.</value>
        public override ConnectionType connectionType {
            get { return PipelineElement.ConnectionType.Transform; }
        }
        /// <summary>
        /// Gets the type of the element.
        /// </summary>
        /// <value>The type of the element.</value>
        public override ElementType elementType {
            get { return PipelineElement.ElementType.MeshGenerator; }
        }
        /// <summary>
        /// Gets the type of the class.
        /// </summary>
        /// <value>The type of the class.</value>
        public override ClassType classType {
            get { return PipelineElement.ClassType.TrunkCustomMesh; }
        }
        /// <summary>
        /// Value used to position elements in the pipeline. The greater the more towards the end of the pipeline.
        /// </summary>
        /// <value>The position weight.</value>
        public override int positionWeight {
            get { return PipelineElement.meshGeneratorWeight + 10; }
        }

        /// <summary>
        /// Trunk mesh descriptors.
        /// </summary>
        public List<TrunkMeshDescriptor> trunkMeshes = new List<TrunkMeshDescriptor>();
        
        /// <summary>
        /// Index of the selected trunk in the editor.
        /// </summary>
        public int selectedTrunkIndex = -1;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Broccoli.Pipe.TrunkCustomMeshElement"/> class.
        /// </summary>
        public TrunkCustomMeshElement () {
            this.elementName = "Trunk Custom Mesh";
            this.elementHelpURL = "https://docs.google.com/document/d/1Nr6Z808i7X2zMFq8PELezPuSJNP5IvRx9C5lJxZ_Z-A/edit#heading=h.qpkoisw82dlr";
            this.elementDescription = "This nodes provides parameters to assign custom meshes to be used as trunks.";
        }
        #endregion

        #region Data Access
        /// <summary>
        /// Gets all the curves from a trunk mesh.
        /// </summary>
        /// <param name="trunkMeshIndex">Index of the trunk mesh.</param>
        /// <returns>List of trunk curves.</returns>
        public List<BezierCurve> GetTrunkBranchCurves (int trunkMeshIndex)
        {
            List<BezierCurve> curves = new List<BezierCurve> ();
            if (trunkMeshIndex >= 0 && trunkMeshIndex < this.trunkMeshes.Count) {
                TrunkMeshDescriptor tmd = this.trunkMeshes [trunkMeshIndex];
                for (int i = 0; i < tmd.trunkDefs.Count; i++) {
                    BezierCurve curve = tmd.trunkDefs [i].curve.Clone ();
                    curve.valueA = tmd.trunkDefs [i].girthScale;
                    curve.Process ();
                    curves.Add (curve); 
                }
            }
            return curves;
        }
        /// <summary>
        /// Checks if the selected TrunkMeshDescriptor has a mesh set and is enabled.
        /// </summary>
        /// <returns>True is the selected TrunkMeshDescriptor is valid.</returns>
        public bool HasSelectedMeshAndIsEnabled ()
        {
            if (selectedTrunkIndex >= 0 && selectedTrunkIndex < trunkMeshes.Count) {
                TrunkMeshDescriptor trunkMeshDescriptor = trunkMeshes [selectedTrunkIndex];
                return trunkMeshDescriptor.enabled && trunkMeshDescriptor.Mesh != null;
            }
            return false;
        }
        /// <summary>
        /// Gets the selected TrunkMeshDescriptor.
        /// </summary>
        /// <returns>Selected TrunkMeshDescriptor instance, if none is selected then returns null.</returns>
        public TrunkMeshDescriptor GetSelectedTrunkMeshDescriptor ()
        {
            if (selectedTrunkIndex >= 0 && selectedTrunkIndex < trunkMeshes.Count) {
                return trunkMeshes [selectedTrunkIndex];
            }
            return null;
        }
        #endregion

        #region Cloning
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <param name="isDuplicate">If <c>true</c> then the clone has elements with new ids.</param>
        /// <returns>Clone of this instance.</returns>
        override public PipelineElement Clone (bool isDuplicate = false) {
            TrunkCustomMeshElement clone = ScriptableObject.CreateInstance<TrunkCustomMeshElement> ();
            SetCloneProperties (clone, isDuplicate);
            for (int i = 0; i < trunkMeshes.Count; i++) {
                clone.trunkMeshes.Add (trunkMeshes [i].Clone ());
            }
            clone.selectedTrunkIndex = this.selectedTrunkIndex;
            return clone;
        }
        #endregion
    }
}