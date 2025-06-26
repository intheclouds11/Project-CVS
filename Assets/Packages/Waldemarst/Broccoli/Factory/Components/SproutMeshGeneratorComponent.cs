using System.Collections.Generic;

using UnityEngine;

using Broccoli.Base;
using Broccoli.Model;
using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.Factory;

namespace Broccoli.Component
{
	/// <summary>
	/// Sprout mesh generator component.
	/// </summary>
	public class SproutMeshGeneratorComponent : TreeFactoryComponent {
		#region Vars
		/// <summary>
		/// The sprout mesh builder.
		/// </summary>
		SproutMeshBuilder sproutMeshBuilder = null;
		/// <summary>
		/// Advanced sprout mesh builder.
		/// </summary>
		AdvancedSproutMeshBuilder advancedSproutMeshBuilder = null;
		/// <summary>
		/// The sprout mesh generator element.
		/// </summary>
		SproutMeshGeneratorElement sproutMeshGeneratorElement = null;
		/// <summary>
		/// The sprout meshes relationship between their group id and the assigned sprout mesh.
		/// </summary>
		Dictionary<int, SproutMesh> sproutMeshes = new Dictionary <int, SproutMesh> ();
		/// <summary>
		/// The sprout mappers.
		/// </summary>
		Dictionary<int, SproutMap> sproutMappers = new Dictionary <int, SproutMap> ();
		/// <summary>
		/// Flag to reduce the complexity of sprouts for LOD purposes.
		/// </summary>
		bool simplifySprouts = false;
		/// <summary>
		/// Saves scaling relative to a map area on an atlas texture.
		/// </summary>
		/// <typeparam name="int">Sprout group multiplied by 10000 plus the index of the map area.</typeparam>
		/// <typeparam name="float">Scaling.</typeparam>
		Dictionary<int, float> _mapAreaToScale = new Dictionary<int, float> ();
		/// <summary>
		/// Relationship between BDC processed hashes and their timestamps.
		/// </summary>
		/// <typeparam name="int">BranchDescriptorCollection hash.</typeparam>
		/// <typeparam name="int">Timestamp of the BranchDescriptorCollection.</typeparam>
		Dictionary<int, int> _registeredBranchDescriptorCollections = new Dictionary<int, int> ();
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
			sproutMeshBuilder = SproutMeshBuilder.GetInstance ();
			advancedSproutMeshBuilder = AdvancedSproutMeshBuilder.GetInstance ();
			sproutMeshBuilder.onBeforeAssignSproutSubgroups = null;
			sproutMeshBuilder.onBeforeAssignSproutSubgroups += OnBeforeAssignSproutSubgroups;
			advancedSproutMeshBuilder.onBeforeAssignSproutSubgroups = null;
			advancedSproutMeshBuilder.onBeforeAssignSproutSubgroups += OnBeforeAssignSproutSubgroups;

			// Gather all SproutMap objects from elements downstream.
			PipelineElement pipelineElement = 
				sproutMeshGeneratorElement.GetDownstreamElement (PipelineElement.ClassType.SproutMapper);
			sproutMappers.Clear ();
			if (pipelineElement != null && pipelineElement.isActive) {
				SproutMapperElement sproutMapperElement = (SproutMapperElement)pipelineElement;
				for (int i = 0; i < sproutMapperElement.sproutMaps.Count; i++) {
					if (sproutMapperElement.sproutMaps[i].groupId > 0) {
						sproutMappers.Add (sproutMapperElement.sproutMaps[i].groupId, sproutMapperElement.sproutMaps[i]);
					}
				}
			}

			// Prepare tree sprouts.
			tree.SetHelperSproutIds ();

			// Gather all SproutMesh objects from element.
			sproutMeshes.Clear ();
			for (int i = 0; i < sproutMeshGeneratorElement.sproutMeshes.Count; i++) {
				sproutMeshes.Add (sproutMeshGeneratorElement.sproutMeshes[i].groupId, sproutMeshGeneratorElement.sproutMeshes[i]);
			}

			sproutMeshBuilder.globalScale = treeFactory.treeFactoryPreferences.factoryScale;
			sproutMeshBuilder.SetGravity (GlobalSettings.gravityDirection);
			sproutMeshBuilder.mapST = true;

