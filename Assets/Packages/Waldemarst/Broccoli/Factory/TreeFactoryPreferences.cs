using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Base;
using Broccoli.Model;
using Broccoli.Serialization;

namespace Broccoli.Factory
{
	/// <summary>
	/// Tree factory preferences.
	/// </summary>
	[System.Serializable]
	public class TreeFactoryPreferences {
		#region Vars
		/// <summary>
		/// The scale applied to all the produced objects by the factory.
		/// </summary>
		public float factoryScale = 1.0f;
		/// <summary>
		/// LOD definitios to build this structure.
		/// </summary>
		/// <typeparam name="LODDef">LOD definition instance.</typeparam>
		/// <returns>List of LOD definitions.</returns>
		public List<LODDef> lods = new List<LODDef> ();
		/// <summary>
		/// The index of the LODDef to be used as preview default.
		/// </summary>
		public int previewLODIndex = -1;
		/// <summary>
		/// Debug option to show sprout direction and normals.
		/// </summary>
		public bool debugShowDrawSprouts = true;
		/// <summary>
		/// Debug option to show branch structure.
		/// </summary>
		public bool debugShowDrawBranches = true;
		/// <summary>
		/// The canvas offset.
		/// </summary>
		public Vector2 canvasOffset = Vector3.zero;
		/// <summary>
		/// Offset for the pipeline node graph.
		/// </summary>
		public Vector2 graphOffset = Vector3.zero;
		/// <summary>
		/// Default zoom factor for the pipeline node graph.
		/// </summary>
		public float graphZoom = 1f;
		/// <summary>
		/// Save path for the prefabs.
		/// </summary>
		public string prefabSavePath = GlobalSettings.prefabSavePath;
		/// <summary>
		/// Prefix used to name prefabs.
		/// </summary>
		public string prefabSavePrefix = GlobalSettings.prefabSavePrefix;
		#endregion

		#region Preview Vars
		/// <summary>
		/// The preview mode to build trees.
		/// </summary>
		public TreeFactory.PreviewMode previewMode = TreeFactory.PreviewMode.Colored;
		#endregion

		#region Material Vars
		/// <summary>
		/// Enumeration for the available tree shaders to use.
		/// </summary>
		public enum PreferredShader {
			//TreeCreator = 0, // Deprecated, no longer supported by Unity, no URP support.
			SpeedTree7 = 1,
			SpeedTree8 = 2,
			//TreeCreatorCompatible = 3, // Deprecated, no longer supported by Unity, no URP support.
			SpeedTree7Compatible = 4,
			SpeedTree8Compatible = 5
		}
		/// <summary>
		/// Preferred tree shader to use.
		/// </summary>
		public PreferredShader preferredShader = PreferredShader.SpeedTree8;
		/// <summary>
		/// Custom shader to use on branches when a compatible shader option is selected.
		/// </summary>
		public Shader customBranchShader = null;
		/// <summary>
		/// Custom shader to use on sprouts when a compatible shader option is selected.
		/// </summary>
		public Shader customSproutShader = null;
		/// <summary>
		/// If true and cloning is enabled then the custom material is used as a base for 
		/// the preview and prefab material using a native shader (Unity's tree creator).
		/// </summary>
		public bool overrideMaterialShaderEnabled { get { return false; } }
		#endregion

