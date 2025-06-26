using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

namespace Broccoli.Model
{
    /// <summary>
    /// Level of Detail (LOD) definition for trees.
    /// </summary>
	[System.Serializable]
	public class LODDef {
		#region Vars
        /// <summary>
        /// Predefined quality settings on LOD definitions.
        /// </summary>
        public enum Preset {
            UltraLowPoly,
            LowPoly,
            RegularPoly,
            HighPoly,
            UltraHighPoly,
            Custom
        }
        /// <summary>
        /// Selected LOD preset for this definition.
        /// </summary>
        public Preset preset = Preset.RegularPoly;
        /// <summary>
        /// <c>True</c> to include this instance in the LOD group for a prefab.
        /// </summary>
        public bool includeInPrefab = true;
        /// <summary>
        /// Minimum number of polygons to use on branches.
        /// </summary>
        public int minPolygonSides = 6;
        /// <summary>
        /// Maximum number of polygons to use on branches.
        /// </summary>
        public int maxPolygonSides = 16;
        /// <summary>
        /// Max angle tolerance on branch curves at the base of the tree.
        /// </summary>
        [FormerlySerializedAs ("branchAngleTolerance")]
        public float branchAngleToleranceAtBase = 24f;
        /// <summary>
        /// Max angle tolerance on branch curves at the top of the tree.
        /// </summary>
        [FormerlySerializedAs ("branchAngleTolerance")]
        public float branchAngleToleranceAtTop = 40f;
        /// <summary>
        /// Resolution value used to calculate the amount of geometry sprouts mesh
        /// units should add to the final tree. From 0 (most basic geometry) to 1 (most complex geometry).
        /// </summary>
        public float sproutResolution = 1f;
        /// <summary>
        /// Capping mesh at the base of each branch.
        /// </summary>
        public bool useMeshCapAtBase = true;
        /// <summary>
        /// Allows applying branch welding to the tree.
        /// </summary>
        public bool allowBranchWelding = true;
        /// <summary>
        /// Allows appying root welding to the tree.
        /// </summary>
        public bool allowRootWelding = true;
        /// <summary>
        /// How much percentage this LOD definition takes on a LOD group.
        /// </summary>
        public float groupPercentage = 0.3f;
        /// <summary>
        /// Flag set to true on preset LODs configuration containing custom values.
        /// </summary>
        public bool hasCustomValues = false;
		#endregion

        #region Presets
        /// <summary>
        /// Get a preset LOD definition.
        /// </summary>
        /// <param name="preset">Preset for the LOD.</param>
        /// <param name="lodDef">Optional LODDef instance to set the preset values to.</param>
        /// <returns>LODDef instance.</returns>
        public static LODDef GetPreset (Preset preset, LODDef lodDef = null) {
            if (lodDef == null) {
                lodDef = new LODDef ();
            }
            lodDef.preset = preset;
            switch (lodDef.preset) {
                case Preset.UltraLowPoly:
                    lodDef.minPolygonSides = 3;
                    lodDef.maxPolygonSides = 5;
                    lodDef.branchAngleToleranceAtBase = 65f;
                    lodDef.branchAngleToleranceAtTop = 85f;
                    lodDef.sproutResolution = 0f;
                    lodDef.useMeshCapAtBase = false;
                    lodDef.allowBranchWelding = false;
                    lodDef.allowRootWelding = false;
                    lodDef.groupPercentage = 0.28f;
                    break;
                case Preset.LowPoly:
                    lodDef.minPolygonSides = 3;
                    lodDef.maxPolygonSides = 6;
                    lodDef.branchAngleToleranceAtBase = 46f;
                    lodDef.branchAngleToleranceAtTop = 70f;
                    lodDef.sproutResolution = 0.25f;
                    lodDef.useMeshCapAtBase = false;
                    lodDef.allowBranchWelding = false;
                    lodDef.allowRootWelding = false;
                    lodDef.groupPercentage = 0.21f;
                    break;
                case Preset.RegularPoly:
                case Preset.Custom:
                    lodDef.minPolygonSides = 3;
                    lodDef.maxPolygonSides = 8;
                    lodDef.branchAngleToleranceAtBase = 34f;
                    lodDef.branchAngleToleranceAtTop = 60f;
                    lodDef.sproutResolution = 0.5f;
                    lodDef.groupPercentage = 0.15f;
                    break;
                case Preset.HighPoly:
                    lodDef.minPolygonSides = 3;
                    lodDef.maxPolygonSides = 9;
                    lodDef.branchAngleToleranceAtBase = 22f;
                    lodDef.branchAngleToleranceAtTop = 45f;
                    lodDef.sproutResolution = 0.75f;
                    lodDef.groupPercentage = 0.1f;
                    break;
                case Preset.UltraHighPoly:
                    lodDef.minPolygonSides = 3;
                    lodDef.maxPolygonSides = 12;
                    lodDef.branchAngleToleranceAtBase = 10f;
                    lodDef.branchAngleToleranceAtTop = 35f;
                    lodDef.sproutResolution = 1f;
                    lodDef.groupPercentage = 0.06f;
                    break;
            }
            return lodDef;
        }
        #endregion

		#region Cloning
		/// <summary>
		/// Clone this instance.
		/// </summary>
		public LODDef Clone() {
			LODDef clone = new LODDef ();
            clone.preset = preset;
            clone.includeInPrefab = includeInPrefab;
            clone.minPolygonSides = minPolygonSides;
            clone.maxPolygonSides = maxPolygonSides;
            clone.branchAngleToleranceAtBase = branchAngleToleranceAtBase;
            clone.branchAngleToleranceAtTop = branchAngleToleranceAtTop;
            clone.sproutResolution = sproutResolution;
            clone.useMeshCapAtBase = useMeshCapAtBase;
            clone.allowBranchWelding = allowBranchWelding;
            clone.allowRootWelding = allowRootWelding;
            clone.groupPercentage = groupPercentage;
            clone.hasCustomValues = hasCustomValues;
			return clone;
		}
		#endregion
	}
}