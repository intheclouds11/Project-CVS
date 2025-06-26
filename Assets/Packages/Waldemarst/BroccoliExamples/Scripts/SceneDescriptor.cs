using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
#endif
using UnityEngine;
using UnityEngine.Rendering;

using Broccoli.Factory;
using Broccoli.Utils;
using Broccoli.Manager;

namespace Broccoli.Examples 
{
    /// <summary>
    /// Descriptor utility class for Broccoli Tree Creator example scenes.
    /// </summary>
    [ExecuteInEditMode]
    public class SceneDescriptor : MonoBehaviour {
        #region Vars
        public string title = string.Empty;
        public string description = string.Empty;
        public bool autoselectOnSceneLoad = true;
        public bool develShowFields = true;
        public enum RenderPipelineType {
			Regular,
			HDRP,
			URP,
            LWRP
		}
        [SerializeField]
        public RenderPipelineType scenePipelineType = RenderPipelineType.Regular;
        public RenderPipelineType detectedPipelineType = RenderPipelineType.Regular;
        bool _requiresRebuild = true;
        public bool requiresRebuild {
            get { return _requiresRebuild; }
        }
        bool forceRebuild = false;
        bool _hasUpgradeableMaterials = false;
        [System.NonSerialized]
        public List<string> upgradeableMaterials = new List<string> ();
        public bool hasUpgradeableMaterials {
            get { return _hasUpgradeableMaterials; }
        }
        [SerializeField]
        double lastSaved = 0;
        #endregion

        #region Strings
        static string PIPELINE_REGULAR = "Built-in Render Pipeline";
        static string PIPELINE_URP = "Universal Render Pipeline (URP)";
        static string PIPELINE_HDRP = "High Definition Render Pipeline (HDRP)";
        static string PIPELINE_LWRP = "Lightweight Render Pipeline (LWRP)";
        #endregion

        #region Mono Events
        void OnEnable () {
            DetectRenderPipeline ();
            CheckRenderPipelineMismatch ();
            if (_requiresRebuild) {
                upgradeableMaterials = DetectUpgradeableMaterials ();
                if (upgradeableMaterials.Count > 0)
                    _hasUpgradeableMaterials = true;
                RebuildTreeFactories ();
            }
        }
        void OnDisable () {

        }
        void Start () {
            if (autoselectOnSceneLoad) {
                #if UNITY_EDITOR
                UnityEditor.Selection.activeGameObject = this.gameObject;
                UnityEditor.Selection.activeObject = this.gameObject;
                UnityEditor.Selection.SetActiveObjectWithContext (this.gameObject, null);
                #endif
            }
        }
        #endregion