		#region Prefab Vars
		/// <summary>
		/// If true a texture atlas is created on the prefab process.
		/// </summary>
		public bool prefabCreateAtlas = true;
		/// <summary>
		/// The size of the atlas texture.
		/// </summary>
		public TreeFactory.TextureSize atlasTextureSize = TreeFactory.TextureSize._512px;
		/// <summary>
		/// The size of the billboard texture.
		/// </summary>
		public TreeFactory.TextureSize billboardTextureSize = TreeFactory.TextureSize._512px;
		/// <summary>
		/// Number of side images to take on the billboard asset creation process.
		/// </summary>
		public int billboardImageCount = 8;
		/// <summary>
		/// LOD group includes a billboard asset.
		/// </summary>
		public bool prefabIncludeBillboard = true;
		/// <summary>
		/// The billboard percentage in the LOD group.
		/// </summary>
		public float prefabBillboardPercentage = 0.3f;
		/// <summary>
		/// If true the prefab mesh coordinates are adjusted to match vector zero position.
		/// </summary>
		public bool prefabRepositionEnabled = true;
		/// <summary>
		/// If true when using custom materials these are cloned inside the prefab.
		/// </summary>
		public bool prefabCloneCustomMaterialEnabled = true;
		/// <summary>
		/// If true the textures from a bark custom material are copied to the prefab folder.
		/// </summary>
		public bool prefabCopyCustomMaterialBarkTexturesEnabled = true;
		/// <summary>
		/// If true, the created meshes are kept in the asset's folder and not within the prefab asset.
		/// </summary>
		public bool prefabIncludeMeshesInsidePrefab = true;
		/// <summary>
		/// If true, the created materials are kept in the asset's folder and not within the prefab asset.
		/// </summary>
		public bool prefabIncludeMaterialsInsidePrefab = false;
		/// <summary>
		/// Seed to use when generating with a seed.
		/// </summary>
		public int customSeed = 0;
		/// <summary>
		/// The appendable components.
		/// </summary>
		public List<ComponentReference> appendableComponents;
		/// <summary>
		/// Saves information about the default view of this pipeline when selected.
		/// Default or Catalog or SproutLab.
		/// </summary>
		[System.NonSerialized]
		public int editorView = 0;
		#endregion

		#region LOD
		/// <summary>
		/// Gets a LOD definition given its index, else a default definition is returned.
		/// </summary>
		/// <param name="index">Index for the LOD definition.</param>
		/// <returns>LOD definition.</returns>
		public LODDef GetLOD (int index)
		{
			if (index >= 0 && index < lods.Count)
				return lods [index];
			return LODDef.GetPreset (LODDef.Preset.RegularPoly);
		}
		/// <summary>
		/// Returns the default preview LOD definition.
		/// </summary>
		/// <returns></returns>
		public LODDef GetPreviewLOD ()
		{
			if (previewLODIndex >= 0 && previewLODIndex < lods.Count) {
				return lods [previewLODIndex];
			} else {
				previewLODIndex = 0;
			}
			return LODDef.GetPreset (LODDef.Preset.RegularPoly);
		}
		public float GetLODFactor (int index, float minFactor = 0f, float maxFactor = 1f)
		{
			float factor = 1f;
			// 1. Validate inputs
			if (lods == null || lods.Count == 0 || index < 0 || index >= lods.Count)
			{
				return Mathf.Lerp (minFactor, maxFactor, factor); // Invalid input
			}

			// 2. Find all indices where the value is true.
			var trueIndices = new List<int>();
			for (int i = 0; i < lods.Count; i++)
			{
				if (lods[i].includeInPrefab)
				{
					trueIndices.Add(i);
				}
			}

			// 3. Handle edge cases based on the count of 'true' values.
			int totalTrues = trueIndices.Count;

			// Case A: No true values at all.
			if (totalTrues == 0)
			{
				return Mathf.Lerp (minFactor, maxFactor, 0.5f); // All are false, so everything is in the middle.
			}

			// Case B: Only one true value exists.
			if (totalTrues == 1)
			{
				if (index < trueIndices[0]) return Mathf.Lerp (minFactor, maxFactor, 0f);;      // Before the single true point.
				if (index > trueIndices[0]) return Mathf.Lerp (minFactor, maxFactor, 1f);;      // After the single true point.
				return Mathf.Lerp (minFactor, maxFactor, 0.5f);;                                // Exactly at the single true point.
			}

			// 4. If the value at the given index is true, calculate its direct normalized position.
			if (lods[index].includeInPrefab)
			{
				int positionInTrueList = trueIndices.IndexOf(index);
				factor = (float)positionInTrueList / (totalTrues - 1);
				return Mathf.Lerp (minFactor, maxFactor, factor);
			}
			else // 5. If the value is false, interpolate its position.
			{
				int prevTrueIndex = -1;
				int nextTrueIndex = -1;

				// Find the nearest true value before the index.
				for (int i = index - 1; i >= 0; i--) {
					if (lods[i].includeInPrefab) {
						prevTrueIndex = i;
						break;
					}
				}

				// Find the nearest true value after the index.
				for (int i = index + 1; i < lods.Count; i++) {
					if (lods[i].includeInPrefab) {
						nextTrueIndex = i;
						break;
					}
				}

				// Case C: The false value is before all true values.
				if (prevTrueIndex == -1)
				{
					return Mathf.Lerp (minFactor, maxFactor, 0f);;
				}

				// Case D: The false value is after all true values.
				if (nextTrueIndex == -1)
				{
					return Mathf.Lerp (minFactor, maxFactor, 1f);;
				}
				
				// Case E: The false value is between two true values.
				// Get the normalized positions of the surrounding true values.
				int positionOfPrev = trueIndices.IndexOf(prevTrueIndex);
				int positionOfNext = trueIndices.IndexOf(nextTrueIndex);
				
				float normPosPrev = (float)positionOfPrev / (totalTrues - 1);
				float normPosNext = (float)positionOfNext / (totalTrues - 1);

				// Calculate the interpolation factor based on distance.
				float totalDistance = nextTrueIndex - prevTrueIndex;
				float distanceToCurrent = index - prevTrueIndex;
				float t = distanceToCurrent / totalDistance;

				// Return the linearly interpolated value.
				factor = Mathf.Lerp(normPosPrev, normPosNext, t);
				return Mathf.Lerp (minFactor, maxFactor, factor);
			}
		}
		#endregion

