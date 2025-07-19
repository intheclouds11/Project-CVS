using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

using Broccoli.Model;
using Broccoli.Utils;
using Broccoli.Pipe;

namespace Broccoli.Generator
{
	using Position = Broccoli.Pipe.Position;
    #region StructureLevel Class
    /// <summary>
    /// Class containing the specifications for trunks generation.
    /// </summary>
    [System.Serializable]
    public class TrunkStructureLevel : StructureGenerator.StructureLevel {
        #region Vars
        /// <summary>
        /// Modes available to generate trunks.
        /// </summary>
        public enum TrunkMode {
            Frequency = 0,
            CustomMesh = 1,
        }
        /// <summary>
        /// Mode selected to generate trunks.
        /// </summary>
        public TrunkMode trunkMode = TrunkMode.Frequency;
        #endregion

        #region Clone
        /// <summary>
        /// Clone this instance.
        /// </summary>
        public TrunkStructureLevel Clone () {
            TrunkStructureLevel clone = new TrunkStructureLevel ();
            base.Clone (clone);
            clone.trunkMode = trunkMode;
            return clone;
        }
        #endregion

        #region Debug
        public override string GetDebugInfo () {
            string info = base.GetDebugInfo ();
            info += $"Trunk Mode: {trunkMode}\n";
            return info;
        }
        #endregion
    }
    
    #endregion
}