        #region Graphics
        public void CheckRenderPipelineMismatch () {
            if (scenePipelineType != detectedPipelineType || forceRebuild) {
                _requiresRebuild = true;
            } else {
                _requiresRebuild = false;
            }
        }
        public void DetectRenderPipeline () {
			// LightweightPipelineAsset
			// HDRenderPipelineAsset
			// UniversalRenderPipelineAsset
			var currentRenderPipeline = GraphicsSettings.defaultRenderPipeline;
			detectedPipelineType = RenderPipelineType.Regular;
			if (currentRenderPipeline != null) {
				if (GraphicsSettings.defaultRenderPipeline.GetType().Name.Contains ("UniversalRenderPipelineAsset")) {
					detectedPipelineType = RenderPipelineType.URP;
				} else if (GraphicsSettings.defaultRenderPipeline.GetType().Name.Contains ("LightweightPipelineAsset")) {
					detectedPipelineType = RenderPipelineType.LWRP;
				} else if (GraphicsSettings.defaultRenderPipeline.GetType().Name.Contains ("HDRenderPipelineAsset")) {
					detectedPipelineType = RenderPipelineType.HDRP;
				}
			}
		}
        public string PipelineTypeToString (RenderPipelineType pipelineType) {
            if (pipelineType == RenderPipelineType.Regular) {
                return PIPELINE_REGULAR;
            } else if (pipelineType == RenderPipelineType.URP) {
                return PIPELINE_URP;
            } else if (pipelineType == RenderPipelineType.HDRP) {
                return PIPELINE_HDRP;
            } else {
                return PIPELINE_LWRP;
            }
        }
        public void RebuildTreeFactories () {
            TreeFactory[] treeFactories = GameObject.FindObjectsOfType<TreeFactory> ();
            bool rebuild = false;
            for (int i = 0; i < treeFactories.Length; i++) {
                treeFactories [i].ProcessPipelinePreview (null, true);
                rebuild = true;
            }
            scenePipelineType = detectedPipelineType;
            _requiresRebuild = false;
            // Trees were rebuild.
            if (rebuild) {
                Debug.Log ("Broccoli Tree Factories on the Scene have been rebuilt to: " + PipelineTypeToString (scenePipelineType));
                #if UNITY_EDITOR
                string saveScenePrompt = string.Empty;
                string yesBtn = string.Empty;
                string noBtn = string.Empty;
                if (!_hasUpgradeableMaterials) {
                    saveScenePrompt = "Do you want to save the scene now?";
                    yesBtn = "Yes, save this scene";
                    noBtn = "No, I'll save this scene later";
                } else {
                    saveScenePrompt = "Do you want to upgrade the materials and save the scene now?";
                    yesBtn = "Yes, upgrade materials and save this scene";
                    noBtn = "No, don't upgrade materials, I'll save this scene later";
                }
                if (EditorUtility.DisplayDialog ("Tree Factories Processed",
                    "Broccoli has rebuilt some of the Trees on this scene to: " + PipelineTypeToString (scenePipelineType) +
                    "\n" + GetRebuildPrompt() +
                    "\n You will need to save this scene to persist the changes made to the Trees. \n\n" + saveScenePrompt,
                    yesBtn, noBtn))
                {
                    if (_hasUpgradeableMaterials)
                        UpgradeMaterials (upgradeableMaterials);
                    lastSaved = EditorApplication.timeSinceStartup;
                    EditorSceneManager.SaveScene (EditorSceneManager.GetActiveScene());
                }
                #endif
            }
            // No trees rebuilt, but ask the user to update the Materials.
            else if (_hasUpgradeableMaterials) {
                #if UNITY_EDITOR
                if (EditorUtility.DisplayDialog ("Pipeline Mismatch",
                    "This scene was saved with a different Render Pipeline. SRP detected: " + PipelineTypeToString (scenePipelineType) +
                    $"\n{upgradeableMaterials.Count} materials are ready to be upgraded for this package (no other materials in your project are going to be modified). " +
                    "\n\nDo you want to proceed?",
                    "Yes, upgrade the materials and save this scene", "No, don't upgrade materials"))
                {
                    UpgradeMaterials (upgradeableMaterials);
                    lastSaved = EditorApplication.timeSinceStartup;
                    EditorSceneManager.SaveScene (EditorSceneManager.GetActiveScene());
                }
                #endif
            }
        }
        public string GetRebuildPrompt () {
            if (_hasUpgradeableMaterials) {
                if (detectedPipelineType == RenderPipelineType.HDRP) {
                    return $"This package has {upgradeableMaterials.Count} materials ready to be upgraded to HDRP (no other materials in your project are going to be modified).";
                } else if (detectedPipelineType == RenderPipelineType.URP || detectedPipelineType == RenderPipelineType.LWRP) {
                    return $"This package has {upgradeableMaterials.Count} materials ready to be upgraded to URP (no other materials in your project are going to be modified).";
                }
            }
            return string.Empty;
        }
        public TreeFactory GetTreeFactory () {
            TreeFactory[] treeFactories = GameObject.FindObjectsOfType<TreeFactory> ();
            if (treeFactories.Length > 0) return treeFactories[0];
            return null;
        }
        #endregion