			advancedSproutMeshBuilder.globalScale = treeFactory.treeFactoryPreferences.factoryScale;
		}
		/// <summary>
		/// Gets the changed aspects on the tree for this component.
		/// </summary>
		/// <returns>The changed aspects.</returns>
		public override int GetChangedAspects () {
			return (int)TreeFactoryProcessControl.ChangedAspect.StructureGirth; // TODO
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public override void Clear ()
		{
			base.Clear ();
			if (sproutMeshBuilder != null)
				sproutMeshBuilder.Clear ();
			sproutMeshBuilder = null;
			/*
			if (advancedSproutMeshBuilder != null)
				advancedSproutMeshBuilder.Clear ();
			advancedSproutMeshBuilder = null;
			*/
			sproutMeshGeneratorElement = null;
			sproutMeshes.Clear ();
			sproutMappers.Clear ();
			_mapAreaToScale.Clear ();
			//_registeredBranchDescriptorCollections.Clear ();
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
			if (pipelineElement != null && tree != null) {
				sproutMeshGeneratorElement = pipelineElement as SproutMeshGeneratorElement;
				PrepareParams (treeFactory, useCache, useLocalCache, processControl);
				BuildMesh (treeFactory, processControl.lodIndex);
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
			if (sproutMeshGeneratorElement != null) {
				sproutMeshGeneratorElement.verticesCount = 0;
				sproutMeshGeneratorElement.trianglesCount = 0;
			}
		}
		/// <summary>
		/// Builds the mesh or meshes for the sprouts.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="lodIndex">Index for the LOD definition.</param>
		private void BuildMesh (TreeFactory treeFactory, int lodIndex) {
			var sproutMeshesEnumerator = sproutMeshes.GetEnumerator ();
			PrepareBuilder (sproutMeshes, sproutMappers, lodIndex);
			sproutMeshBuilder.PrepareBuilder (sproutMeshes, sproutMappers, lodIndex);
			SproutMesh sproutMesh;
			Mesh groupMesh = null;
			int verticesCount = 0;
			int trisCount = 0;

			// For each SproutMesh generate a Mesh.
			while (sproutMeshesEnumerator.MoveNext ()) {
				sproutMesh = sproutMeshesEnumerator.Current.Value;
				// SHAPE MODE.
				if (sproutMesh.meshingMode == SproutMesh.MeshingMode.Shape) {
					groupMesh = BuildAdvancedShapeMesh (treeFactory, lodIndex, sproutMesh);
				} 
				// BRANCH COLLECTION MODE.
				else if (sproutMesh.meshingMode == SproutMesh.MeshingMode.BranchCollection) {
					// Normalize required LOD to SproutCollection LOD.
					lodIndex = NormalizeToBranchCollectionLOD (treeFactory, lodIndex);
					groupMesh = BuildBranchCollectionMesh (treeFactory, lodIndex, sproutMesh);
				}
				if (groupMesh != null) {
					verticesCount += groupMesh.vertexCount;
					trisCount += (int)(groupMesh.triangles.Length / 3);
				}
			}
			sproutMeshGeneratorElement.verticesCount = verticesCount;
			sproutMeshGeneratorElement.trianglesCount = trisCount;
		}
		private int NormalizeToBranchCollectionLOD (TreeFactory treeFactory, int lodIndex) {
			LODDef lodDef = treeFactory.treeFactoryPreferences.GetLOD (lodIndex);
			int normalizedLod = Mathf.RoundToInt (Mathf.Lerp (2f, 0f, lodDef.sproutResolution));
			//Debug.Log ($"Normalizing TreeFactory LOD index from {lodIndex} to sprout resolution of {normalizedLod}");
			return normalizedLod;
		}
		/// <summary>
		/// Analyzes the SproutMesh and SproutMap instances in preparation to build meshes.
		/// Takes the size of SproutMap areas to assign a scale to each one.
		/// </summary>
		/// <param name="sproutMeshes">Relationship between Sprout Group Id and SproutMesh instance.</param>
		/// <param name="sproutMappers">Relationship between Sprout Group Id and SproutMap instance if present.</param>
		public void PrepareBuilder (Dictionary<int, SproutMesh> sproutMeshes, Dictionary<int, SproutMap> sproutMappers, int lodIndex) {
			// Clean sprout mesh to atlas dictionary
			// Iterate through all areas.
			// Save the scalings.
			var sproutMappersEnumerator = sproutMappers.GetEnumerator ();
			int groupId;
			SproutMap sproutMap;
			SproutMap.SproutMapArea sproutArea;
			List<float> areaDiagonals = new List<float> ();
			float maxDiagonal = -1f;
			float diagonal;
			_mapAreaToScale.Clear ();
			while (sproutMappersEnumerator.MoveNext ()) {
				groupId = sproutMappersEnumerator.Current.Key;
				sproutMap = sproutMappersEnumerator.Current.Value;
				areaDiagonals.Clear ();
				maxDiagonal = -1f;
				for (int i = 0; i < sproutMap.sproutAreas.Count; i++) {
					sproutArea = sproutMap.sproutAreas [i];
					if (sproutArea.enabled && sproutArea.texture != null) {
						diagonal = sproutArea.diagonal;
						if (diagonal > maxDiagonal) {
							maxDiagonal = diagonal;
						}
					} else {
						diagonal = 0f;
					}
					areaDiagonals.Add (diagonal);
				}
				for (int i = 0; i < areaDiagonals.Count; i++) {
					int meshId = advancedSproutMeshBuilder.GetGroupSubgroupId (0, groupId, i, lodIndex);
					_mapAreaToScale.Add (meshId, areaDiagonals [i] / maxDiagonal);
				}
			}
		}
		#endregion

		#region Process Shape Mesh
		/// <summary>
		/// Builds the mesh or meshes for the sprouts.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="lodIndex">Index for the LOD definition.</param>
		private Mesh BuildAdvancedShapeMesh (TreeFactory treeFactory, int lodIndex, SproutMesh sproutMesh) {
			int groupId = sproutMesh.groupId;

			// Validation... the Sprout Group does not exist.
			if (!pipelineElement.pipeline.sproutGroups.HasSproutGroup (groupId)) return null;

			// Preparing Mesh to return.
			Mesh groupMesh = null;
			// Shape Meshes having mappers (planes, grid).
			if (sproutMappers.ContainsKey (groupId) && sproutMeshes[groupId].shapeMode != SproutMesh.ShapeMode.Mesh) {
				if (sproutMappers [groupId].IsTextured ()) {
					// Assign Sprouts to their subgroup.
					advancedSproutMeshBuilder.AssignSproutSubgroups (tree, groupId, sproutMappers [groupId], sproutMesh);

					// For each sprout area:
					List<SproutMap.SproutMapArea> sproutAreas = sproutMappers [groupId].sproutAreas;
					for (int i = 0; i < sproutAreas.Count; i++) {
						if (sproutAreas[i].enabled) {
							// Create the mesh for the Sprout Group and Subgroup (area).
							/*
							groupMesh = sproutMeshBuilder.MeshSprouts (tree, 
								groupId, TranslateSproutMesh (sproutMeshes [groupId]), sproutAreas[i], i, false);
								*/
							advancedSproutMeshBuilder.SetMapArea (sproutAreas[i]);
							int meshId = advancedSproutMeshBuilder.GetGroupSubgroupId (sproutMesh.meshType, groupId, i, lodIndex);
							Mesh baseMesh = GetBaseMesh (treeFactory, sproutMesh, sproutAreas[i], meshId, lodIndex);
							advancedSproutMeshBuilder.RegisterMesh (baseMesh, sproutMesh.meshType, groupId, i);
							groupMesh = advancedSproutMeshBuilder.MeshSprouts (tree, sproutMesh, groupId, i);

							// Customize the normals. TODO: jobify.
							ApplyNormalMode (groupMesh, Vector3.zero);

							// Register the created Mesh on the MeshManager.
							treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId, i);

							// Removing SproutMeshData, now all data should be available at the Mesh data.
							/*
							List<SproutMeshBuilder.SproutMeshData> sproutMeshDatas = sproutMeshBuilder.sproutMeshData;
							for (int j = 0; j < sproutMeshDatas.Count; j++) {
								MeshManager.MeshPart meshPart = treeFactory.meshManager.AddMeshPart (sproutMeshDatas[j].startIndex, 
																	sproutMeshDatas[j].length,
																	sproutMeshDatas[j].position, 
																	0, 
																	sproutMeshDatas[j].origin,
																	MeshManager.MeshData.Type.Sprout,
																	groupId,
																	i);
								meshPart.sproutId = sproutMeshDatas[j].sproutId;
								meshPart.branchId = sproutMeshDatas[j].branchId;
							}
							*/
						} else {
							// Deregister a Mesh if its subgroup is disabled.
							treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Sprout, groupId, i);
						}
					}
				} else {
					int meshId = advancedSproutMeshBuilder.GetGroupSubgroupId (sproutMesh.meshType, groupId, lodIndex);
					// Create the mesh for the Sprout group.
					Mesh baseMesh = GetBaseMesh (treeFactory, sproutMesh, null, meshId, lodIndex);
					advancedSproutMeshBuilder.RegisterMesh (baseMesh, sproutMesh.meshType, groupId);
					groupMesh = advancedSproutMeshBuilder.MeshSprouts (tree, sproutMesh, groupId);

					// Customize the normals, TODO: jobify.
					ApplyNormalMode (groupMesh, Vector3.zero);
					
					// Register the created Mesh on the MeshManager.
					treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
				}
			}

			// Shape Meshes having no mappers (unassigned or custom Mesh based).
			else if (sproutMeshes[groupId].meshGameObject != null) {
				// Process without sprout areas.
				int meshId = advancedSproutMeshBuilder.GetGroupSubgroupId (sproutMesh.meshType, groupId, 0, lodIndex);
				// Create the mesh for the Sprout group.
				Mesh baseMesh = GetBaseMesh (treeFactory, sproutMesh, null, meshId, lodIndex);
				advancedSproutMeshBuilder.RegisterMesh (baseMesh, sproutMesh.meshType, groupId);
				groupMesh = advancedSproutMeshBuilder.MeshSprouts (tree, sproutMesh, groupId);

				// Customize the normals, TODO: jobify.
				ApplyNormalMode (groupMesh, Vector3.zero);
				
				// Register the created Mesh on the MeshManager.
				treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
				// TODOOOOO
				/*
				groupMesh = sproutMeshBuilder.MeshSprouts (tree, groupId, sproutMeshes [groupId]);
				ApplyNormalMode (groupMesh, Vector3.zero);
				treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Sprout, groupId);
				treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
				*/
			}

			return groupMesh;
		}
		/// <summary>
		/// Scales and rotates a mesh taking its zero coordinates as pivot point.
		/// </summary>
		/// <param name="mesh">Mesh.</param>
		/// <param name="scale">Scale.</param>
		/// <param name="rotation">Rotation.</param>
		void ScaleRotateMesh (Mesh mesh, Vector3 scale, Vector3 rotation) {
			if (mesh != null) {
				Vector3[] originalVertices = mesh.vertices;
				Vector3[] originalNormals = mesh.normals;
				for (int i = 0; i < originalVertices.Length; i++) {
					originalVertices [i].x *= scale.x;
					originalVertices [i].y *= scale.y;
					originalVertices [i].z *= scale.z;
					originalVertices [i] = Quaternion.Euler (rotation) * originalVertices [i];
					originalNormals [i] = Quaternion.Euler (rotation) * originalNormals [i];
				}
				mesh.vertices = originalVertices;
				mesh.normals = originalNormals;
			}
		}
		/// <summary>
		/// Get the base mesh for a given SproutMesh.
		/// </summary>
		/// <param name="sproutMesh">SproutMesh instance.</param>
		/// <param name="sproutArea">SproutArea instance with information to map UVs.</param>
		/// <param name="meshId">Mesh identifier.</param>
		/// <param name="lodIndex"></param>
		/// <returns></returns>
		private Mesh GetBaseMesh (TreeFactory treeFactory, SproutMesh sproutMesh, SproutMap.SproutMapArea sproutArea = null, int meshId = 0, int lodIndex = 0)
		{
			// Prepare params for plane.
			float width = sproutMesh.width;
			float height = sproutMesh.height;
			float pivotW = sproutMesh.pivotX;
			float pivotH = sproutMesh.pivotY;
			float uvX = 0f;
			float uvY = 0f;
			float uvWidth = 1f;
			float uvHeight = 1f;
			int uvDir = 0;
			if (sproutArea != null && 
				sproutArea.enabled &&
				sproutArea.width > 0 && 
				sproutArea.texture != null) {
				if (sproutMesh.overrideHeightWithTexture)
					height = sproutMesh.width * sproutArea.normalizedHeightPx / (float)sproutArea.normalizedWidthPx;
				if (sproutMesh.includeScaleFromAtlas) {
					width *= _mapAreaToScale [meshId];
					height *= _mapAreaToScale [meshId];
				}
				sproutMesh.overridedHeight = height;
			}
			if (sproutArea != null) {
				pivotW = sproutArea.normalizedPivotX;
				pivotH = sproutArea.normalizedPivotY;
				uvX = sproutArea.x;
				uvY = sproutArea.y;
				uvWidth = sproutArea.width;
				uvHeight = sproutArea.height;
				uvDir = sproutArea.normalizedStep;
			}

			// Build mesh.
			Mesh baseMesh = null;
			LODDef lodDef = treeFactory.treeFactoryPreferences.GetLOD (lodIndex);
			switch (sproutMesh.shapeMode) {
				case SproutMesh.ShapeMode.PlaneX:
					PlaneXSproutMeshBuilder.SetUVData (uvX, uvY, uvWidth, uvHeight, uvDir);
					PlaneXSproutMeshBuilder.SetIdData (meshId);
					baseMesh = PlaneXSproutMeshBuilder.GetPlaneXMesh (width, height, pivotW, pivotH, sproutMesh.depth);
					break;
				case SproutMesh.ShapeMode.GridPlane:
					GridSproutMeshBuilder.SetUVData (uvX, uvY, uvWidth, uvHeight, uvDir);
					GridSproutMeshBuilder.SetIdData (meshId);
					int resolutionWidth = Mathf.RoundToInt (Mathf.Lerp (2f, (float)sproutMesh.resolutionWidth, lodDef.sproutResolution));
					int resolutionHeight = Mathf.RoundToInt (Mathf.Lerp (2f, (float)sproutMesh.resolutionHeight, lodDef.sproutResolution));
					baseMesh = GridSproutMeshBuilder.GetGridMesh (
						width, height, resolutionWidth, resolutionHeight, pivotW, pivotH);
					break;
				case SproutMesh.ShapeMode.Mesh:
					MeshSproutMeshBuilder.SetIdData (meshId);
					if (sproutMesh.processedMesh != null) {
						Object.DestroyImmediate (sproutMesh.processedMesh);
					}
					if (sproutMesh.meshGameObject != null) {
						MeshFilter[] meshFilters;
						if (sproutMesh.useMultiLOD) {
							int lodObjIndex = Mathf.RoundToInt (Mathf.Lerp (2f, 0f, lodDef.sproutResolution));
							if (lodObjIndex == 2) meshFilters = sproutMesh.meshGameObjectLOD2.GetComponentsInChildren<MeshFilter> ();
							else if (lodObjIndex == 1) meshFilters = sproutMesh.meshGameObjectLOD1.GetComponentsInChildren<MeshFilter> ();
							else meshFilters = sproutMesh.meshGameObject.GetComponentsInChildren<MeshFilter> ();
						} else {
							meshFilters = sproutMesh.meshGameObject.GetComponentsInChildren<MeshFilter> ();
						}
						if (meshFilters.Length > 0 && meshFilters [0] != null) {
							sproutMesh.processedMesh =  Object.Instantiate (meshFilters [0].sharedMesh);



















							// Get the transformation matrix from the SOURCE object
							Transform sourceTransform = meshFilters[0].transform; // Cache transform
							Matrix4x4 matrix = sourceTransform.localToWorldMatrix;
							Matrix4x4 inverseTranspose = matrix.inverse.transpose;
							bool matrixIsValidForNormals = matrix.ValidTRS();

							// Get mesh data arrays from the copy
							Vector3[] vertices = sproutMesh.processedMesh.vertices;
							Vector3[] normals = sproutMesh.processedMesh.normals;
							Vector4[] tangents = sproutMesh.processedMesh.tangents;

							bool hasNormals = normals != null && normals.Length == vertices.Length;
							bool hasTangents = tangents != null && tangents.Length == vertices.Length;

							// Transform Vertices
							for (int i = 0; i < vertices.Length; i++)
							{
								vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
							}
							sproutMesh.processedMesh.vertices = vertices; // Assign transformed vertices first

							// Transform Normals (Use Inverse Transpose)
							if (hasNormals)
							{
								if (matrixIsValidForNormals) {
									for (int i = 0; i < normals.Length; i++) {
										normals[i] = inverseTranspose.MultiplyVector(normals[i]).normalized;
									}
								} else {
									Quaternion rotationOnly = sourceTransform.rotation;
									for (int i = 0; i < normals.Length; i++) { normals[i] = (rotationOnly * normals[i]).normalized; }
									//Debug.LogWarning($"Source transform matrix on {sourceGO.name} was invalid. Only rotation applied to normals.", sourceGO);
								}
								sproutMesh.processedMesh.normals = normals; // Assign potentially transformed normals
							}

							// Transform Tangents (Use Inverse Transpose)
							if (hasTangents)
							{
								if (matrixIsValidForNormals) {
									for (int i = 0; i < tangents.Length; i++) {
										Vector3 tanXYZ = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
										float w = tangents[i].w;
										tanXYZ = inverseTranspose.MultiplyVector(tanXYZ).normalized;
										tangents[i] = new Vector4(tanXYZ.x, tanXYZ.y, tanXYZ.z, w);
									}
								} else {
									Quaternion rotationOnly = sourceTransform.rotation;
									for (int i = 0; i < tangents.Length; i++) {
										Vector3 tanXYZ = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
										float w = tangents[i].w;
										tanXYZ = (rotationOnly * tanXYZ).normalized;
										tangents[i] = new Vector4(tanXYZ.x, tanXYZ.y, tanXYZ.z, w);
									}
									//Debug.LogWarning($"Source transform matrix on {sourceGO.name} was invalid. Only rotation applied to tangents.", sourceGO);
								}
								sproutMesh.processedMesh.tangents = tangents; // Assign potentially transformed tangents
							}

							// --- 5. Correct for Negative Scale ---
							Vector3 scale = sourceTransform.lossyScale;
							bool hasNegativeScale = (scale.x * scale.y * scale.z) < 0f;

							if (hasNegativeScale)
							{
								//Debug.LogWarning($"Detected negative scale on source {sourceGO.name}. Flipping normals, tangents W, and triangle winding order on mesh copy.", sourceGO);

								// 1. Flip Normals Array
								if (hasNormals) {
									/*
									Vector3[] currentNormals = sproutMesh.processedMesh.normals; // Get potentially transformed normals
									for (int i = 0; i < currentNormals.Length; i++) { currentNormals[i] = -currentNormals[i]; }
									sproutMesh.processedMesh.normals = currentNormals;
									*/
								}

								// 2. Flip Tangent Winding (W component)
								if (hasTangents) {
									/*
									Vector4[] currentTangents = sproutMesh.processedMesh.tangents; // Get potentially transformed tangents
									for (int i = 0; i < currentTangents.Length; i++) { currentTangents[i].w *= -1f; }
									sproutMesh.processedMesh.tangents = currentTangents;
									*/
								}

								// 3. Flip Triangle Winding Order
								int[] triangles = sproutMesh.processedMesh.triangles;
								for (int submesh = 0; submesh < sproutMesh.processedMesh.subMeshCount; submesh++)
								{
									// Process each submesh separately
									triangles = sproutMesh.processedMesh.GetTriangles(submesh);
									for (int i = 0; i < triangles.Length; i += 3)
									{
										int temp = triangles[i];
										triangles[i] = triangles[i + 1];
										triangles[i + 1] = temp;
									}
									sproutMesh.processedMesh.SetTriangles(triangles, submesh); // Assign back to the correct submesh
								}
							}
							// --- End Negative Scale Correction ---

							// 6. Recalculate bounds
							sproutMesh.processedMesh.RecalculateBounds();























							ScaleRotateMesh (sproutMesh.processedMesh, sproutMesh.meshScale, sproutMesh.meshRotation);
						}
					}
					baseMesh = MeshSproutMeshBuilder.GetMesh (sproutMesh.processedMesh, Vector3.one, Vector3.zero, Quaternion.identity);
					break;
				default:
					int planes = 1;
					if (sproutMesh.shapeMode == SproutMesh.ShapeMode.Cross) {
						planes = 2;
					} else if (sproutMesh.shapeMode == SproutMesh.ShapeMode.Tricross) {
						planes = 3;
					}
					if (sproutMesh.shapeMode == SproutMesh.ShapeMode.Cross) {
						GridSproutMeshBuilder.SetUVData (uvX, uvY, uvWidth, uvHeight, uvDir);
						GridSproutMeshBuilder.SetIdData (meshId);
						baseMesh = GridSproutMeshBuilder.GetGridMesh (
							width, height, sproutMesh.resolutionWidth, sproutMesh.resolutionHeight, pivotW, pivotH, 2);
					} else {
						PlaneSproutMeshBuilder.SetUVData (uvX, uvY, uvWidth, uvHeight, uvDir);
						PlaneSproutMeshBuilder.SetIdData (meshId);
						baseMesh = PlaneSproutMeshBuilder.GetPlaneMesh (
							width, height, pivotW, pivotH, planes);
					}
					break;
			}
			return baseMesh;
		}
		/// <summary>
		/// Reprocess normals for the sprout mesh.
		/// </summary>
		/// <param name="targetMesh">Target sprout mesh.</param>
		/// <param name="offset">Vector3 offset from the normal reference point (depending on the normal mode applied).</param>
		void ApplyNormalMode (Mesh targetMesh, Vector3 offset) {
			// PER SPROUT (Unchanged).
			if (sproutMeshGeneratorElement.normalMode == SproutMeshGeneratorElement.NormalMode.PerSprout) return;

			Vector3 referenceCenter = targetMesh.bounds.center;
			// TREE ORIGIN.
			if (sproutMeshGeneratorElement.normalMode == SproutMeshGeneratorElement.NormalMode.TreeOrigin) {
				referenceCenter.y = 0;
			} 
			// SPROUT BASE/CENTER, get sprouts bounds.
			else {
				Bounds sproutsBounds = GetSproutsBounds (targetMesh);
				referenceCenter = sproutsBounds.center;
				//SPROUT BASE
				if (sproutMeshGeneratorElement.normalMode == SproutMeshGeneratorElement.NormalMode.SproutsBase) {
					referenceCenter.y -= sproutsBounds.min.y;
				}
			}
			List<Vector3> normals = new List<Vector3> ();
			List<Vector3> vertices = new List<Vector3> ();
			targetMesh.GetNormals (normals);
			targetMesh.GetVertices (vertices);
			for (int i = 0; i < normals.Count; i++) {
				normals [i] = Vector3.Lerp (normals[i], (vertices[i] - referenceCenter + offset).normalized, sproutMeshGeneratorElement.normalModeStrength);
			}
			targetMesh.SetNormals (normals);
		}
		Bounds GetSproutsBounds (Mesh targetMesh) {
			Bounds sproutBounds = new Bounds ();
			if (targetMesh != null && targetMesh.vertexCount > 0 && targetMesh.subMeshCount > 0) {
				UnityEngine.Rendering.SubMeshDescriptor smd = targetMesh.GetSubMesh (0);
				int sproutVertexStart = smd.firstVertex;
				int vertexCount = targetMesh.vertexCount;
				Vector3[] vertices = targetMesh.vertices;
				for (int i = sproutVertexStart; i < vertexCount; i++) {
					sproutBounds.Encapsulate (vertices [i]);
				}
			}
			return sproutBounds;
		}
		/// <summary>
		/// Simplifies sprout mesh parameters for LOD purposes.
		/// </summary>
		/// <param name="sproutMesh">SproutMesh to evaluate.</param>
		/// <returns>Translated SproutMesh.</returns>
		SproutMesh TranslateSproutMesh (SproutMesh sproutMesh) {
			if (simplifySprouts) {
				if (sproutMesh.shapeMode == SproutMesh.ShapeMode.GridPlane) {
					SproutMesh simplyfiedSproutMesh = sproutMesh.Clone ();
					if (sproutMesh.resolutionHeight > sproutMesh.resolutionWidth) {
						simplyfiedSproutMesh.resolutionWidth = 1;
						simplyfiedSproutMesh.resolutionHeight = 
						(int) Mathf.Clamp ( (float) simplyfiedSproutMesh.resolutionHeight / 2f,
							2.0f, 
							(float) simplyfiedSproutMesh.resolutionHeight);
					} else if (sproutMesh.resolutionWidth > sproutMesh.resolutionHeight) {
						simplyfiedSproutMesh.resolutionHeight = 1;
						simplyfiedSproutMesh.resolutionWidth = 
						(int) Mathf.Clamp ( (float) simplyfiedSproutMesh.resolutionWidth / 2f,
							2.0f, 
							(float) simplyfiedSproutMesh.resolutionWidth);
					} else {
						simplyfiedSproutMesh.resolutionHeight = 
						(int) Mathf.Clamp ( (float) simplyfiedSproutMesh.resolutionHeight / 2f,
							2.0f, 
							(float) simplyfiedSproutMesh.resolutionHeight);
						simplyfiedSproutMesh.resolutionWidth = 
						(int) Mathf.Clamp ( (float) simplyfiedSproutMesh.resolutionWidth / 2f,
							2.0f, 
							(float) simplyfiedSproutMesh.resolutionWidth);
					}
					return simplyfiedSproutMesh;
				} else if (sproutMesh.shapeMode == SproutMesh.ShapeMode.PlaneX) {
					
				}
			}
			return sproutMesh;
		}
		#endregion

