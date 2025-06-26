using System.Collections.Generic;

using UnityEngine;

using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.Factory;

namespace Broccoli.Component
{
	using AssetManager = Broccoli.Manager.AssetManager;
	/// <summary>
	/// Sprout mapper component.
	/// Set materials and UV mapping for sprout elements.
	/// </summary>
	public class SproutMapperComponent : TreeFactoryComponent {
		#region Vars
		/// <summary>
		/// The sprout mapper element.
		/// </summary>
		SproutMapperElement sproutMapperElement = null;
		/// <summary>
		/// The sprout meshes.
		/// </summary>
		Dictionary<int, SproutMesh> sproutMeshes = new Dictionary <int, SproutMesh> ();
		/// <summary>
		/// The sprout mappers.
		/// </summary>
		Dictionary<int, SproutMap> sproutMappers = new Dictionary <int, SproutMap> ();
		/// <summary>
		/// Component command.
		/// </summary>
		public enum ComponentCommand
		{
			BuildMaterials,
			SetUVs
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
			// Gather all SproutGroup objects from elements upstream.
			List<PipelineElement> sproutMeshGenerators = 
				sproutMapperElement.GetUpstreamElements(PipelineElement.ClassType.SproutMeshGenerator);
			sproutMeshes.Clear ();

			for (int i = 0; i < sproutMeshGenerators.Count; i++) {
				SproutMeshGeneratorElement sproutMeshGeneratorElement = (SproutMeshGeneratorElement)sproutMeshGenerators[i];
				for (int j = 0; j < sproutMeshGeneratorElement.sproutMeshes.Count; j++) {
					if (sproutMeshGeneratorElement.sproutMeshes[j].groupId > 0) {
						sproutMeshes.Add (sproutMeshGeneratorElement.sproutMeshes[j].groupId, 
							sproutMeshGeneratorElement.sproutMeshes[j]);
					}
				}
			}
			sproutMeshGenerators.Clear ();