        #region Materials
        public List<string> DetectUpgradeableMaterials () {
            List<string> filteredPaths = new List<string> ();
            #if UNITY_EDITOR
            List<string> paths = new List<string> ();
            string path = Broccoli.Base.ExtensionManager.extensionPath; 
            if (AssetDatabase.IsValidFolder (path))
                paths.Add (path);
            path = Broccoli.Base.ExtensionManager.extensionPath.Replace ("Broccoli", "BroccoliExamples");
            if (AssetDatabase.IsValidFolder (path))
                paths.Add (path);
            path = Broccoli.Base.ExtensionManager.extensionPath.Replace ("Broccoli", "Alfalfa");
            if (AssetDatabase.IsValidFolder (path))
                paths.Add (path);
            path = Broccoli.Base.ExtensionManager.extensionPath.Replace ("Broccoli", "AlfalfaExamples");
            if (AssetDatabase.IsValidFolder (path))
                paths.Add (path);
            var allMaterialGUIDs = AssetDatabase.FindAssets("t:material", paths.ToArray ());

            foreach (var asset in allMaterialGUIDs) {
                path = AssetDatabase.GUIDToAssetPath(asset);
                var name = Path.GetFileName(path);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material.shader.name.IndexOf ("Standard") == 0 || material.shader.name.IndexOf ("Nature/SpeedTree8") == 0) {
                    filteredPaths.Add (path);
                }
            }
            #endif
            return filteredPaths;
        }
        public void UpgradeMaterials (List<string> upgradeableMaterialPaths) {
            #if UNITY_EDITOR
            MaterialManager materialManager = new MaterialManager ();
            materialManager.SetDefaultShader ();
            materialManager.SetLeavesShader (TreeFactoryPreferences.PreferredShader.SpeedTree8);
            string upgradedMaterialsInfo = string.Empty;
            int totalUpgradedMaterials = 0;
            foreach (var materialPath in upgradeableMaterialPaths) {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material.shader.name.IndexOf ("Standard") == 0) {
                    Material defaultMaterial = new Material(MaterialManager.defaultShader);
                    // Color.
                    if (defaultMaterial.HasProperty ("_BaseColor")) {
                        defaultMaterial.SetColor ("_BaseColor", material.GetColor ("_Color"));
                    }
                    // MainTex.
                    if (defaultMaterial.HasProperty ("_BaseMap")) { // URP
                        defaultMaterial.SetTexture ("_BaseMap", material.GetTexture ("_MainTex"));
                    }
                    if (defaultMaterial.HasProperty ("_BaseColorMap")) { // HDRP
                        defaultMaterial.SetTexture ("_BaseColorMap", material.GetTexture ("_MainTex"));
                    }
                    // BumpMap.
                    if (defaultMaterial.HasProperty ("_BumpMap")) { // URP
                        defaultMaterial.SetTexture ("_BumpMap", material.GetTexture ("_BumpMap"));
                    }
                    if (defaultMaterial.HasProperty ("_NormalMap")) { // HDRP
                        defaultMaterial.SetTexture ("_NormalMap", material.GetTexture ("_BumpMap"));
                    }
                    // Glossiness.
                    if (defaultMaterial.HasProperty ("_Smoothness")) {
                        defaultMaterial.SetFloat ("_Smoothness", material.GetFloat ("_Glossiness"));
                    }
                    // Metallic.
                    if (defaultMaterial.HasProperty ("_Metallic")) {
                        defaultMaterial.SetFloat ("_Metallic", material.GetFloat ("_Metallic"));
                    }
                    // Cutoff.
                    if (defaultMaterial.HasProperty ("_Cutoff")) {
                        defaultMaterial.SetFloat ("_Cutoff", material.GetFloat ("_Cutoff"));
                    }
                    if (defaultMaterial.HasProperty ("_AlphaCutoff")) { // HDRP
                        defaultMaterial.SetFloat ("_AlphaCutoff", material.GetFloat ("_Cutoff"));
                    }
                    // Mode for transparency.
                    int matMode = Mathf.RoundToInt (material.GetFloat ("_Mode"));
                    if (matMode == 1 || matMode == 3) {
                        if (defaultMaterial.HasProperty ("_AlphaClip")) { // URP
                            defaultMaterial.SetFloat ("_AlphaClip", 1f);
                        }
                        if (defaultMaterial.HasProperty ("_AlphaCutoffEnable")) { // HDRP
                            defaultMaterial.SetFloat ("_AlphaCutoffEnable", 1f);
                        }
                    }

                    material.shader = MaterialManager.defaultShader;
                    material.CopyPropertiesFromMaterial (defaultMaterial);
                    
                    upgradedMaterialsInfo += material.name + "\n";
                    totalUpgradedMaterials++;
                }
                else if (material.shader.name.IndexOf ("Nature/SpeedTree8") == 0) {
                    material.shader = MaterialManager.leavesShader;
                    upgradedMaterialsInfo += material.name + "\n";
                    totalUpgradedMaterials++;
                }
            }
            AssetDatabase.SaveAssets ();
            AssetDatabase.Refresh ();
            Resources.UnloadUnusedAssets ();
            if (!string.IsNullOrEmpty(upgradedMaterialsInfo)) {
                Debug.Log ($"{totalUpgradedMaterials} materials has been upgraded to {PipelineTypeToString (detectedPipelineType)}\n" +
                    upgradedMaterialsInfo);
            }
            #endif
        }
        #endregion
    }
}