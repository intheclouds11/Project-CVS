using System.Collections.Generic;

using UnityEngine;

using Broccoli.Model;
using Broccoli.Pipe;
using Broccoli.Factory;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.RectpackSharp;

namespace Broccoli.Component
{
	using AssetManager = Broccoli.Manager.AssetManager;
	/// <summary>
	/// Baker component.
	/// Does nothing, knows nothing... just like Jon.
	/// </summary>
	public class BakerComponent : TreeFactoryComponent {
		#region Vars
		/// <summary>
		/// The positioner element.
		/// </summary>
		BakerElement bakerElement = null;
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
		/// <param name="ProcessControl">Process control.</param>
		public override bool Process (TreeFactory treeFactory, 
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl ProcessControl = null) 
		{
			if (pipelineElement != null && tree != null) {
				bakerElement = pipelineElement as BakerElement;
				// AMBIENT OCCLUSION.
				if (bakerElement.enableAO) {
					bool enableAO = (ProcessControl.isPreviewProcess && bakerElement.enableAOInPreview) || ProcessControl.isRuntimeProcess || ProcessControl.isPrefabProcess;
					if (enableAO) {
						treeFactory.meshManager.enableAO = true;
						treeFactory.meshManager.samplesAO = bakerElement.samplesAO;
						treeFactory.meshManager.strengthAO = bakerElement.strengthAO;
					} else {
						treeFactory.meshManager.enableAO = false;
					}
				} else {
					treeFactory.meshManager.enableAO = false;
				}
				// UNWRAP UV1s
				if (bakerElement.unwrapUV1sAtRuntime) {
					UnwrapUV1s (treeFactory);
				}
				// COLLIDER.
				if (bakerElement.addCollider) {
					AddCollisionObjects (treeFactory);
				} else {
					RemoveCollisionObjects ();
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Processes called only on the prefab creation.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void OnProcessPrefab (TreeFactory treeFactory) {
			treeFactory.meshManager.enableAO = false;
			if (bakerElement.enableAO) {
				treeFactory.assetManager.enableAO = true;
				treeFactory.assetManager.samplesAO = bakerElement.samplesAO;
				treeFactory.assetManager.strengthAO = bakerElement.strengthAO;
			}
			if (bakerElement.lodFade == BakerElement.LODFade.Crossfade) {
				treeFactory.assetManager.lodFadeMode = LODFadeMode.CrossFade;
			} else if (bakerElement.lodFade == BakerElement.LODFade.SpeedTree) {
				treeFactory.assetManager.lodFadeMode = LODFadeMode.SpeedTree;
			} else {
				treeFactory.assetManager.lodFadeMode = LODFadeMode.None;
			}
			treeFactory.assetManager.lodFadeAnimate = bakerElement.lodFadeAnimate;
			treeFactory.assetManager.lodTransitionWidth = bakerElement.lodTransitionWidth;
			treeFactory.assetManager.enableUnwrappedUV1 = bakerElement.unwrapUV1sAtPrefab;
			treeFactory.assetManager.keepUV5Data = bakerElement.keepUV5Data;
			treeFactory.assetManager.splitSubmeshesIntoGOs = bakerElement.splitSubmeshes;
		}
		/// <summary>
		/// Adds the collision objects.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		protected void AddCollisionObjects (TreeFactory treeFactory) {
			List<BroccoTree.Branch> rootBranches = tree.branches;
			Vector3 trunkBase;
			Vector3 trunkTip;
			RemoveCollisionObjects ();
			for (int i = 0; i < rootBranches.Count; i++) {
				float scale = treeFactory.treeFactoryPreferences.factoryScale;
				CapsuleCollider capsuleCollider = tree.obj.AddComponent<CapsuleCollider> ();
				capsuleCollider.radius = rootBranches [i].maxGirth * bakerElement.colliderScale * scale;
				trunkBase = rootBranches [i].GetPointAtPosition (0f);
				trunkTip = rootBranches [i].GetPointAtPosition (1f);
				capsuleCollider.height = Vector3.Distance (trunkTip, trunkBase) * scale;
				capsuleCollider.center = (trunkTip + trunkBase) / 2f * scale;
			}
		}
		/// <summary>
		/// Removes the collision objects.
		/// </summary>
		protected void RemoveCollisionObjects () {
			// Remove any capsule colliders.
			List<CapsuleCollider> capsuleColliders = new List<CapsuleCollider> ();
			tree.obj.GetComponents<CapsuleCollider> (capsuleColliders);
			if (capsuleColliders.Count > 0) {
				for (int i = 0; i < capsuleColliders.Count; i++) {
					Object.DestroyImmediate (capsuleColliders [i]);
				}
			}
			capsuleColliders.Clear ();
			// Remove any mesh colliders.
			List<MeshCollider> meshColliders = new List<MeshCollider> ();
			tree.obj.GetComponents<MeshCollider> (meshColliders);
			if (meshColliders.Count > 0) {
				for (int i = 0; i < meshColliders.Count; i++) {
					Object.DestroyImmediate (meshColliders [i]);
				}
			}
			meshColliders.Clear ();
		}
		/// <summary>
		/// Sets the wind data.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="updatePreviewTree">If set to <c>true</c> update preview tree.</param>
		void UnwrapUV1s (TreeFactory treeFactory) {
			if (pipelineElement != null && tree != null) {
				BranchMeshGeneratorElement branchMeshGeneratorElement = 
				(BranchMeshGeneratorElement) pipelineElement.GetUpstreamElement (PipelineElement.ClassType.BranchMeshGenerator); 
			
				if (branchMeshGeneratorElement != null && branchMeshGeneratorElement.isActive) {
					BranchMeshGeneratorComponent branchMeshGeneratorComponent = 
						(BranchMeshGeneratorComponent)treeFactory.componentManager.GetFactoryComponent (branchMeshGeneratorElement);
					Dictionary<int, BranchMeshBuilder.BranchSkin> branchSkins = 
						branchMeshGeneratorComponent.meshBuilder.GetIdToBranchSkin ();
					List<PackingRectangle> packingRects = new List<PackingRectangle> ();

					// Add branch sking to rect packer.
					BranchMeshBuilder.BranchSkin skin;
					var branchSkinEnum = branchSkins.GetEnumerator ();
					float scaleFactor = 1000f;
					while (branchSkinEnum.MoveNext ()) {
						skin = branchSkinEnum.Current.Value;
						if (skin.maxAvgGirth > 0 && skin.length > 0) {
							packingRects.Add (new PackingRectangle (
								0, 0, (uint)Mathf.CeilToInt(skin.maxAvgGirth * scaleFactor), (uint)Mathf.CeilToInt(skin.length * scaleFactor), branchSkinEnum.Current.Key, 0));
						}
					}

					// Pack rectangles.
					PackingRectangle[] packRects = packingRects.ToArray ();
					PackingRectangle packingRectangle;
					RectanglePacker.Pack(packRects, out packingRectangle);

					// Create the packing rect to branck skin id relationship dictionary.
					Dictionary<int, PackingRectangle> idToPackRect = new Dictionary<int, PackingRectangle> ();
					PackingRectangle tmpPackRect;
					float padding = (packingRectangle.Width * 5) / 1000.0f;
					if (padding > 0f) {
						padding = Mathf.Ceil (padding);
					}
					for (int i = 0; i < packRects.Length; i++) {
						tmpPackRect = packRects [i];
						tmpPackRect.Padding = (uint)padding;
						if (tmpPackRect.Group == 0) {
							idToPackRect.Add (tmpPackRect.Id, tmpPackRect);
						}
					}

					// Apply the UV1s to the branch mesh.
					// Adding branch skins to the rect packer.
					/// UV5 (ch. 4) information of the mesh.
					/// x: id of the branch.
					/// y: id of the branch skin.
					/// z: id of the struct.
					/// w: geometry type (0 = branch, 1 = sprout).
					/// 
					/// UV8 (ch. 7) information of the mesh.
					/// x: radial position. (used for UV mapping and unwrapping)
					/// y: branch length position. (used for UV mapping)
					/// z: girth.
					/// w: branch skin length position. (used for UV unwrapping)
					int branchMeshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Branch);
					Mesh mesh = treeFactory.meshManager.GetMesh (branchMeshId);
					if (mesh != null) {
						List<Vector4> uv1s = new List<Vector4> ();
						List<Vector4> uv4s = new List<Vector4> ();
						List<Vector4> uv7s = new List<Vector4> ();
						mesh.GetUVs (1, uv1s);
						mesh.GetUVs (4, uv4s);
						mesh.GetUVs (7, uv7s);

						int branchSkinId;
						int uvsLength = uv1s.Count;
						packingRectangle.Id = -1;
						tmpPackRect = packingRectangle;
						Vector4 uv1;
						Vector4 uv7;
						for (int i = 0; i < uvsLength; i++) {
							// Get the pack rect to use on the uvs.
							branchSkinId = (int)uv4s[i].y;
							if (branchSkinId != tmpPackRect.Id) {
								if (idToPackRect.ContainsKey (branchSkinId)) {
									tmpPackRect = idToPackRect [branchSkinId];
								} else {
									tmpPackRect = packingRectangle;
								}
							}

							// Apply UV1s.
							uint pWidth = tmpPackRect.GetPWidth(packingRectangle.Width);
							uint pHeight = tmpPackRect.GetPHeight(packingRectangle.Height);
							if (tmpPackRect.Id > -1) {
								uv1 = uv1s[i];
								uv7 = uv7s[i];
								uv1.x = (tmpPackRect.PX + (tmpPackRect.GetPWidth(packingRectangle.Width) * uv7.x)) / (float)packingRectangle.Width;
								uv1.y = (tmpPackRect.PY + (tmpPackRect.GetPHeight(packingRectangle.Height) * uv7.w)) / (float)packingRectangle.Height;
								uv1s[i] = uv1;
							}
						}
						mesh.SetUVs (1, uv1s);
					}
				}
			}
		}
		#endregion
	}
}