			sproutMappers.Clear ();
			for (int i = 0; i < sproutMapperElement.sproutMaps.Count; i++) {
				if (!sproutMappers.ContainsKey (sproutMapperElement.sproutMaps[i].groupId)) {
					sproutMappers.Add (sproutMapperElement.sproutMaps[i].groupId, 
						sproutMapperElement.sproutMaps[i]);
				}
			}
		}
		/// <summary>
		/// Gets the changed aspects on the tree for this component.
		/// </summary>
		/// <returns>The changed aspects.</returns>
		public override int GetChangedAspects () {
			return (int)TreeFactoryProcessControl.ChangedAspect.Material;
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public override void Clear ()
		{
			base.Clear ();
			sproutMapperElement = null;
			sproutMeshes.Clear ();
			sproutMappers.Clear ();
		}
		#endregion

		#region Processing
		/// <summary>
		/// Process the tree according to the pipeline element.
		/// </summary>
		/// <param name="treeFactory">Parent tree factory.</param>
		/// <param name="useCache">If set to <c>true</c> use cache.</param>
		/// <param name="useLocalCache">If set to <c>true</c> use local cache.</param>
		/// <param name="ProcessControl">Process control.</param>
		public override bool Process (TreeFactory treeFactory, 
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl ProcessControl = null) {
			if (pipelineElement != null && tree != null) {
				sproutMapperElement = pipelineElement as SproutMapperElement;
				PrepareParams (treeFactory, useCache, useLocalCache);
				BuildMaterials (treeFactory);
				AssignUVs (treeFactory);
				for (int i = 0; i < sproutMapperElement.sproutMaps.Count; i++) {
					if (sproutMapperElement.sproutMaps [i].colorVarianceMode == SproutMap.ColorVarianceMode.Shades ||
						sproutMapperElement.sproutMaps [i].alphaVarianceMode == SproutMap.AlphaVarianceMode.Dissolve)
					{
						AssignColorVariance (treeFactory, sproutMapperElement.sproutMaps [i]);
					}
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Removes the product of this component on the factory processing.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void Unprocess (TreeFactory treeFactory) {
			treeFactory.meshManager.DeregisterMeshByType (MeshManager.MeshData.Type.Sprout);
			treeFactory.materialManager.DeregisterMaterialByType (MeshManager.MeshData.Type.Sprout);
		}
		/// <summary>
		/// Process a special command or subprocess on this component.
		/// </summary>
		/// <param name="cmd">Cmd.</param>
		/// <param name="treeFactory">Tree factory.</param>
		public override void ProcessComponentOnly (int cmd, TreeFactory treeFactory) {
			if (pipelineElement != null && tree != null) {
				if (cmd == (int)ComponentCommand.BuildMaterials) {
					BuildMaterials (treeFactory, true);
				} else {
					AssignUVs (treeFactory, true);
					for (int i = 0; i < sproutMapperElement.sproutMaps.Count; i++) {
						if (sproutMapperElement.sproutMaps [i].colorVarianceMode == SproutMap.ColorVarianceMode.Shades ||
							sproutMapperElement.sproutMaps [i].alphaVarianceMode == SproutMap.AlphaVarianceMode.Dissolve)
						{
							AssignColorVariance (treeFactory, sproutMapperElement.sproutMaps [i]);
						}
					}
				}
			}
		}
		#endregion

		#region Prefab Processing
		/// <summary>
		/// Processes called only on the prefab creation.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void OnProcessPrefab (TreeFactory treeFactory) {
			#if UNITY_EDITOR
			var sproutMeshesEnumerator = sproutMeshes.GetEnumerator ();
			int groupId;
			int meshId;
			while (sproutMeshesEnumerator.MoveNext ()) {
				groupId = sproutMeshesEnumerator.Current.Key;
				SproutGroups.SproutGroup sproutGroup = treeFactory.localPipeline.sproutGroups.GetSproutGroup (groupId);				
				if (sproutMappers.ContainsKey (groupId)) {
					// IF material comes from the Branch Collection.
					if (sproutGroup.branchCollection != null) {
						Material material;
						meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Sprout, groupId);
						if (treeFactory.meshManager.HasMeshAndNotEmpty (meshId)) {
							// Use a clone of the original material if specified by the preferences.
							material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Sprout, true, groupId);
							material.name = "Sprout Material " + groupId;
							if (material != null) {
								treeFactory.assetManager.AddMaterialToPrefab (
									material,
									treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Sprout, groupId), 
									groupId);
							}
						}
					}
					// If is a generted or custom material.
					else {
						// If texture mode.
						if (sproutMappers[groupId].IsTextured ()) {
							// For each area.
							SproutMap.SproutMapArea sproutArea;
							for (int i = 0; i < sproutMappers[groupId].sproutAreas.Count; i++) {
								sproutArea = sproutMappers[groupId].sproutAreas[i];
								meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Sprout, groupId, i);
								if (treeFactory.meshManager.HasMeshAndNotEmpty (meshId)) {
									// Register as a native material CLONING
									if (treeFactory.treeFactoryPreferences.prefabCloneCustomMaterialEnabled) {
										Material material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Sprout, true, groupId, i);
										treeFactory.assetManager.AddMaterialParams (
											new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Native,
												treeFactory.treeFactoryPreferences.prefabCreateAtlas, true),
											treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Sprout, groupId, i));
										// Add the material to the asset manager.
										if (material != null) {
											material.name = "Optimized Sprout Material " + groupId + "." + i;
											treeFactory.assetManager.AddMaterialToPrefab (
												material,
												treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Sprout, groupId, i), 
												groupId,
												sproutArea);
										}
									} else {
										Material material = treeFactory.materialManager.GetMaterial (meshId, false);
										treeFactory.assetManager.AddMaterialParams (
											new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Native,
												treeFactory.treeFactoryPreferences.prefabCreateAtlas, true),
											treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Sprout, groupId, i));
										if (material != null) {
											treeFactory.assetManager.AddMaterialToPrefab (
												material,
												treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Sprout, groupId, i), 
												groupId,
												sproutArea);
										}
									}
								}
							}
						} else {
							// If custom material mode.
							Material material;
							meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Sprout, groupId);
							if (treeFactory.meshManager.HasMeshAndNotEmpty (meshId)) {
								if (treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
									// Create material based on the custom one.
									material = treeFactory.materialManager.GetOverridedMaterial (meshId, true);
									// Register as a native material (using the tree creator shader).
									treeFactory.assetManager.AddMaterialParams (
										new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Native,
											treeFactory.treeFactoryPreferences.prefabCreateAtlas, false),
										treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Sprout, groupId));
									material.name = "Optimized Sprout Material " + groupId;
								} else {
									// Use a clone of the original material if specified by the preferences.
									if (treeFactory.treeFactoryPreferences.prefabCloneCustomMaterialEnabled) {
										material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Sprout, true, groupId);
										material.name = "Sprout Material " + groupId;
									} else {
										material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Sprout, false, groupId);
									}
								}
								if (material != null) {
									treeFactory.assetManager.AddMaterialToPrefab (
										material,
										treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Sprout, groupId), 
										groupId);
								}
							}
						}
					}
				}
			}
			#endif
		}
		/// <summary>
		/// Gets the process prefab weight.
		/// </summary>
		/// <returns>The process prefab weight.</returns>
		/// <param name="treeFactory">Tree factory.</param>
		public override int GetProcessPrefabWeight (TreeFactory treeFactory) {
			// TODO: weith for atlas should go on the asset manager.
			int totalWeight = 0;
			if (treeFactory.treeFactoryPreferences.prefabCreateAtlas) {
				var sproutMeshesEnumerator = sproutMeshes.GetEnumerator ();
				int groupId;
				while (sproutMeshesEnumerator.MoveNext ()) {
					groupId = sproutMeshesEnumerator.Current.Key;
					switch (sproutMeshes [groupId].shapeMode) {
					//case SproutMesh.Mode.Billboard:
					case SproutMesh.ShapeMode.Plane:
					case SproutMesh.ShapeMode.Cross:
					case SproutMesh.ShapeMode.Tricross:
					case SproutMesh.ShapeMode.GridPlane:
						if (sproutMappers.ContainsKey (groupId)) {
							if (sproutMappers [groupId].IsTextured ()) {
								for (int i = 0; i < sproutMappers[groupId].sproutAreas.Count; i++) {
									if (sproutMappers[groupId].sproutAreas[i].texture != null && 
										sproutMappers[groupId].sproutAreas[i].enabled) {
										totalWeight += 10;
									}
								}
							} else {
								totalWeight += 10;
							}
						}
						break;
					case SproutMesh.ShapeMode.Mesh:
						totalWeight += 30;
						break;
					}
				}
				if (totalWeight > 0) {
					// Weight for creating atlas.
					totalWeight += 40;
				}
			}
			return totalWeight;
		}
		#endregion

		#region Materials
		/// <summary>
		/// Builds the materials for the sprouts.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		private void BuildMaterials (TreeFactory treeFactory, bool updatePreviewTree = false) {
			var sproutMeshesEnumerator = sproutMeshes.GetEnumerator ();
			int groupId;
			SproutMap sproutMap;
			while (sproutMeshesEnumerator.MoveNext ()) {
				groupId = sproutMeshesEnumerator.Current.Key;
				SproutGroups.SproutGroup sproutGroup = pipelineElement.pipeline.sproutGroups.GetSproutGroup (groupId);				
				if (sproutMappers.ContainsKey (groupId)) {
					sproutMap = sproutMappers [groupId];
					// IF material comes from the Branch Collection.
					if (sproutGroup.branchCollection != null) {
						BranchDescriptorCollection branchDescriptorCollection = 
							((BranchDescriptorCollectionSO)sproutGroup.branchCollection).branchDescriptorCollection;
						Material m = treeFactory.materialManager.GetMaterial(MeshManager.MeshData.Type.Sprout, false, groupId, 0);
						// If the material has not been registered, create it.
						if (m == null) {
							m = SproutCompositeManager.GenerateMaterial (sproutMap.color, sproutMap.alphaCutoff,
								sproutMap.glossiness, sproutMap.metallic, sproutMap.subsurfaceValue, sproutMap.subsurfaceColor,
								branchDescriptorCollection.atlasAlbedoTexture, branchDescriptorCollection.atlasNormalsTexture,
								branchDescriptorCollection.atlasExtrasTexture, branchDescriptorCollection.atlasSubsurfaceTexture,
								true);
							MaterialManager.SetDiffusionProfile (m, sproutMap.diffusionProfileSettings, sproutMap.alphaCutoff);
							treeFactory.materialManager.RegisterCustomMaterial (MeshManager.MeshData.Type.Sprout, m, groupId, 0);
						}
						// If the material has been created, update its values.
						else {
							MaterialManager.SetLeavesMaterialProperties (
								m, Color.white, sproutMap.alphaCutoff,
								sproutMap.glossiness, sproutMap.metallic, sproutMap.subsurfaceValue, sproutMap.subsurfaceColor,
								branchDescriptorCollection.atlasAlbedoTexture, branchDescriptorCollection.atlasNormalsTexture,
								branchDescriptorCollection.atlasExtrasTexture, branchDescriptorCollection.atlasSubsurfaceTexture, null);
							MaterialManager.SetDiffusionProfile (m, sproutMap.diffusionProfileSettings, sproutMap.alphaCutoff);
						}
					}
					// ELSE generate the materials.
					else {
						if (sproutMap.mode == SproutMap.Mode.Texture) {
							SproutMap.SproutMapArea sproutArea;
							for (int i = 0; i < sproutMappers[groupId].sproutAreas.Count; i++) {
								sproutArea = sproutMap.sproutAreas [i];
								int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Sprout, groupId, i);

								/// Get an existing material from the material manager or create a new one.
								Material material;
								//TODO: why the else? shouldn't the manager take care of returning a new material if none has been created?
								if (treeFactory.materialManager.HasMaterial (meshId) && 
									!treeFactory.materialManager.IsCustomMaterial (meshId) &&
									treeFactory.materialManager.GetMaterial (meshId) != null) {
									material = treeFactory.materialManager.GetMaterial (meshId);
								} else {
									material = treeFactory.materialManager.GetOwnedMaterial (meshId, treeFactory.materialManager.GetLeavesShader ().name);
								}
								treeFactory.materialManager.SetLeavesMaterialProperties (material, sproutMap, sproutArea);
								material.name = "Sprout " + meshId;
							}
						} else if (sproutMap.IsMaterialMode() &&
							sproutMap.customMaterial != null) {
							if (sproutMap.mode == SproutMap.Mode.MaterialOverride) {
								// Material Override Mode Cloning the Material
								if (treeFactory.treeFactoryPreferences.prefabCloneCustomMaterialEnabled) {
									SproutMap.SproutMapArea sproutArea;
									for (int i = 0; i < sproutMappers[groupId].sproutAreas.Count; i++) {
										sproutArea = sproutMap.sproutAreas [i];
										int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Sprout, groupId, i);

										/// Get a cloned the material.
										Material material;
										if (treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
											material = treeFactory.materialManager.GetOwnedMaterial (meshId, treeFactory.materialManager.GetLeavesShader ().name);
										} else {
											material = treeFactory.materialManager.GetOwnedMaterial (meshId, 
												sproutMap.customMaterial);
										}
										MaterialManager.OverrideLeavesMaterialProperties (material, sproutMap, sproutArea);
										material.name = "Sprout " + meshId;
									}
								} else {
									// Material Override NOT Cloning the Material
									SproutMap.SproutMapArea sproutArea;
									for (int i = 0; i < sproutMappers[groupId].sproutAreas.Count; i++) {
										sproutArea = sproutMap.sproutAreas [i];
										int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Sprout, groupId, i);
										treeFactory.materialManager.RegisterCustomMaterial (meshId, sproutMap.customMaterial);
									}
								}
							} else {
								// Material Mode.
								Material customMaterial = sproutMap.customMaterial;
								treeFactory.materialManager.RegisterCustomMaterial (MeshManager.MeshData.Type.Sprout,
									customMaterial, groupId, 0);
							}
						} else {
							treeFactory.materialManager.DeregisterMaterial (MeshManager.MeshData.Type.Sprout, groupId);
						}
					}



				}
				if (updatePreviewTree) {
					MeshRenderer renderer = tree.obj.GetComponent<MeshRenderer> ();
					Material[] materials = renderer.sharedMaterials;
					for (int j = 0; j < treeFactory.meshManager.GetMeshesCount (); j++) {
						int meshId = treeFactory.meshManager.GetMergedMeshId (j);
						if (treeFactory.materialManager.HasMaterial (meshId)) {
							if (treeFactory.materialManager.IsCustomMaterial (meshId) &&
							    treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
								bool isSprout = treeFactory.meshManager.IsSproutMesh (meshId);
								materials [j] = treeFactory.materialManager.GetOverridedMaterial (meshId, isSprout);
							} else {
								materials [j] = treeFactory.materialManager.GetMaterial (meshId);
							}
						} else if (materials [j] != null) {
							materials [j] = null;
						}
					}
					renderer.sharedMaterials = materials;
				}
			}
		}
		#endregion

		#region UVs and Colors
		/// <summary>
		/// Assigns the UVs to the sprout meshes.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		private void AssignUVs (TreeFactory treeFactory, bool updatePreviewTree = false) {
			var sproutMeshesEnumerator = sproutMeshes.GetEnumerator ();
			int groupId;
			while (sproutMeshesEnumerator.MoveNext ()) {
				groupId = sproutMeshesEnumerator.Current.Key;
				if (sproutMappers.ContainsKey (groupId) && sproutMeshesEnumerator.Current.Value.branchCollection == null) {
					if (sproutMappers [groupId].IsTextured ()) {
						List<Vector4> originalUVs = new List<Vector4> ();
						if (updatePreviewTree) {
							MeshFilter meshFilter = tree.obj.GetComponent<MeshFilter> ();
							meshFilter.sharedMesh.GetUVs (0, originalUVs);
						}
						SproutMap.SproutMapArea sproutArea;
						for (int i = 0; i < sproutMappers[groupId].sproutAreas.Count; i++) {
							sproutArea = sproutMappers [groupId].sproutAreas [i];
							int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Sprout, groupId, i);
							List<Vector4> uvs = new List<Vector4> ();
							Mesh mesh = treeFactory.meshManager.GetMesh (meshId);
							mesh = treeFactory.meshManager.GetMesh (meshId);
							if (mesh != null) {
								mesh.GetUVs (0, uvs);
								SproutMeshMetaBuilder.GetInstance ().GetCropUVs (ref uvs, 
									sproutArea.x, 
									sproutArea.y, 
									sproutArea.width, 
									sproutArea.height, 
									sproutArea.normalizedStep);
								mesh.SetUVs (0, uvs);
							}
							if (updatePreviewTree) {
								int vertexOffset = treeFactory.meshManager.GetMergedMeshVertexOffset (meshId);
								for (int j = 0; j < uvs.Count; j++) {
									originalUVs [vertexOffset + j] = uvs [j];
								}
							}
						}
						if (updatePreviewTree) {
							MeshFilter meshFilter = tree.obj.GetComponent<MeshFilter> ();
							meshFilter.sharedMesh.SetUVs (0, originalUVs);
						}
					}
				}
			}
		}
		/// <summary>
		/// Takes the sprout mesh and assign values to the color channel to be used for rendering by shaders.
		/// 
		/// Sets the following values:
		/// Red:	value from min and max color shader (ST8 uses red to set AO).
		/// Green:	random value from 0 to 1 (used for SproutLab to set tint).
		/// Blue:	random value from 0 to 1 (used for SproutLab to set saturation).
		/// Alpha:	value of 0 to dissolve a sprout.
		/// 
		/// Reads UV6 (ch. 5) to get the sprout branch and hierarchy position.
		/// UV6s:
		/// x: sprout branch position (0 to 1).
		/// y: sprout hierarchy position (0 to 1).
		/// z: packed sprout direction (16, 1, 1/16).
		/// w: wind pattern.
		/// <param name="treeFactory"></param>
		/// <param name="sproutMap"></param>
		private void AssignColorVariance (TreeFactory treeFactory, SproutMap sproutMap) {
			Dictionary<int, MeshManager.MeshData> meshDatas = 
				treeFactory.meshManager.GetMeshesDataOfType (MeshManager.MeshData.Type.Sprout, sproutMap.groupId);
			var meshDatasEnumerator = meshDatas.GetEnumerator ();
			int sproutMeshId;
			float normalizedShade = 0f;
			float normalizedDissolve = 0f;
			Mesh mesh;
			Color meshColor = Color.green;
			while (meshDatasEnumerator.MoveNext ()) {
				sproutMeshId = meshDatasEnumerator.Current.Key;
				mesh = treeFactory.meshManager.GetMesh (sproutMeshId);
				if (treeFactory.meshManager.GetMesh (sproutMeshId) != null) {
					List<Color> localColors = new List<Color> ();
					List<Vector4> localUV2s = new List<Vector4> ();
					List<Vector2> localUV6s = new List<Vector2> ();
					mesh.GetUVs (1, localUV2s);
					mesh.GetUVs (5, localUV6s);
					float currentSproutRand = -1f;
					float sproutRandShade = 0f;
					float sproutRandDissolve = 0f;
					float randG = 0f;
					float randB = 0f;
					for (int i = 0; i < localUV2s.Count; i++) {
						if (currentSproutRand != localUV2s[i].z) {
							currentSproutRand = localUV2s[i].z;

							// SHADE
							if (sproutMap.colorVarianceMode == SproutMap.ColorVarianceMode.Shades) {
								sproutRandShade = currentSproutRand;
								if (sproutMap.shadeMode == SproutMap.ShadeMode.Branch)
									normalizedShade = GetVarianceValue (sproutMap.minColorShade, sproutMap.maxColorShade, sproutRandShade, 
										localUV6s[i].x, sproutMap.shadeVariance, sproutMap.invertShadeMode);
								else if (sproutMap.shadeMode == SproutMap.ShadeMode.Hierarchy)
									normalizedShade = GetVarianceValue (sproutMap.minColorShade, sproutMap.maxColorShade, sproutRandShade, 
										localUV6s[i].y, sproutMap.shadeVariance, sproutMap.invertShadeMode);
								else
									normalizedShade = Mathf.Lerp (sproutMap.minColorShade, sproutMap.maxColorShade, sproutRandShade);
							} else {
								normalizedShade = 1f;
							}

							// DISSOLVE
							if (sproutMap.alphaVarianceMode == SproutMap.AlphaVarianceMode.Dissolve) {
								sproutRandDissolve = currentSproutRand * 100f % 1f;
								if (sproutMap.dissolveMode == SproutMap.DissolveMode.Branch)
									normalizedDissolve = GetVarianceValue (sproutMap.minColorDissolve, sproutMap.maxColorDissolve, sproutRandDissolve, 
										localUV6s[i].x, sproutMap.dissolveVariance, sproutMap.invertDissolveMode);
								else if (sproutMap.dissolveMode == SproutMap.DissolveMode.Hierarchy)
									normalizedDissolve = GetVarianceValue (sproutMap.minColorDissolve, sproutMap.maxColorDissolve, sproutRandDissolve, 
										localUV6s[i].y, sproutMap.dissolveVariance, sproutMap.invertDissolveMode);
								else
									normalizedDissolve = Mathf.Lerp (sproutMap.minColorDissolve, sproutMap.maxColorDissolve, sproutRandDissolve);
							} else {
								normalizedDissolve = 0f;
							}

							randG = Random.Range (0f, 1f);
							randB = Random.Range (0f, 1f);
							meshColor = Color.white;						
							//meshColor = Color.HSVToRGB (normalizedShade, 1f, 1f);
							meshColor.r = normalizedShade;
							meshColor.g = randG;
							meshColor.b = randB;
							meshColor.a = 1f - normalizedDissolve;
						}
						localColors.Add (meshColor);
					}
					mesh.SetColors (localColors); 
				}
			}
		}
		private static float GetVarianceValue (float minValue, float maxValue, float t, float varianceT = 0, float variance = 0, bool inverse = false) {
			float varianceValue;
			if (inverse) {
				varianceValue = Mathf.Lerp (maxValue, minValue, varianceT);
			} else {
				varianceValue = Mathf.Lerp (minValue, maxValue, varianceT);
			}
			if (variance > 0f) {
				float value;
				if (inverse) {
					value = Mathf.Lerp (maxValue, minValue, t);
				} else {
					value = Mathf.Lerp (minValue, maxValue, t);
				}
				varianceValue = Mathf.Lerp (varianceValue, value, variance);
			}
			return varianceValue;
		}
		/// <summary>
		/// Updates the color red channel with an occlusion value from min to max shade for sprouts.
		/// The min and max value is distributed according to the shade mode:
		/// 	Uniform: 	the shade value is distributed randomly on the sprouts.
		/// 	Branch: 	the shade min value is assigned to sprouts at the base of branches and the max value to the top ones.
		/// 	Hierarchy:	the shade min value is assigned to sprouts at the base of the tree and the max value to the ones at the top.
		/// </summary>
		/// <param name="mesh">Mesh to receive the color update.</param>
		/// <param name="minColorShade">Minimum shade value.</param>
		/// <param name="maxColorShade">Maximum shade value.</param>
		/// <param name="sproutShadeMode">Mode to apply the shade value from min to max.</param>
		/// <param name="invertSproutShadeMode">Min and max values are inverted when applied to the sprouts.</param>
		/// <param name="sproutShadeVariance">For Branch and Hierachy modes, how much the shade value assigned should be randomized.</param>
		/// <param name="subMeshIndex">Mesh subindex for the target sprouts.</param>
		public static void UpdateShadeVariance (
			Mesh mesh, 
			float minColorShade, 
			float maxColorShade,
			BranchDescriptorCollection.SproutStyle.SproutShadeMode sproutShadeMode,
			bool invertSproutShadeMode,
			float sproutShadeVariance, 
			int subMeshIndex)
		{
			if (mesh != null && subMeshIndex < mesh.subMeshCount) {
				UnityEngine.Rendering.SubMeshDescriptor smDesc = mesh.GetSubMesh (subMeshIndex);
				List<Color> localColors = new List<Color> ();
				List<Vector4> localUV2s = new List<Vector4> ();
				List<Vector2> localUV6s = new List<Vector2> ();
				mesh.GetColors (localColors);
				mesh.GetUVs (1, localUV2s);
				mesh.GetUVs (5, localUV6s);
				Color shadedColor = Color.white;
				Color baseColor;
				float normalizedShade = 1f;
				float currentSproutRand = -1f;
				for (int i = smDesc.firstVertex; i < smDesc.firstVertex + smDesc.vertexCount; i++) {
					if (currentSproutRand != localColors[i].b) {
						currentSproutRand = localColors[i].b;
						if (sproutShadeMode == BranchDescriptorCollection.SproutStyle.SproutShadeMode.Branch)
							normalizedShade = GetVarianceValue (minColorShade, maxColorShade, currentSproutRand,
								localUV6s[i].x, sproutShadeVariance, invertSproutShadeMode);
						else if (sproutShadeMode == BranchDescriptorCollection.SproutStyle.SproutShadeMode.Hierarchy)
							normalizedShade = GetVarianceValue (minColorShade, maxColorShade, currentSproutRand,
								localUV6s[i].y, sproutShadeVariance, invertSproutShadeMode);
						else
							normalizedShade = Mathf.Lerp (minColorShade, maxColorShade, currentSproutRand);
						//shadedColor = Color.HSVToRGB (normalizedShade, 1f, 1f);
					}
					baseColor = localColors [i];
					//baseColor.r = shadedColor.r;
					baseColor.r = normalizedShade;
					localColors [i] = baseColor;
				}
				mesh.SetColors (localColors);
			}
		}
		/// <summary>
		/// Updates the color red channel with an occlusion value from min to max dissolve for sprouts.
		/// The min and max value is distributed according to the dissolve mode:
		/// 	Uniform: 	the dissolve value is distributed randomly on the sprouts.
		/// 	Branch: 	the dissolve min value is assigned to sprouts at the base of branches and the max value to the top ones.
		/// 	Hierarchy:	the dissolve min value is assigned to sprouts at the base of the tree and the max value to the ones at the top.
		/// </summary>
		/// <param name="mesh">Mesh to receive the color update.</param>
		/// <param name="minColorDissolve">Minimum dissolve value.</param>
		/// <param name="maxColorDissolve">Maximum dissolve value.</param>
		/// <param name="sproutDissolveMode">Mode to apply the dissolve value from min to max.</param>
		/// <param name="invertSproutDissolveMode">Min and max values are inverted when applied to the sprouts.</param>
		/// <param name="sproutDissolveVariance">For Branch and Hierachy modes, how much the dissolve value assigned should be randomized.</param>
		/// <param name="subMeshIndex">Mesh subindex for the target sprouts.</param>
		public static void UpdateDissolveVariance (
			Mesh mesh, 
			float minColorDissolve, 
			float maxColorDissolve,
			BranchDescriptorCollection.SproutStyle.SproutDissolveMode sproutDissolveMode,
			bool invertSproutDissolveMode,
			float sproutDissolveVariance, 
			int subMeshIndex)
		{
			if (mesh != null && subMeshIndex < mesh.subMeshCount) {
				UnityEngine.Rendering.SubMeshDescriptor smDesc = mesh.GetSubMesh (subMeshIndex);
				List<Color> localColors = new List<Color> ();
				List<Vector4> localUV2s = new List<Vector4> ();
				List<Vector2> localUV6s = new List<Vector2> ();
				mesh.GetColors (localColors);
				mesh.GetUVs (1, localUV2s);
				mesh.GetUVs (5, localUV6s);
				Color dissolvedColor = Color.white;
				Color baseColor;
				float normalizedDissolve = 0f;
				float currentSproutRand = -1f;
				float sproutRandDissolve = 0f;
				for (int i = smDesc.firstVertex; i < smDesc.firstVertex + smDesc.vertexCount; i++) {
					if (currentSproutRand != localColors[i].b) {
						currentSproutRand = localColors[i].b;
						sproutRandDissolve = currentSproutRand * 100f % 1f;
						if (sproutDissolveMode == BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Branch)
							normalizedDissolve = GetVarianceValue (minColorDissolve, maxColorDissolve, sproutRandDissolve,
								localUV6s[i].x, sproutDissolveVariance, invertSproutDissolveMode);
						else if (sproutDissolveMode == BranchDescriptorCollection.SproutStyle.SproutDissolveMode.Hierarchy)
							normalizedDissolve = GetVarianceValue (minColorDissolve, maxColorDissolve, sproutRandDissolve,
								localUV6s[i].y, sproutDissolveVariance, invertSproutDissolveMode);
						else
							normalizedDissolve = Mathf.Lerp (minColorDissolve, maxColorDissolve, sproutRandDissolve);
						//dissolvedColor = Color.HSVToRGB (normalizedDissolve, 1f, 1f);
					}
					baseColor = localColors [i];
					//baseColor.r = dissolvedColor.r;
					baseColor.a = 1f - normalizedDissolve;
					localColors [i] = baseColor;
				}
				mesh.SetColors (localColors);
			}
		}
		#endregion
	}
}