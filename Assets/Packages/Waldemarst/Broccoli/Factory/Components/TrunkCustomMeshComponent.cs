using System.Collections.Generic;

using UnityEngine;

using Broccoli.Model;
using Broccoli.Pipe;
using Broccoli.Factory;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.RectpackSharp;
using Broccoli.Generator;
using Broccoli.Utils;
using UnityEngine.AI;

namespace Broccoli.Component
{
	using AssetManager = Broccoli.Manager.AssetManager;
	/// <summary>
	/// Baker component.
	/// Does nothing, knows nothing... just like Jon.
	/// </summary>
	public class TrunkCustomMeshComponent : TreeFactoryComponent {
		#region Vars
		/// <summary>
		/// The positioner element.
		/// </summary>
		TrunkCustomMeshElement trunkCustomMeshElement = null;
		public const int CMD_UPDATE_TRUNK_GO = 0;
		/// <summary>
		/// Temporary holds the combined mesh (trunk and branches) when processing a Prefab.
		/// </summary>
		private Mesh combinedTrunkMesh = null;

		private int trunkMeshIndex = -1;
		private int rectIndex = -1;
		private int branchMeshVertLength = -1;
		private bool hasTexturesCollectedRectsCreated = false;
		List<Texture2D> albedoTextures = new List<Texture2D> ();
		List<Texture2D> normalTextures = new List<Texture2D> ();
		List<Texture2D> extraTextures = new List<Texture2D> ();
		List<Rect> rects = new List<Rect> ();
		Texture2D albedoTex = null;
		Texture2D normalTex = null;
		Texture2D extraTex = null;
		bool hasAlbedoTex = false;
		bool hasNormalTex = false;
		bool hasExtraTex = false;
		public int BranchMeshVertLength {
			get {return branchMeshVertLength; }
		}
		public int RectIndex {
			get { return rectIndex; }
		}
		public int TrunkMeshIndex {
			get { return trunkMeshIndex; }
		}
		public List<Rect> Rects {
			get { return rects; }
		}
		#endregion