		#region Process Branch Collection Mesh
		/// <summary>
		/// Builds the mesh or meshes for the sprouts.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="lodIndex">Index for the LOD definition.</param>
		private Mesh BuildBranchCollectionMesh (TreeFactory treeFactory, int lodIndex, SproutMesh sproutMesh) {
			Mesh groupMesh = null;
			int groupId = sproutMesh.groupId;
			sproutMeshGeneratorElement.ResetFixedSeed ();
			bool isTwoSided = treeFactory.materialManager.IsSproutTwoSided ();
			if (pipelineElement.pipeline.sproutGroups.HasSproutGroup (groupId) && sproutMesh.branchCollection != null) {
				// Get the branch collection.
				BranchDescriptorCollection branchCollection = ((BranchDescriptorCollectionSO)sproutMesh.branchCollection).branchDescriptorCollection;

				// Assign the sprout subgroups.
				sproutMeshBuilder.AssignSproutSubgroups (tree, groupId, branchCollection, sproutMesh);

				// Register the branch collection.
				RegisterBranchCollection (treeFactory, sproutMesh, branchCollection);

				// Generate a mesh for each snapshot.
				treeFactory.meshManager.DeregisterSproutGroupMeshes (groupId);
				if (sproutMesh.subgroups.Length == 0) {
					groupMesh = advancedSproutMeshBuilder.MeshSprouts (tree, sproutMesh, groupId, -1, lodIndex);
					ApplyNormalMode (groupMesh, Vector3.zero);
					treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
				} else {
					groupMesh = new Mesh ();
					CombineInstance[] combine = new CombineInstance [sproutMesh.subgroups.Length];
					for (int i = 0; i < sproutMesh.subgroups.Length; i++) {
						combine [i].mesh = advancedSproutMeshBuilder.MeshSprouts (tree, sproutMesh, groupId, sproutMesh.subgroups [i], lodIndex);
						combine [i].transform = Matrix4x4.identity;
						combine [i].subMeshIndex = 0;
						ApplyNormalMode (combine [i].mesh, Vector3.zero);
					}
					groupMesh.CombineMeshes (combine, true, false);
					treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
				}
			}
			return groupMesh;
		}
		/// <summary>
		/// Register all the meshes (snapshots or variations and their LODs) of a BranchDescriptorCollection.
		/// It checks the hash and timestamp of the collection to reprocess the collection conditionally.
		/// </summary>
		/// <param name="treeFactory"></param>
		/// <param name="sproutMesh"></param>
		/// <param name="branchDescriptorCollection"></param>
		private void RegisterBranchCollection (
			TreeFactory treeFactory,
			SproutMesh sproutMesh, 
			BranchDescriptorCollection branchDescriptorCollection)
		{
			// Check if the collection needs to register its meshes.
			bool shouldProcessCollection = false;
			int bdcHash = branchDescriptorCollection.GetHashCode ();
			if (_registeredBranchDescriptorCollections.ContainsKey (bdcHash)) {
				if (_registeredBranchDescriptorCollections[bdcHash] != branchDescriptorCollection.timestamp) 
					shouldProcessCollection = true;
			} else {
				shouldProcessCollection = true;
			}

			// Conditionally register BDC meshes.
			shouldProcessCollection = true;
			if (shouldProcessCollection) {
				// Prepare to build the meshes.
				BranchCollectionSproutMeshBuilder.PrepareMeshBuilding ();

				// Add SNAPSHOT meshes.
				if (branchDescriptorCollection.descriptorImplId == BranchDescriptorCollection.SNAPSHOT_COLLECTION
					|| branchDescriptorCollection.descriptorImplId == BranchDescriptorCollection.BASE_COLLECTION)
				{
					// Register each Snapshot/lod mesh.
					for (int snapI = 0; snapI < branchDescriptorCollection.snapshots.Count; snapI++) {
						BranchDescriptor snapshot = branchDescriptorCollection.snapshots [snapI];
						for (int lodI = 0; lodI < snapshot.lodCount; lodI++) {
							Mesh meshToRegister = BranchCollectionSproutMeshBuilder.GetSnapshotMesh (
							branchDescriptorCollection, snapI, lodI, 
							Vector3.one, Vector3.zero, Quaternion.identity);
							advancedSproutMeshBuilder.RegisterMesh (meshToRegister, sproutMesh.meshType, sproutMesh.groupId, snapI, lodI);	
						}
					}
				}
				// Add VARIATION meshes.
				if (branchDescriptorCollection.descriptorImplId == BranchDescriptorCollection.VARIATION_COLLECTION) {
					// Register each Snapshot/lod mesh.
					for (int varI = 0; varI < branchDescriptorCollection.variations.Count; varI++) {
						VariationDescriptor variation = branchDescriptorCollection.variations [varI];
						for (int lodI = 0; lodI < variation.lodCount; lodI++) {
							Mesh meshToRegister = BranchCollectionSproutMeshBuilder.GetVariationMesh (
							branchDescriptorCollection, varI, lodI, 
							Vector3.one, Vector3.zero, Quaternion.identity);
							advancedSproutMeshBuilder.RegisterMesh (meshToRegister, sproutMesh.meshType, sproutMesh.groupId, varI, lodI);	
						}
					}
				}

				// Register timestamp to conditionally process the collection.
				if (!_registeredBranchDescriptorCollections.ContainsKey (bdcHash)) {
					_registeredBranchDescriptorCollections.Add (bdcHash, branchDescriptorCollection.timestamp);
				} else {
					_registeredBranchDescriptorCollections[bdcHash] = branchDescriptorCollection.timestamp;
				}
			}
		}
		/// <summary>
		/// Called before assigning subgroups on a branch to set the random seed if fixed.
		/// </summary>
		/// <param name="groupId">Sprout Group Id.</param>
		/// <param name="subgroupId">Sprout Subgroup Id.</param>
		/// <param name="parentBranch">Parent branch.</param>
		void OnBeforeAssignSproutSubgroups (int groupId, int subgroupId, BroccoTree.Branch parentBranch)
		{
			if (sproutMeshGeneratorElement != null && sproutMeshGeneratorElement.isSeedFixed) {
				sproutMeshGeneratorElement.SetSeed (sproutMeshGeneratorElement.fixedSeed + parentBranch.id);
			}
		}
		#endregion
	}
}