		#region Clone
		/// <summary>
		/// Clone this instance.
		/// </summary>
		public TreeFactoryPreferences Clone () {
			TreeFactoryPreferences clone = new TreeFactoryPreferences ();
			clone.factoryScale = factoryScale;
			for (int i = 0; i < lods.Count; i++) {
				clone.lods.Add (lods [i].Clone ());
			}
			clone.previewLODIndex = previewLODIndex;
			clone.previewMode = previewMode;
			clone.prefabCreateAtlas = prefabCreateAtlas;
			clone.atlasTextureSize = atlasTextureSize;
			clone.billboardTextureSize = billboardTextureSize;
			clone.billboardImageCount = billboardImageCount;
			/*
			clone.prefabStrictLowPoly = prefabStrictLowPoly;
			clone.prefabUseLODGroups = prefabUseLODGroups;
			*/
			clone.prefabIncludeBillboard = prefabIncludeBillboard;
			clone.prefabBillboardPercentage = prefabBillboardPercentage;
			clone.prefabRepositionEnabled = prefabRepositionEnabled;
			clone.prefabCloneCustomMaterialEnabled = prefabCloneCustomMaterialEnabled;
			clone.prefabCopyCustomMaterialBarkTexturesEnabled = prefabCopyCustomMaterialBarkTexturesEnabled;
			clone.prefabIncludeMeshesInsidePrefab = prefabIncludeMeshesInsidePrefab;
			clone.prefabIncludeMaterialsInsidePrefab = prefabIncludeMaterialsInsidePrefab;
			clone.customSeed = customSeed;
			clone.appendableComponents = new List<ComponentReference> (appendableComponents);
			clone.debugShowDrawSprouts = debugShowDrawSprouts;
			clone.debugShowDrawBranches = debugShowDrawBranches;
			clone.canvasOffset = canvasOffset;
			clone.graphOffset = graphOffset;
			clone.graphZoom = graphZoom;
			clone.prefabSavePath = prefabSavePath;
			clone.prefabSavePrefix = prefabSavePrefix;
			clone.preferredShader = preferredShader;
			clone.customSproutShader = customSproutShader;
			clone.customBranchShader = customBranchShader;
			clone.editorView = editorView;
			return clone;
		}
		#endregion
	}
}