		#region Configuration
		/// <summary>
		/// Gets the changed aspects on the tree for this component.
		/// </summary>
		/// <returns>The changed aspects.</returns>
		public override int GetChangedAspects () {
			return (int)TreeFactoryProcessControl.ChangedAspect.None;
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
			TreeFactoryProcessControl processControl = null) 
		{
			if (pipelineElement != null && tree != null) {
				trunkCustomMeshElement = pipelineElement as TrunkCustomMeshElement;
				combinedTrunkMesh = null;
				
				RegisterTrunks (treeFactory);
				UpdateTrunkGO (treeFactory);

				// If Prefab building merge mesh.
				if (processControl.isPrefabProcess) {
					// Get the random trunk if it is the first lod being processed (keep the same trunk index for the rest of the lods).
					if (!hasTexturesCollectedRectsCreated) {
						trunkMeshIndex = GetTrunkMeshIndex (processControl.isPrefabProcess);
					}

					// If the mesh manager has a mesh for branches.
					if (treeFactory.meshManager.HasMesh (MeshManager.MeshData.TYPE_BRANCH)) {
						// 1. Get the Branches mesh.
						Mesh branchesMesh = treeFactory.meshManager.GetMeshByType (MeshManager.MeshData.TYPE_BRANCH);
						branchMeshVertLength = branchesMesh.vertexCount;

						// 2. Get the Trunk mesh.
						TrunkMeshDescriptor trunkDesc = trunkCustomMeshElement.trunkMeshes[trunkMeshIndex];
						float lodFactor = treeFactory.treeFactoryPreferences.GetLODFactor (processControl.lodIndex, 0f, 2f);
						int lod = Mathf.RoundToInt (lodFactor);
						Mesh trunkMesh = GetTrunkMesh (trunkMeshIndex, lod, treeFactory);

						if (trunkMesh != null) {
							// 3. Combine both meshes. Save the indices to further UV remapping.
							Mesh combinedMesh = CombineTrunkMesh (branchesMesh, trunkMesh, 
								trunkDesc.combinedPosition, trunkDesc.combinedRotation, trunkDesc.combinedScale);
							combinedTrunkMesh = combinedMesh; 
							// 4. Replace with the new branch mesh.
							treeFactory.meshManager.RegisterBranchMesh (combinedMesh);

							// 5. Collect the textures and create the rects.
							CollectTexturesCreateRects (treeFactory);
						}
					}
				} else {
					if (hasTexturesCollectedRectsCreated) ClearTexturesAndRects ();
				}
				return true;
			}
			return false;
		}
		private Mesh GetTrunkMesh (int trunkIndex, int targetLOD, TreeFactory treeFactory)
		{
			if (targetLOD == 2 && treeFactory.extraMeshManager.HasMesh (MESH_TYPE_CUSTOM_TRUNK, trunkIndex, targetLOD)) {
				return treeFactory.extraMeshManager.GetMeshByType (MESH_TYPE_CUSTOM_TRUNK, trunkIndex, 2);
			}
			if (targetLOD == 1 && treeFactory.extraMeshManager.HasMesh (MESH_TYPE_CUSTOM_TRUNK, trunkIndex, targetLOD)) {
				return treeFactory.extraMeshManager.GetMeshByType (MESH_TYPE_CUSTOM_TRUNK, trunkIndex, 1);
			} else {
				return treeFactory.extraMeshManager.GetMeshByType (MESH_TYPE_CUSTOM_TRUNK, trunkIndex, 0);
			}
		}
		/// <summary>
		/// Processes called only on the prefab creation.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void OnProcessPrefab (TreeFactory treeFactory) {
			// Create combined albedo texture.
			string folderPath = treeFactory.assetManager.GetPrefabResourceFolder ();
			string texturePath = folderPath + "/" + "branch_albedo" + ".png";
			Debug.Log ("Saving compound texture to: " + texturePath);
			Texture2D albedoCombinedTex = TextureUtil.CombineTextures (1024, 1024, albedoTextures, rects);
			TextureUtil.SaveTextureToFile (albedoCombinedTex, texturePath, false, true);
		}
		/// <summary>
		/// Removes the product of this component on the factory processing.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void Unprocess (TreeFactory treeFactory) {
			DeregisterTrunks (treeFactory);
		}
		/// <summary>
		/// Process a special command or subprocess on this component.
		/// </summary>
		/// <param name="cmd">Cmd.</param>
		/// <param name="treeFactory">Tree factory.</param>
		public override void ProcessComponentOnly (int cmd, TreeFactory treeFactory) {
			if (pipelineElement != null && tree != null) {
				if (cmd == CMD_UPDATE_TRUNK_GO) {
					UpdateTrunkGO (treeFactory);
				}
			}
		}
		#endregion

		#region Prefabs Processing (Mesh, Materials and Textures)
		private const string CUSTOM_TRUNK_GO_NAME = "custmoTrunk";
		private const int MESH_TYPE_CUSTOM_TRUNK = 3;
		/// <summary>
		/// Register the custom trunk meshes and materials.
		/// </summary>
		/// <param name="treeFactory">TreeFactory processing the tree.</param>
		private void RegisterTrunks (TreeFactory treeFactory)
		{
			// Extract Meshes and Material. Register them.
			for (int i = 0; i < trunkCustomMeshElement.trunkMeshes.Count; i++) {
				TrunkMeshDescriptor trunkDesc = trunkCustomMeshElement.trunkMeshes[i];
				trunkDesc.ExtractMainMeshAndMaterial ();
				trunkDesc.ExtractLOD1Mesh ();
				trunkDesc.ExtractLOD2Mesh ();
				if (trunkDesc.Mesh != null) {
					treeFactory.extraMeshManager.RegisterCustomMesh (trunkDesc.Mesh, MESH_TYPE_CUSTOM_TRUNK, i, 0, false);
				}
				if (trunkDesc.MeshLOD1 != null) {
					treeFactory.extraMeshManager.RegisterCustomMesh (trunkDesc.MeshLOD1, MESH_TYPE_CUSTOM_TRUNK, i, 1, false);
				}
				if (trunkDesc.MeshLOD2 != null) {
					treeFactory.extraMeshManager.RegisterCustomMesh (trunkDesc.MeshLOD2, MESH_TYPE_CUSTOM_TRUNK, i, 2, false);
				}
				if (trunkDesc.Material != null) {
					treeFactory.extraMaterialManager.RegisterCustomMaterial (MESH_TYPE_CUSTOM_TRUNK, trunkDesc.Material, i, 0, false);
				}
			}
		}
		/// <summary>
		/// Deregisters the custom trunk meshes and materials.
		/// </summary>
		/// <param name="treeFactory">TreeFactory processing the tree.</param>
		private void DeregisterTrunks (TreeFactory treeFactory)
		{
			treeFactory.extraMeshManager.DeregisterMeshByType (MESH_TYPE_CUSTOM_TRUNK);
			treeFactory.extraMaterialManager.DeregisterMaterialByType (MESH_TYPE_CUSTOM_TRUNK); 
		}
		private void UpdateTrunkGO (TreeFactory treeFactory)
		{
			if (trunkCustomMeshElement.HasSelectedMeshAndIsEnabled ())
			{
				TrunkMeshDescriptor trunkDesc = trunkCustomMeshElement.GetSelectedTrunkMeshDescriptor ();
				treeFactory.gameObjectManager.AddMeshGameObject (CUSTOM_TRUNK_GO_NAME, trunkDesc.Mesh, trunkDesc.Material);
				treeFactory.gameObjectManager.SetTransform (CUSTOM_TRUNK_GO_NAME, 
					trunkDesc.combinedPosition, 
					trunkDesc.combinedRotation.eulerAngles, 
					trunkDesc.combinedScale);
			} else {
				treeFactory.gameObjectManager.RemoveGameObject (CUSTOM_TRUNK_GO_NAME);
			}
		}
		
		private void ClearTexturesAndRects ()
		{
			albedoTextures.Clear ();
			normalTextures.Clear ();
			extraTextures.Clear ();
			rects.Clear ();
			albedoTex = null;
			normalTex = null;
			extraTex = null;
			hasAlbedoTex = false;
			hasNormalTex = false;
			hasExtraTex = false;
			hasTexturesCollectedRectsCreated = false;
		}
		private void CollectTexturesCreateRects (TreeFactory treeFactory)
		{
			if (!hasTexturesCollectedRectsCreated) {
				// 0. Clear all the variables holding textures, rects and checks.
				ClearTexturesAndRects ();

				// 1. Add the branch texture as the 1st texture.
				BranchMapperElement branchMapperElement =
					(BranchMapperElement)trunkCustomMeshElement.GetDownstreamElement (PipelineElement.ClassType.BranchMapper);
				if (branchMapperElement != null) {
					branchMapperElement.GetTextures (out albedoTex, out normalTex, out extraTex);
					albedoTex = branchMapperElement.mainTexture;
					normalTex = branchMapperElement.normalTexture;
					extraTex =  branchMapperElement.extrasTexture;
					if (albedoTex != null) {
						albedoTextures.Add (albedoTex);
						hasAlbedoTex = true;
						if (normalTex != null) {
							normalTextures.Add (normalTex);
							hasNormalTex = true;
						} else {
							normalTextures.Add (null);
						}
						if (extraTex != null) {
							extraTextures.Add (extraTex);
							hasExtraTex = true;
						} else {
							extraTextures.Add (null);
						}
					}
				}

				// 2. Add trunk textures (if a branch texture is found).
				if (hasAlbedoTex) {
					for (int trunkIndex = 0; trunkIndex < trunkCustomMeshElement.trunkMeshes.Count; trunkIndex ++) {
						TrunkMeshDescriptor trunkDesc = trunkCustomMeshElement.trunkMeshes[trunkIndex];
						// If the trunk descriptor is found and enabled.
						if (trunkDesc != null && trunkDesc.enabled) {
							Material trunkMat = treeFactory.extraMaterialManager.GetMaterial (MESH_TYPE_CUSTOM_TRUNK, false, trunkIndex);
							// If the trunk material is not null.
							if (trunkMat != null) {
								if (trunkMat.HasTexture (trunkDesc.albedoTexProp)) {
									albedoTex = (Texture2D)trunkMat.GetTexture (trunkDesc.albedoTexProp);
									if (trunkIndex == trunkMeshIndex) rectIndex = albedoTextures.Count;
									albedoTextures.Add (albedoTex);
									if (trunkMat.HasTexture (trunkDesc.normalTexProp)) {
										normalTex = (Texture2D)trunkMat.GetTexture (trunkDesc.normalTexProp);
										normalTextures.Add (normalTex);
										hasNormalTex = true;
									} else {
										normalTextures.Add (null);
									}
									if (trunkMat.HasTexture (trunkDesc.extraTexProp)) {
										extraTex = (Texture2D)trunkMat.GetTexture (trunkDesc.extraTexProp);
										extraTextures.Add (extraTex);
										hasExtraTex = true;
									} else {
										extraTextures.Add (null);
									}
								} else {
									Debug.LogWarning ("Albedo texture not for TrunkMeshDescriptor at index " + trunkIndex);
								}
							}
						}
					}

					//4. Create Rects for albedo textures.
					float width = 0f;
					width = 1f / albedoTextures.Count;
					for (int texIndex = 0; texIndex < albedoTextures.Count; texIndex++) {
						Rect rect = new Rect (width * texIndex, 0f, width, 1f); // x, y, width, height
						rects.Add (rect);
					}
				}
			}
			hasTexturesCollectedRectsCreated = true;
		}
		#endregion

		#region Mesh Processing
		private int GetTrunkMeshIndex (bool isRandom)
		{
			if (isRandom) {
				// List enabled trunk mesh descriptors.
				List<int> enabledTrunkDescIndices = new List<int> ();
				for (int i = 0; i < trunkCustomMeshElement.trunkMeshes.Count; i++) {
					if (trunkCustomMeshElement.trunkMeshes[i].enabled) {
						enabledTrunkDescIndices.Add (i);
					}
				}
				return enabledTrunkDescIndices [Random.Range (0, enabledTrunkDescIndices.Count)];
			} else {
				TrunkMeshDescriptor trunkDesc = trunkCustomMeshElement.GetSelectedTrunkMeshDescriptor ();
				if (trunkDesc != null && trunkDesc.enabled) {
					return trunkCustomMeshElement.selectedTrunkIndex;
				}
			}
			return -1;
		}
		/// <summary>
		/// Merges a primary mesh with the first submesh of a secondary mesh,
		/// applying a given transformation to the secondary mesh.
		/// </summary>
		/// <param name="primaryMesh">The main mesh. Assumed to have one submesh.</param>
		/// <param name="secondaryMesh">The mesh to take the first submesh from.</param>
		/// <param name="position">The position offset to apply to the secondary mesh.</param>
		/// <param name="rotation">The rotation to apply to the secondary mesh.</param>
		/// <param name="scale">The scale to apply to the secondary mesh.</param>
		/// <returns>A new Mesh with two submeshes, or null if inputs are invalid.</returns>
		private Mesh CombineTrunkMesh(
			Mesh primaryMesh, 
			Mesh secondaryMesh, 
			Vector3 position, 
			Quaternion rotation, 
			Vector3 scale)
		{
			// --- 1. Input Validation ---
			if (primaryMesh == null || secondaryMesh == null)
			{
				Debug.LogError("Cannot merge meshes. One or both input meshes are null.");
				return null;
			}
			if (secondaryMesh.subMeshCount < 1)
			{
				Debug.LogError("Secondary mesh does not have any submeshes to merge.");
				return null;
			}

			// --- 2. Create an array of CombineInstance structs ---
			CombineInstance[] combine = new CombineInstance[2];

			// Setup the first CombineInstance for the primary mesh (with no transformation).
			combine[0].mesh = primaryMesh;
			combine[0].subMeshIndex = 0;
			combine[0].transform = Matrix4x4.identity; // No change for the primary mesh

			// Setup the second CombineInstance for the secondary mesh.
			combine[1].mesh = secondaryMesh;
			combine[1].subMeshIndex = 0; // Use only the first submesh.
			
			// Create a transformation matrix from the position, rotation, and scale parameters.
			combine[1].transform = Matrix4x4.TRS(position, rotation, scale);

			// --- 3. Create a new mesh and combine the instances into it ---
			Mesh combinedMesh = new Mesh();
			combinedMesh.name = "Combined_Mesh_With_Transform";
			
			// The 'true' for mergeSubMeshes is crucial to get a unique submesh.
			// The 'true' for useMatrices ensures the transform matrix is applied.
			combinedMesh.CombineMeshes(combine, true, true);
			
			// --- 4. Finalize the mesh for better performance ---
			combinedMesh.UploadMeshData(false);

			return combinedMesh;
		}
		/// <summary>
        /// Remaps the UV coordinates of a mesh into two separate rectangular regions.
        /// The mesh's vertices are split into two groups based on a starting index.
        /// </summary>
        /// <param name="mesh">The Mesh object to modify in place.</param>
        /// <param name="secondRegionVertStart">The vertex index at which the second region begins. All vertices before this index belong to the first region.</param>
        /// <param name="firstRect">The target UV rectangle (values 0-1) for the first region of vertices.</param>
        /// <param name="secondRect">The target UV rectangle (values 0-1) for the second region of vertices.</param>
        public static void RemapUVs(Mesh mesh, int secondRegionVertStart, Rect firstRect, Rect secondRect)
        {
            // 1. --- Input Validation ---
            if (mesh == null)
            {
                Debug.LogError("MeshUtils.RemapUVs: Input mesh cannot be null.");
                return;
            }

            List<Vector4> originalUVs = new List<Vector4>();
			mesh.GetUVs (0, originalUVs);
 
            if (originalUVs == null || originalUVs.Count == 0)
            {
                Debug.LogWarning("MeshUtils.RemapUVs: Mesh has no existing UV data to remap.");
                return;
            }

            int vertexCount = mesh.vertexCount;
            if (originalUVs.Count != vertexCount)
            {
                Debug.LogError("MeshUtils.RemapUVs: UV array length does not match vertex count.");
                return;
            }

            // Clamp the start index to ensure it's within a valid range.
            int clampedSecondRegionStart = Mathf.Clamp(secondRegionVertStart, 0, vertexCount);


            // 2. --- Remap UVs for both regions ---
            // We will create a List<Vector4> to hold the new UV data.
            // Using Vector4 allows for compatibility with Mesh.SetUVs and potential future use of z/w channels.
            List<Vector4> remappedUVs = new List<Vector4>(vertexCount);

            // Process the first region (from vertex 0 to before the start of the second region)
            for (int i = 0; i < clampedSecondRegionStart; i++)
            {
                // Get the original UV coordinate
                Vector4 originalUV = originalUVs[i];

                // Remap the original UV (assumed to be in 0-1 space) into the target firstRect
                originalUV.x = firstRect.x + originalUV.x * firstRect.width;
                originalUV.y = firstRect.y + originalUV.y * firstRect.height;

                remappedUVs.Add(originalUV);
            }

            // Process the second region (from the start index to the end of the mesh)
            for (int i = clampedSecondRegionStart; i < vertexCount; i++)
            {
                // Get the original UV coordinate
                Vector4 originalUV = originalUVs[i];

                // Remap the original UV into the target secondRect
                originalUV.x = secondRect.x + originalUV.x * secondRect.width;
                originalUV.y = secondRect.y + originalUV.y * secondRect.height;

                remappedUVs.Add(originalUV);
            }


            // 3. --- Apply the new UVs to the Mesh ---
            // Set the remapped UVs to the first UV channel (uv).
            mesh.SetUVs(0, remappedUVs);
        }
		#endregion
	}
}