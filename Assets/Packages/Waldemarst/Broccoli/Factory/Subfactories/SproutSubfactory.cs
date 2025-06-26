using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Rendering;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Model;
using Broccoli.Builder;
using Broccoli.Generator;
using Broccoli.Manager;
using Broccoli.Utils;

namespace Broccoli.Factory
{
    using Pipeline = Broccoli.Pipe.Pipeline;
    /// <summary>
    /// Factory used to generate snapshots and variations related outputs.
    /// ProcessSnapshotPolygons > GenerateSnapshotPolygonsPerLOD: analyzes and creates mesh data (polygons) for each snapshot (snapshot).
    /// </summary>
    public class SproutSubfactory {
        #region Vars
        /// <summary>
        /// Internal TreeFactory instance to create branches. 
        /// It must be provided from a parent TreeFactory when initializing this subfactory.
        /// </summary>
        public TreeFactory treeFactory = null;
        /// <summary>
        /// Factory scale to override every pipeline loaded.
        /// All the exposed factory values will be multiplied by this scale and displayed in meters.
        /// The generated mesh will have scaled vertex positions.
        /// </summary>
        public float factoryScale = 1f;
        /// <summary>
        /// Polygon area builder.
        /// </summary>
        public PolygonAreaBuilder polygonBuilder = new PolygonAreaBuilder ();
        /// <summary>
        /// Sprout composite manager.
        /// </summary>
        public SproutCompositeManager sproutCompositeManager = new SproutCompositeManager ();
        /// <summary>
        /// Simplyfies the convex hull on the branch segments.
        /// </summary>
        public bool simplifyHullEnabled = true;
        /// <summary>
        /// Branch descriptor collection to handle values.
        /// </summary>
        public BranchDescriptorCollection branchDescriptorCollection = null;
        /// <summary>
        /// Selected snapshot index.
        /// </summary>
        public int snapshotIndex = -1;
        /// <summary>
        /// Selected variation descriptor index.
        /// </summary>
        public int variationIndex = -1;
        /// <summary>
        /// Saves the branch structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> branchLevels = new List<StructureGenerator.StructureLevel> ();
        // TODOSSS remove
        /// <summary>
        /// Saves the sprout A structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> sproutALevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the sprout B structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> sproutBLevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the crown structure level on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> crownLevels = new List<StructureGenerator.StructureLevel> ();
        // TODOSSS remove
        /// <summary>
        /// Saves the sprout A structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> sproutLevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the sprout mesh instances representing sprout groups.
        /// </summary>
        List<SproutMesh> sproutMeshes = new List<SproutMesh> ();
        /// <summary>
        /// Branch mesh generator element.
        /// </summary>
        BranchMeshGeneratorElement branchMeshGeneratorElement = null;
        /// <summary>
        /// Branch mapper element to set branch textures.
        /// </summary>
        BranchMapperElement branchMapperElement = null;
        /// <summary>
        /// Branch girth element to set branch girth.
        /// </summary>
        GirthTransformElement girthTransformElement = null;
        /// <summary>
        /// Sprout mapper element to set sprout textures.
        /// </summary>
        SproutMapperElement sproutMapperElement = null;
        /// <summary>
        /// Branch bender element to set branch noise.
        /// </summary>
        BranchBenderElement branchBenderElement = null;
        /// <summary>
        /// Number of branch levels available on the pipeline.
        /// </summary>
        /// <value>Count of branch levels.</value>
        public int branchLevelCount { get; private set; }
        /// <summary>
        /// Number of sprout levels available on the pipeline.
        /// </summary>
        /// <value>Count of sprout levels.</value>
        public int sproutLevelCount { get; private set; }
        /// <summary>
        /// Enum describing the possible materials to apply to a preview.
        /// </summary>
        public enum MaterialMode {
            Composite,
            Albedo,
            Normals,
            Extras,
            Subsurface,
            Mask,
            Thickness
        }
        public Broccoli.Model.BroccoTree snapshotTree = null;
        public Mesh snapshotTreeMesh = null;
        public static Dictionary<int, SnapshotProcessor> _snapshotProcessors = 
            new Dictionary<int, SnapshotProcessor> ();
        /// <summary>
        /// Keeps a reference to a governing bounds of a fragment texture used to calculate UVs on subsequent fragments.
        /// </summary>
        /// <typeparam name="Hash128">Fragment hash.</typeparam>
        /// <typeparam name="Bounds">First bound created for the fragment.</typeparam>
        private Dictionary<Hash128, Bounds> _refFragBounds = new Dictionary<Hash128, Bounds> ();
        /// <summary>
        /// Set to true while exporting to prefab (generate normal textures with non linear gamma).
        /// </summary>
        public bool isPrefabExport = false;
        private static TexturePacker.PackMode defaultPackMode = TexturePacker.PackMode.ScaleEven;
        bool IsLinearColorSpace {
            get {
                #if UNITY_EDITOR 
                return UnityEditor.PlayerSettings.colorSpace != ColorSpace.Linear;
                #else
                return false;
                #endif
            }
        }
        bool hasTransparentTextureBackground = true;
        bool textureDilationEnabled = true;
        #endregion

        #region Constants
        private static Color NORMAL_BG_COLOR = new Color (0.5f, 0.5f, 1f, 1f);
        private static Color EXTRAS_BG_COLOR = new Color (0f, 0f, 1f, 1f);
        private static Color SUBSURFACE_BG_COLOR = new Color (0f, 0f, 0f, 1f);
        #endregion

        #region Texture Vars
        public TextureManager textureManager;
        #endregion

        #region Constructors
        /// <summary>
		/// Static constructor. Registers processors for this factory.
		/// </summary>
		static SproutSubfactory () {
			_snapshotProcessors.Clear ();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				foreach (Type type in assembly.GetTypes()) {
                    SnapshotProcessorAttribute processorAttribute = type.GetCustomAttribute<SnapshotProcessorAttribute> ();
					if (processorAttribute != null) {
						SnapshotProcessor instance = (SnapshotProcessor)Activator.CreateInstance (type);
                        if (!_snapshotProcessors.ContainsKey (processorAttribute.id)) {
						    _snapshotProcessors.Add (processorAttribute.id, instance);
                        }
					}
				}
			}
		}
        #endregion

        #region Factory Initialization and Termination
        /// <summary>
        /// Initializes the subfactory instance.
        /// </summary>
        /// <param name="treeFactory">TreeFactory instance to use to produce branches.</param>
        public void Init (TreeFactory treeFactory) {
            this.treeFactory = treeFactory;
            if (textureManager != null) {
                textureManager.Clear ();
            }
            textureManager = new TextureManager ();
        }
        /// <summary>
        /// Check if there is a valid tree factory assigned to this sprout factory.
        /// </summary>
        /// <returns>True is there is a valid TreeFactory instance.</returns>
        public bool HasValidTreeFactory () {
            return treeFactory != null;
        }
        /// <summary>
        /// Clears data from this instance.
        /// </summary>
        public void Clear () {
            treeFactory = null;
            branchLevels.Clear ();
            sproutALevels.Clear ();
            sproutBLevels.Clear ();
            crownLevels.Clear ();
            sproutLevels.Clear ();
            sproutMeshes.Clear ();
            branchMeshGeneratorElement = null;
            branchMapperElement = null;
            girthTransformElement = null;
            sproutMapperElement = null;
            branchBenderElement = null;
            textureManager.Clear ();
            snapshotTree= null;
            snapshotTreeMesh = null;
        }
        #endregion

        #region Pipeline Load and Analysis
        /// <summary>
        /// Loads a Broccoli pipeline to process branches.
        /// The branch is required to have from 1 to 3 hierarchy levels of branch nodes.
        /// </summary>
        /// <param name="pipeline">Pipeline to load on this subfactory.</param>
        /// <param name="pathToAsset">Path to the asset.</param>
        public void LoadPipeline (Pipeline pipeline, BranchDescriptorCollection branchDescriptorCollection, string pathToAsset) {
            if (treeFactory != null) {
                treeFactory.UnloadAndClearPipeline ();
                treeFactory.LoadPipeline (pipeline.Clone (), pathToAsset, true , true);
                // TODOSS
                // Code to support SproutStructures.

                if (branchDescriptorCollection.snapshots.Count > 0 && branchDescriptorCollection.snapshots[0].sproutStructures.Count == 0) {
                    // Copy Sprout Styles.
                    BranchDescriptorCollection.SproutStyle styleA = branchDescriptorCollection.sproutStyleA.Clone ();
                    styleA.id = Guid.NewGuid ();
                    BranchDescriptorCollection.SproutStyle styleB = branchDescriptorCollection.sproutStyleB.Clone ();
                    styleB.id = Guid.NewGuid ();
                    BranchDescriptorCollection.SproutStyle styleC = branchDescriptorCollection.sproutStyleCrown.Clone ();
                    styleC.id = Guid.NewGuid ();
                    branchDescriptorCollection.sproutStyles.Add (styleA);
                    branchDescriptorCollection.sproutStyles.Add (styleB);
                    bool hasCrown = false;

                    // Copy Sprout A, Sprout B and Sprout Crown to the new SproutStructures.
                    foreach (BranchDescriptor snapshot in branchDescriptorCollection.snapshots) {
                        if (snapshot.sproutStructures.Count == 0) {
                            // Create Sprout A
                            BranchDescriptor.SproutStructure sproutStructureA = new BranchDescriptor.SproutStructure (false);
                            sproutStructureA.submeshIndex = snapshot.sproutASubmeshIndex;
                            sproutStructureA.submeshCount = snapshot.sproutASubmeshCount;
                            sproutStructureA.size = snapshot.sproutASize;
                            sproutStructureA.scaleAtBase = snapshot.sproutAScaleAtBase;
                            sproutStructureA.scaleAtTop = snapshot.sproutAScaleAtTop;
                            sproutStructureA.scaleVariance = snapshot.sproutAScaleVariance;
                            sproutStructureA.scaleMode = snapshot.sproutAScaleMode;
                            sproutStructureA.flipAlign = snapshot.sproutAFlipAlign;
                            sproutStructureA.normalRandomness = snapshot.sproutANormalRandomness;
                            sproutStructureA.bendingAtTop = snapshot.sproutABendingAtTop;
                            sproutStructureA.bendingAtBase = snapshot.sproutABendingAtBase;
                            sproutStructureA.sideBendingAtTop = snapshot.sproutASideBendingAtTop;
                            sproutStructureA.sideBendingAtBase = snapshot.sproutASideBendingAtBase;
                            for (int i = 0; i < snapshot.sproutALevelDescriptors.Count; i++) {
                                sproutStructureA.levelDescriptors.Add (snapshot.sproutALevelDescriptors [i].Clone ());
                            }
                            sproutStructureA.styleId = styleA.id;
                            snapshot.sproutStructures.Add (sproutStructureA);

                            // Create Sprout B
                            BranchDescriptor.SproutStructure sproutStructureB = new BranchDescriptor.SproutStructure (false);
                            sproutStructureB.submeshIndex = snapshot.sproutBSubmeshIndex;
                            sproutStructureB.submeshCount = snapshot.sproutBSubmeshCount;
                            sproutStructureB.size = snapshot.sproutBSize;
                            sproutStructureB.scaleAtBase = snapshot.sproutBScaleAtBase;
                            sproutStructureB.scaleAtTop = snapshot.sproutBScaleAtTop;
                            sproutStructureB.scaleVariance = snapshot.sproutBScaleVariance;
                            sproutStructureB.scaleMode = snapshot.sproutBScaleMode;
                            sproutStructureB.flipAlign = snapshot.sproutBFlipAlign;
                            sproutStructureB.normalRandomness = snapshot.sproutBNormalRandomness;
                            sproutStructureB.bendingAtTop = snapshot.sproutBBendingAtTop;
                            sproutStructureB.bendingAtBase = snapshot.sproutBBendingAtBase;
                            sproutStructureB.sideBendingAtTop = snapshot.sproutBSideBendingAtTop;
                            sproutStructureB.sideBendingAtBase = snapshot.sproutBSideBendingAtBase;
                            for (int i = 0; i < snapshot.sproutBLevelDescriptors.Count; i++) {
                                sproutStructureB.levelDescriptors.Add (snapshot.sproutBLevelDescriptors [i].Clone ());
                            }
                            sproutStructureB.styleId = styleB.id;
                            snapshot.sproutStructures.Add (sproutStructureB);

                            if (snapshot.crownEnabled) {
                                hasCrown = true;
                                // Create Sprout Crown
                                BranchDescriptor.SproutStructure sproutStructureCrown = new BranchDescriptor.SproutStructure (false);
                                sproutStructureCrown.submeshIndex = snapshot.sproutCrownSubmeshIndex;
                                sproutStructureCrown.submeshCount = snapshot.sproutCrownSubmeshCount;
                                sproutStructureCrown.size = snapshot.crownSize;
                                sproutStructureCrown.scaleAtBase = snapshot.crownScaleAtBase;
                                sproutStructureCrown.scaleAtTop = snapshot.crownScaleAtTop;
                                sproutStructureCrown.scaleVariance = snapshot.crownScaleVariance;
                                sproutStructureCrown.scaleMode = snapshot.sproutAScaleMode;
                                sproutStructureCrown.flipAlign = snapshot.sproutAFlipAlign;
                                sproutStructureCrown.normalRandomness = snapshot.sproutANormalRandomness;
                                sproutStructureCrown.bendingAtTop = snapshot.sproutABendingAtTop;
                                sproutStructureCrown.bendingAtBase = snapshot.sproutABendingAtBase;
                                sproutStructureCrown.sideBendingAtTop = snapshot.sproutASideBendingAtTop;
                                sproutStructureCrown.sideBendingAtBase = snapshot.sproutASideBendingAtBase;
                                for (int i = 0; i < snapshot.sproutALevelDescriptors.Count; i++) {
                                    sproutStructureCrown.levelDescriptors.Add (snapshot.sproutALevelDescriptors [i].Clone ());
                                }
                                sproutStructureCrown.styleId = styleC.id;
                                snapshot.sproutStructures.Add (sproutStructureCrown);
                            }
                        }
                    }
                    if (hasCrown) {
                        branchDescriptorCollection.sproutStyles.Add (styleC);
                    }
                }


                this.branchDescriptorCollection = branchDescriptorCollection;
            }
        }
        /// <summary>
        /// Analyzes the loaded pipeline to index the branch and sprout levels to modify using the
        /// BranchDescriptor instance values.
        /// </summary>
        void AnalyzePipeline (BranchDescriptor snapshot = null) {
            branchLevelCount = 0;
            sproutLevelCount = 0;
            branchLevels.Clear ();
            sproutALevels.Clear ();
            sproutBLevels.Clear ();
            sproutLevels.Clear ();
            crownLevels.Clear ();
            sproutMeshes.Clear ();

            // Set working pipeline.
            if (snapshot != null && !string.IsNullOrEmpty (snapshot.snapshotType)) {
                treeFactory.localPipeline.SetPreferredSrcElement (snapshot.snapshotType);
            } else {
                treeFactory.localPipeline.SetPreferredSrcElement ("Main");
            }

            // t structures for branches and sprouts.
            StructureGeneratorElement structureGeneratorElement = (StructureGeneratorElement)treeFactory.localPipeline.root;
            AnalyzePipelineStructure (structureGeneratorElement.rootStructureLevel);

            // Get sprout meshes.
            SproutMeshGeneratorElement sproutMeshGeneratorElement = 
                (SproutMeshGeneratorElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.SproutMeshGenerator);
            if (sproutMeshGeneratorElement != null) {
                for (int i = 0; i < sproutMeshGeneratorElement.sproutMeshes.Count; i++) {
                    sproutMeshes.Add (sproutMeshGeneratorElement.sproutMeshes [i]);
                }
            }

            // Get other elements.
            branchMeshGeneratorElement = 
                (BranchMeshGeneratorElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.BranchMeshGenerator);
            branchMapperElement = 
                (BranchMapperElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.BranchMapper);
            girthTransformElement = 
                (GirthTransformElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.GirthTransform);
            sproutMapperElement = 
                (SproutMapperElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.SproutMapper);
            branchBenderElement = 
                (BranchBenderElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.BranchBender);
        }
        void OnDirectionalBending (BroccoTree tree, BranchBenderElement branchBenderElement) {
            BranchDescriptor snapshot = branchDescriptorCollection.snapshots [snapshotIndex];
            BranchDescriptor.BranchLevelDescriptor branchLevelDesc;
            int branchLevel;
            List<BroccoTree.Branch> allBranches = tree.GetDescendantBranches ();
            for (int i = 0; i < allBranches.Count; i++) {
                branchLevel = allBranches [i].GetLevel();
                if (branchLevel >= 1) {
                    branchLevelDesc = snapshot.branchLevelDescriptors [branchLevel];
                    Vector3 dir = allBranches [i].GetDirectionAtPosition (0f);
                    dir.x = UnityEngine.Random.Range (branchLevelDesc.minPlaneAlignAtBase, branchLevelDesc.maxPlaneAlignAtBase);
                    allBranches [i].ApplyDirectionalLength (dir, allBranches [i].length);
                }
            }
        }
        void AnalyzePipelineStructure (StructureGenerator.StructureLevel structureLevel) {
            if (!structureLevel.isSprout) {
                // Add branch structure level.
                branchLevels.Add (structureLevel);
                branchLevelCount++;

                // TODOSSS
                // Add sprout A structure level.
                StructureGenerator.StructureLevel sproutStructureLevel = structureLevel.GetSproutStructureLevelByGroupId (1);
                if (sproutStructureLevel != null) {
                    sproutALevels.Add (sproutStructureLevel);
                    sproutLevelCount++;
                }
                // Add sprout B structure level.
                sproutStructureLevel = structureLevel.GetSproutStructureLevelByGroupId (2);
                if (sproutStructureLevel != null) {
                    sproutBLevels.Add (sproutStructureLevel);
                }
                // Add crown structure level.
                sproutStructureLevel = structureLevel.GetSproutStructureLevelByGroupId (3);
                if (sproutStructureLevel != null) {
                    crownLevels.Add (sproutStructureLevel);
                }


                // Send the next banch structure level to analysis if found.
                StructureGenerator.StructureLevel branchStructureLevel = 
                    structureLevel.GetFirstBranchStructureLevel ();
                if (branchStructureLevel != null) {
                    AnalyzePipelineStructure (branchStructureLevel);                    
                }
            }
        }
        public void UnloadPipeline () {
            if (treeFactory != null) {
                treeFactory.UnloadAndClearPipeline ();
            }
        }
        #endregion

        #region Pipeline Reflection
        /// <summary>
        /// Reflects the selected SNAPSHOT to a PIPELINE.
        /// </summary>
        public void SnapshotToPipeline () {
            if (snapshotIndex < 0 || branchDescriptorCollection.snapshots.Count == 0) return;

            BranchDescriptor.BranchLevelDescriptor branchLD;
            StructureGenerator.StructureLevel branchSL;

            BranchDescriptor.SproutLevelDescriptor sproutLD;
            StructureGenerator.StructureLevel sproutSL;

            BranchDescriptor snapshot = branchDescriptorCollection.snapshots [snapshotIndex];

            AnalyzePipeline (snapshot);
            ProcessTextures ();

            // Set seed.
            treeFactory.localPipeline.seed = snapshot.seed;

            // Set Factory Scale to 1/10.

            treeFactory.treeFactoryPreferences.factoryScale = factoryScale;

            SnapshotSettings snapshotSettings = SnapshotSettings.Get (snapshot.snapshotType);

            // Update branch girth.
            if (girthTransformElement != null) {
                girthTransformElement.minGirthAtBase = snapshot.girthAtBase;
                girthTransformElement.maxGirthAtBase = snapshot.girthAtBase;
                girthTransformElement.minGirthAtTop = snapshot.girthAtTop;
                girthTransformElement.maxGirthAtTop = snapshot.girthAtTop;
            }
            // Update branch noise.
            if (branchBenderElement) {
                branchBenderElement.noiseType = (BranchBenderElement.NoiseType)snapshot.noiseType;
                float noiseScaleMul = 1f;
                float noiseStrengthMul = 1f;
                if (branchBenderElement.noiseType == BranchBenderElement.NoiseType.Basic) {
                    branchBenderElement.noiseResolution = snapshot.noiseResolution * snapshotSettings.noiseResolution;
                    branchBenderElement.noiseStrength = snapshotSettings.noiseStrength;
                } else {
                    float scaleToGirthScale = Mathf.Lerp (0.4f, 1f, Mathf.InverseLerp (0.025f, 0.003f, snapshot.girthAtBase));
                    noiseScaleMul = 15f * scaleToGirthScale;
                    noiseStrengthMul = 15f * scaleToGirthScale;
                }
                branchBenderElement.noiseAtBase = snapshot.noiseAtBase * noiseStrengthMul;
                branchBenderElement.noiseAtTop = snapshot.noiseAtTop * noiseStrengthMul;
                branchBenderElement.noiseScaleAtBase = snapshot.noiseScaleAtBase * noiseScaleMul;
                branchBenderElement.noiseScaleAtTop = snapshot.noiseScaleAtTop * noiseScaleMul;
                branchBenderElement.onDirectionalBending -= OnDirectionalBending;
                if (snapshotSettings.level1PlaneAlignmentEnabled) {
                    branchBenderElement.onDirectionalBending += OnDirectionalBending;
                }
            }
            // Update snapshot active levels.
            for (int i = 0; i < branchLevels.Count; i++) {
                if (i <= snapshot.activeLevels) {
                    branchLevels [i].enabled = true;
                } else {
                    branchLevels [i].enabled = false;
                }
            }
            // Update branch level descriptors.
            for (int i = 0; i < snapshot.branchLevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    branchLD = snapshot.branchLevelDescriptors [i];
                    branchSL = branchLevels [i];
                    // Disable all children Sprout levels.
                    for (int sprI = 0; sprI < branchSL.structureLevels.Count; sprI++) {
                        if (branchSL.structureLevels[sprI].isSprout) {
                            branchSL.structureLevels[sprI].enabled = false;
                        }
                    }
                    // Pass Values.
                    branchSL.minFrequency = branchLD.minFrequency;
                    branchSL.maxFrequency = branchLD.maxFrequency;
                    branchSL.minRange = branchLD.minRange;
                    branchSL.maxRange = branchLD.maxRange;
                    if (branchSL.minRange > 0f || branchSL.maxRange < 1f) {
                        branchSL.actionRangeEnabled = true;
                    } else {
                        branchSL.actionRangeEnabled = false;
                    }
                    branchSL.distribution = (StructureGenerator.StructureLevel.Distribution)branchLD.distribution;
                    //branchSL.twirlOffset = 0.25f;
                    if (branchSL.distribution == StructureGenerator.StructureLevel.Distribution.Opposite) {
                        branchSL.twirlOffset = 0.25f;
                    } else {
                        branchSL.twirlOffset = 0f;
                    }
                    branchSL.distributionCurve = branchLD.distributionCurve;
                    if (branchDescriptorCollection.descriptorImplId == 0) {
                        branchSL.radius = 0;
                    } else {
                        branchSL.radius = branchLD.radius;
                    }
                    branchSL.minLengthAtBase = branchLD.minLengthAtBase;
                    branchSL.maxLengthAtBase = branchLD.maxLengthAtBase;
                    branchSL.minLengthAtTop = branchLD.minLengthAtTop;
                    branchSL.maxLengthAtTop = branchLD.maxLengthAtTop;
                    branchSL.lengthCurve = branchLD.lengthCurve;
                    branchSL.distributionSpacingVariance = branchLD.spacingVariance;
                    branchSL.minParallelAlignAtBase = branchLD.minParallelAlignAtBase;
                    branchSL.maxParallelAlignAtBase = branchLD.maxParallelAlignAtBase;
                    branchSL.minParallelAlignAtTop = branchLD.minParallelAlignAtTop;
                    branchSL.maxParallelAlignAtTop = branchLD.maxParallelAlignAtTop;
                    branchSL.minGravityAlignAtBase = branchLD.minGravityAlignAtBase;
                    branchSL.maxGravityAlignAtBase = branchLD.maxGravityAlignAtBase;
                    branchSL.minGravityAlignAtTop = branchLD.minGravityAlignAtTop;
                    branchSL.maxGravityAlignAtTop = branchLD.maxGravityAlignAtTop;
                }
            }
            // Update branch mesh generator.
            if (branchMeshGeneratorElement != null) {
                branchMeshGeneratorElement.minCurveStepsPerUnit = 90f;
                branchMeshGeneratorElement.maxCurveStepsPerUnit = 180f;
            }
            // Update branch mapping textures.
            if (branchMapperElement != null) {
                branchMapperElement.mainTexture = branchDescriptorCollection.branchAlbedoTexture;
                branchMapperElement.normalTexture = branchDescriptorCollection.branchNormalTexture;
                branchMapperElement.mappingYDisplacement = branchDescriptorCollection.branchTextureYDisplacement;
            }







            for (int sproutStructureI = 0; sproutStructureI < snapshot.sproutStructures.Count; sproutStructureI++)
            {
                int groupId = sproutStructureI + 1;
                BranchDescriptor.SproutStructure sproutStructure = snapshot.sproutStructures [sproutStructureI];

                for (int levelIndex = 0; levelIndex < sproutStructure.levelDescriptors.Count; levelIndex++) {
                    if (levelIndex < branchLevelCount) {
                        sproutLD = sproutStructure.levelDescriptors [levelIndex];
                        sproutSL = branchLevels [levelIndex].GetSproutStructureLevelByGroupId (groupId);
                        if (sproutSL != null) {
                            // Pass Values.
                            sproutSL.enabled = sproutLD.isEnabled;
                            sproutSL.minFrequency = sproutLD.minFrequency;
                            sproutSL.maxFrequency = sproutLD.maxFrequency;
                            sproutSL.minParallelAlignAtBase = sproutLD.minParallelAlignAtBase;
                            sproutSL.maxParallelAlignAtBase = sproutLD.maxParallelAlignAtBase;
                            sproutSL.minParallelAlignAtTop = sproutLD.minParallelAlignAtTop;
                            sproutSL.maxParallelAlignAtTop = sproutLD.maxParallelAlignAtTop;
                            sproutSL.minGravityAlignAtBase = sproutLD.minGravityAlignAtBase;
                            sproutSL.maxGravityAlignAtBase = sproutLD.maxGravityAlignAtBase;
                            sproutSL.minGravityAlignAtTop = sproutLD.minGravityAlignAtTop;
                            sproutSL.maxGravityAlignAtTop = sproutLD.maxGravityAlignAtTop;
                            sproutSL.flipSproutAlign = sproutStructure.flipAlign;
                            sproutSL.normalSproutRandomness = sproutStructure.normalRandomness;
                            sproutSL.actionRangeEnabled = true;
                            sproutSL.minRange = sproutLD.minRange;
                            sproutSL.maxRange = sproutLD.maxRange;
                            sproutSL.distribution = (StructureGenerator.StructureLevel.Distribution)sproutLD.distribution;
                            if (sproutSL.distribution == StructureGenerator.StructureLevel.Distribution.Alternative) {
                                sproutSL.minTwirl = 0f;
                                sproutSL.maxTwirl = 0f;
                                //sproutSL.twirlOffset = 0.5f;
                                sproutSL.twirlOffset = 0f;
                            } else {
                                sproutSL.minTwirl = 0.5f;
                                sproutSL.maxTwirl = 0.5f;
                                //sproutSL.twirlOffset = 0.75f;
                                sproutSL.twirlOffset = 0.5f;
                            }
                            sproutSL.distributionCurve = sproutLD.distributionCurve;
                            sproutSL.distributionSpacingVariance = sproutLD.spacingVariance;
                        } else {
                            Debug.LogWarning ("SproutStructure not found to reflect at the Pipeline.");
                        }
                    }
                }
                // Update sprout mesh properties.
                if (sproutMeshes.Count > 0) {
                    sproutMeshes [sproutStructureI].width = sproutStructure.size;
                    sproutMeshes [sproutStructureI].scaleAtBase = sproutStructure.scaleAtBase;
                    sproutMeshes [sproutStructureI].scaleAtTop = sproutStructure.scaleAtTop;
                    sproutMeshes [sproutStructureI].scaleVariance = sproutStructure.scaleVariance;
                    sproutMeshes [sproutStructureI].scaleMode = sproutStructure.scaleMode;
                    sproutMeshes [sproutStructureI].gravityBendingAtBase = sproutStructure.bendingAtBase;
                    sproutMeshes [sproutStructureI].gravityBendingAtTop = sproutStructure.bendingAtTop;
                    sproutMeshes [sproutStructureI].sideGravityBendingAtBase = sproutStructure.sideBendingAtBase;
                    sproutMeshes [sproutStructureI].sideGravityBendingAtTop = sproutStructure.sideBendingAtTop;
                    sproutMeshes [sproutStructureI].noisePattern = sproutStructure.noisePattern;
                    sproutMeshes [sproutStructureI].noiseDistribution = sproutStructure.noiseDistribution;
                    sproutMeshes [sproutStructureI].noiseResolutionAtBase = sproutStructure.noiseResolutionAtBase;
                    sproutMeshes [sproutStructureI].noiseResolutionAtTop = sproutStructure.noiseResolutionAtTop;
                    sproutMeshes [sproutStructureI].noiseResolutionVariance = sproutStructure.noiseResolutionVariance;
                    sproutMeshes [sproutStructureI].noiseResolutionCurve = sproutStructure.noiseResolutionCurve;
                    sproutMeshes [sproutStructureI].noiseStrengthAtBase = sproutStructure.noiseStrengthAtBase;
                    sproutMeshes [sproutStructureI].noiseStrengthAtTop = sproutStructure.noiseStrengthAtTop;
                    sproutMeshes [sproutStructureI].noiseStrengthVariance = sproutStructure.noiseStrengthVariance;
                    sproutMeshes [sproutStructureI].noiseStrengthCurve = sproutStructure.noiseStrengthCurve;
                }

                // Update sprout mapping textures.
                if (sproutMapperElement != null) {
                    BranchDescriptorCollection.SproutStyle sproutStyle = branchDescriptorCollection.GetSproutStyle (sproutStructure.styleId);
                    if (sproutStyle != null) {
                        sproutMapperElement.sproutMaps [sproutStructureI].alphaCutoff = 0f;
                        sproutMapperElement.sproutMaps [sproutStructureI].colorVarianceMode = SproutMap.ColorVarianceMode.Shades;
                        sproutMapperElement.sproutMaps [sproutStructureI].minColorShade = sproutStyle.minColorShade;
                        sproutMapperElement.sproutMaps [sproutStructureI].maxColorShade = sproutStyle.maxColorShade;
                        sproutMapperElement.sproutMaps [sproutStructureI].shadeMode =
                            (SproutMap.ShadeMode)sproutStyle.sproutShadeMode;
                        sproutMapperElement.sproutMaps [sproutStructureI].shadeVariance = sproutStyle.sproutShadeVariance;
                        sproutMapperElement.sproutMaps [sproutStructureI].invertShadeMode = sproutStyle.invertSproutShadeMode;
                        if (Broccoli.Base.GlobalSettings.experimentalSproutLabDissolveSprouts) {
                            sproutMapperElement.sproutMaps [sproutStructureI].alphaVarianceMode = SproutMap.AlphaVarianceMode.Dissolve;
                            sproutMapperElement.sproutMaps [sproutStructureI].minColorDissolve = sproutStyle.minColorDissolve;
                            sproutMapperElement.sproutMaps [sproutStructureI].maxColorDissolve = sproutStyle.maxColorDissolve;
                            sproutMapperElement.sproutMaps [sproutStructureI].dissolveMode =
                                (SproutMap.DissolveMode)sproutStyle.sproutDissolveMode;
                            sproutMapperElement.sproutMaps [sproutStructureI].dissolveVariance = sproutStyle.sproutDissolveVariance;
                            sproutMapperElement.sproutMaps [sproutStructureI].invertDissolveMode = sproutStyle.invertSproutDissolveMode;
                        } else {
                            sproutMapperElement.sproutMaps [sproutStructureI].alphaVarianceMode = SproutMap.AlphaVarianceMode.None;
                        }
                        sproutMapperElement.sproutMaps [sproutStructureI].colorTintEnabled = true;
                        sproutMapperElement.sproutMaps [sproutStructureI].colorTint = sproutStyle.colorTint;
                        sproutMapperElement.sproutMaps [sproutStructureI].minColorTint = sproutStyle.minColorTint;
                        sproutMapperElement.sproutMaps [sproutStructureI].maxColorTint = sproutStyle.maxColorTint;
                        sproutMapperElement.sproutMaps [sproutStructureI].metallic = sproutStyle.metallic;
                        sproutMapperElement.sproutMaps [sproutStructureI].glossiness = sproutStyle.glossiness;
                        //sproutMapperElement.sproutMaps [sproutStructureI].subsurfaceValue = 0.5f + Mathf.Lerp (-0.4f, 0.4f, sproutStyle.subsurface - 0.5f);
                        sproutMapperElement.sproutMaps [sproutStructureI].subsurfaceColor = GetSubsurfaceColor (sproutStyle.subsurface);
                        sproutMapperElement.sproutMaps [sproutStructureI].sproutAreas.Clear ();
                        for (int i = 0; i < sproutStyle.sproutMapAreas.Count; i++) {
                            SproutMap.SproutMapArea sma = sproutStyle.sproutMapAreas [i].Clone ();
                            // Forced sproutstyle to sproutgroup
                            sma.texture = GetSproutTexture (sproutStructureI, i);
                            sproutMapperElement.sproutMaps [sproutStructureI].sproutAreas.Add (sma);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Loads values from a PIPELINE to a SNAPSHOT.
        /// </summary>
        /// <param name="snapshot"></param>
        // TODOOOOO
        public void PipelineToSnapshot (BranchDescriptor snapshot) {

            BranchDescriptor.BranchLevelDescriptor branchLD;
            StructureGenerator.StructureLevel branchSL;
            BranchDescriptor.SproutLevelDescriptor sproutALD;
            StructureGenerator.StructureLevel sproutASL;
            BranchDescriptor.SproutLevelDescriptor sproutBLD;
            StructureGenerator.StructureLevel sproutBSL;

            // Setting for the snapshot.
            SnapshotSettings snapshotSettings = SnapshotSettings.Get (snapshot.snapshotType);
            snapshot.activeLevels = snapshotSettings.defaultActiveLevels;
            snapshot.processorId = snapshotSettings.processorId;

            AnalyzePipeline (snapshot);

            // Update branch girth.
            if (girthTransformElement != null) {
                snapshot.girthAtBase = girthTransformElement.minGirthAtBase;
                snapshot.girthAtBase = girthTransformElement.maxGirthAtBase;
                snapshot.girthAtTop = girthTransformElement.minGirthAtTop;
                snapshot.girthAtTop = girthTransformElement.maxGirthAtTop;
            }
            // Update branch noise.
            if (branchBenderElement) {
                snapshot.noiseType = (BranchDescriptor.NoiseType)branchBenderElement.noiseType;
                snapshot.noiseResolution = branchBenderElement.noiseResolution;
                snapshot.noiseAtBase = branchBenderElement.noiseAtBase;
                snapshot.noiseAtTop = branchBenderElement.noiseAtTop;
                snapshot.noiseScaleAtBase = branchBenderElement.noiseScaleAtBase;
                snapshot.noiseScaleAtTop = branchBenderElement.noiseScaleAtTop;
            }
            /*
            // Update snapshot active levels.
            for (int i = 0; i < branchLevels.Count; i++) {
                if (i <= branchDescriptor.activeLevels) {
                    branchLevels [i].enabled = true;
                } else {
                    branchLevels [i].enabled = false;
                }
            }
            */
            // Update branch level descriptors.
            for (int i = 0; i < snapshot.branchLevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    branchLD = snapshot.branchLevelDescriptors [i];
                    branchSL = branchLevels [i];
                    // Pass Values.
                    branchLD.minFrequency = branchSL.minFrequency;
                    branchLD.maxFrequency = branchSL.maxFrequency;
                    branchLD.minRange = branchSL.minRange;
                    branchLD.maxRange = branchSL.maxRange;
                    branchLD.distributionCurve = branchSL.distributionCurve;
                    if (branchDescriptorCollection.descriptorImplId == 0) {
                        branchLD.radius = 0;
                    } else {
                        branchLD.radius = branchSL.radius;
                    }
                    branchLD.minLengthAtBase        = branchSL.minLengthAtBase;
                    branchLD.maxLengthAtBase        = branchSL.maxLengthAtBase;
                    branchLD.minLengthAtTop         = branchSL.minLengthAtTop;
                    branchLD.maxLengthAtTop         = branchSL.maxLengthAtTop;
                    branchLD.lengthCurve            = branchSL.lengthCurve;
                    branchLD.spacingVariance         = branchSL.distributionSpacingVariance;
                    branchLD.minParallelAlignAtBase = branchSL.minParallelAlignAtBase;
                    branchLD.maxParallelAlignAtBase = branchSL.maxParallelAlignAtBase;
                    branchLD.minParallelAlignAtTop  = branchSL.minParallelAlignAtTop;
                    branchLD.maxParallelAlignAtTop  = branchSL.maxParallelAlignAtTop;
                    branchLD.minGravityAlignAtBase  = branchSL.minGravityAlignAtBase;
                    branchLD.maxGravityAlignAtBase  = branchSL.maxGravityAlignAtBase;
                    branchLD.minGravityAlignAtTop   = branchSL.minGravityAlignAtTop;
                    branchLD.maxGravityAlignAtTop   = branchSL.maxGravityAlignAtTop;
                }
            }
            /*
            // Update branch mapping textures.
            if (branchMapperElement != null) {
                branchMapperElement.mainTexture = branchDescriptorCollection.branchAlbedoTexture;
                branchMapperElement.normalTexture = branchDescriptorCollection.branchNormalTexture;
                branchMapperElement.mappingYDisplacement = branchDescriptorCollection.branchTextureYDisplacement;
            }
            */
            // Update sprout A level descriptors.
            for (int i = 0; i < snapshot.sproutALevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    sproutALD = snapshot.sproutALevelDescriptors [i];
                    sproutASL = sproutALevels [i];
                    // Pass Values.
                    sproutALD.isEnabled = sproutASL.enabled;
                    sproutALD.minFrequency = sproutASL.minFrequency;
                    sproutALD.maxFrequency = sproutASL.maxFrequency;
                    sproutALD.minParallelAlignAtBase = sproutASL.minParallelAlignAtBase;
                    sproutALD.maxParallelAlignAtBase = sproutASL.maxParallelAlignAtBase;
                    sproutALD.minParallelAlignAtTop = sproutASL.minParallelAlignAtTop;
                    sproutALD.maxParallelAlignAtTop = sproutASL.maxParallelAlignAtTop;
                    sproutALD.minGravityAlignAtBase = sproutASL.minGravityAlignAtBase;
                    sproutALD.maxGravityAlignAtBase = sproutASL.maxGravityAlignAtBase;
                    sproutALD.minGravityAlignAtTop = sproutASL.minGravityAlignAtTop;
                    sproutALD.maxGravityAlignAtTop = sproutASL.maxGravityAlignAtTop;
                    snapshot.sproutAFlipAlign = sproutASL.flipSproutAlign;
                    snapshot.sproutANormalRandomness = sproutASL.normalSproutRandomness;
                    //sproutALD.actionRangeEnabled = true;
                    sproutALD.minRange = sproutASL.minRange;
                    sproutALD.maxRange = sproutASL.maxRange;
                    sproutALD.distribution = (BranchDescriptor.SproutLevelDescriptor.Distribution)sproutASL.distribution;
                    sproutALD.distributionCurve = sproutASL.distributionCurve;
                    sproutALD.spacingVariance = sproutASL.distributionSpacingVariance;
                }
            }
            // Update sprout A properties.
            if (sproutMeshes.Count > 0) {
                snapshot.sproutASize = sproutMeshes [0].width;
                snapshot.sproutAScaleAtBase = sproutMeshes [0].scaleAtBase;
                snapshot.sproutAScaleAtTop = sproutMeshes [0].scaleAtTop;
                snapshot.sproutAScaleVariance = sproutMeshes [0].scaleVariance;
                snapshot.sproutAScaleMode = sproutMeshes [0].scaleMode;
                snapshot.sproutABendingAtBase = sproutMeshes [0].gravityBendingAtBase;
                snapshot.sproutABendingAtTop = sproutMeshes [0].gravityBendingAtTop;
                snapshot.sproutASideBendingAtBase = sproutMeshes [0].sideGravityBendingAtBase;
                snapshot.sproutASideBendingAtTop = sproutMeshes [0].sideGravityBendingAtTop;
            }
            /*
            // Update sprout mapping textures.
            if (sproutMapperElement != null) {
                sproutMapperElement.sproutMaps [0].colorVarianceMode = SproutMap.ColorVarianceMode.Shades;
                sproutMapperElement.sproutMaps [0].minColorShade = branchDescriptorCollection.sproutStyleA.minColorShade;
                sproutMapperElement.sproutMaps [0].maxColorShade = branchDescriptorCollection.sproutStyleA.maxColorShade;
                sproutMapperElement.sproutMaps [0].colorTintEnabled = true;
                sproutMapperElement.sproutMaps [0].colorTint = branchDescriptorCollection.sproutStyleA.colorTint;
                sproutMapperElement.sproutMaps [0].minColorTint = branchDescriptorCollection.sproutStyleA.minColorTint;
                sproutMapperElement.sproutMaps [0].maxColorTint = branchDescriptorCollection.sproutStyleA.maxColorTint;
                sproutMapperElement.sproutMaps [0].metallic = branchDescriptorCollection.sproutStyleA.metallic;
                sproutMapperElement.sproutMaps [0].glossiness = branchDescriptorCollection.sproutStyleA.glossiness;
                sproutMapperElement.sproutMaps [0].subsurfaceValue = 0.5f + Mathf.Lerp (-0.4f, 0.4f, branchDescriptorCollection.sproutStyleA.subsurfaceMul - 0.5f);
                sproutMapperElement.sproutMaps [0].sproutAreas.Clear ();
                for (int i = 0; i < branchDescriptorCollection.sproutAMapAreas.Count; i++) {
                    SproutMap.SproutMapArea sma = branchDescriptorCollection.sproutAMapAreas [i].Clone ();
                    sma.texture = GetSproutTexture (0, i);
                    sproutMapperElement.sproutMaps [0].sproutAreas.Add (sma);
                }
            }
            */

            // Update sprout B level descriptors.
            for (int i = 0; i < snapshot.sproutBLevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    sproutBLD = snapshot.sproutBLevelDescriptors [i];
                    sproutBSL = sproutBLevels [i];
                    // Pass Values.
                    sproutBLD.isEnabled = sproutBSL.enabled;
                    sproutBLD.minFrequency = sproutBSL.minFrequency;
                    sproutBLD.maxFrequency = sproutBSL.maxFrequency;
                    sproutBLD.minParallelAlignAtBase = sproutBSL.minParallelAlignAtBase;
                    sproutBLD.maxParallelAlignAtBase = sproutBSL.maxParallelAlignAtBase;
                    sproutBLD.minParallelAlignAtTop = sproutBSL.minParallelAlignAtTop;
                    sproutBLD.maxParallelAlignAtTop = sproutBSL.maxParallelAlignAtTop;
                    sproutBLD.minGravityAlignAtBase = sproutBSL.minGravityAlignAtBase;
                    sproutBLD.maxGravityAlignAtBase = sproutBSL.maxGravityAlignAtBase;
                    sproutBLD.minGravityAlignAtTop = sproutBSL.minGravityAlignAtTop;
                    sproutBLD.maxGravityAlignAtTop = sproutBSL.maxGravityAlignAtTop;
                    snapshot.sproutBFlipAlign = sproutBSL.flipSproutAlign;
                    snapshot.sproutBNormalRandomness = sproutBSL.normalSproutRandomness;
                    //sproutBLD.actionRangeEnabled = true;
                    sproutBLD.minRange = sproutBSL.minRange;
                    sproutBLD.maxRange = sproutBSL.maxRange;
                    sproutBLD.distribution = (BranchDescriptor.SproutLevelDescriptor.Distribution)sproutBSL.distribution;
                    sproutBLD.distributionCurve = sproutBSL.distributionCurve;
                    sproutBLD.spacingVariance = sproutBSL.distributionSpacingVariance;
                }
            }
            // Update sprout B properties.
            if (sproutMeshes.Count > 1) {
                snapshot.sproutBSize = sproutMeshes [1].width;
                snapshot.sproutBScaleAtBase = sproutMeshes [1].scaleAtBase;
                snapshot.sproutBScaleAtTop = sproutMeshes [1].scaleAtTop;
                snapshot.sproutBScaleVariance = sproutMeshes [1].scaleVariance;
                snapshot.sproutBScaleMode = sproutMeshes [1].scaleMode;
                snapshot.sproutBBendingAtBase = sproutMeshes [1].gravityBendingAtBase;
                snapshot.sproutBBendingAtTop = sproutMeshes [1].gravityBendingAtTop;
                snapshot.sproutBSideBendingAtBase = sproutMeshes [1].sideGravityBendingAtBase;
                snapshot.sproutBSideBendingAtTop = sproutMeshes [1].sideGravityBendingAtTop;
            }
            
            /*
            // Update sprout mapping textures.
            if (sproutMapperElement != null && sproutMapperElement.sproutMaps.Count > 1) {
                sproutMapperElement.sproutMaps [1].colorVarianceMode =  SproutMap.ColorVarianceMode.Shades;
                sproutMapperElement.sproutMaps [1].minColorShade = branchDescriptorCollection.sproutStyleB.minColorShade;
                sproutMapperElement.sproutMaps [1].maxColorShade = branchDescriptorCollection.sproutStyleB.maxColorShade;
                sproutMapperElement.sproutMaps [1].colorTintEnabled = true;
                sproutMapperElement.sproutMaps [1].colorTint = branchDescriptorCollection.sproutStyleB.colorTint;
                sproutMapperElement.sproutMaps [1].minColorTint = branchDescriptorCollection.sproutStyleB.minColorTint;
                sproutMapperElement.sproutMaps [1].maxColorTint = branchDescriptorCollection.sproutStyleB.maxColorTint;
                sproutMapperElement.sproutMaps [1].metallic = branchDescriptorCollection.sproutStyleB.metallic;
                sproutMapperElement.sproutMaps [1].glossiness = branchDescriptorCollection.sproutStyleB.glossiness;
                sproutMapperElement.sproutMaps [1].subsurfaceValue = 0.5f + Mathf.Lerp (-0.4f, 0.4f, branchDescriptorCollection.sproutStyleB.subsurfaceMul - 0.5f);
                sproutMapperElement.sproutMaps [1].sproutAreas.Clear ();
                for (int i = 0; i < branchDescriptorCollection.sproutBMapAreas.Count; i++) {
                    SproutMap.SproutMapArea sma = branchDescriptorCollection.sproutBMapAreas [i].Clone ();
                    sma.texture = GetSproutTexture (1, i);
                    sproutMapperElement.sproutMaps [1].sproutAreas.Add (sma);
                }
            }
            */

            // Update crown properties.
            if (sproutMeshes.Count > 2) {
                snapshot.crownDepth = sproutMeshes[2].depth;
                snapshot.crownSize = sproutMeshes[2].width;
                snapshot.crownScaleAtBase = sproutMeshes[2].scaleAtBase;
                snapshot.crownScaleAtTop = sproutMeshes[2].scaleAtTop;
                snapshot.crownScaleVariance = sproutMeshes[2].scaleVariance;
            }
        }
        private void OnBeforeGenerateCrownedLevel (
            StructureGenerator.StructureLevel structureLevel, 
            StructureGenerator.Structure parentStructure)
        {
            StructureGenerator.StructureLevel crownStructureLevel = structureLevel.parentStructureLevel.GetSproutStructureLevelByGroupId (3);
            float randomVal = UnityEngine.Random.Range (0f, 1f);
            BranchDescriptor snapshot = (BranchDescriptor)structureLevel.obj;
            if (randomVal <= snapshot.crownProbability) {
                crownStructureLevel.enabled = true;
                structureLevel.actionRangeEnabled = true;
                crownStructureLevel.actionRangeEnabled = true;
                float range = UnityEngine.Random.Range (snapshot.crownMinRange, snapshot.crownMaxRange);
                crownStructureLevel.minRange = 1f - range;
                structureLevel.maxRange = 1f - range;
                if (structureLevel.minRange > structureLevel.maxRange) {
                    structureLevel.minRange = structureLevel.maxRange;
                }
                crownStructureLevel.minFrequency = snapshot.crownMinFrequency;
                crownStructureLevel.maxFrequency = snapshot.crownMaxFrequency;
            } else {
                crownStructureLevel.enabled = false;
                /*
                structureLevel.actionRangeEnabled = false;
                structureLevel.maxRange = 1f;
                */
            }
        }
    
        /// <summary>
        /// Gets a color to apply as subsurface on a material.
        /// </summary>
        /// <param name="subsurface">Subsurface value from 0 to 1.5.</param>
        /// <returns>Subsurface color.</returns>
        private Color GetSubsurfaceColor (float subsurface) {
            float normalizedSubsurface = Mathf.InverseLerp (0f, 1.5f, subsurface);
            //float colorVal = Mathf.Lerp (0.5f, 1f, normalizedSubsurface);
            float colorVal = normalizedSubsurface;
            Color subsurfaceColor = new Color (colorVal, colorVal, colorVal, 1f);
            return subsurfaceColor;
        }
        #endregion

        #region Snapshot Processing
        /*
        /// <summary>
        /// Regenerates a preview for the loaded snapshot.
        /// </summary>
        /// <param name="materialMode">Materials mode to apply.</param>
        /// <param name="isNewSeed"><c>True</c> to create a new preview (new seed).</param>
        public void ProcessSnapshot (int snapshotIndex, MaterialMode materialMode = MaterialMode.Composite, bool isNewSeed = false) {
            int _branchDescriptorIndex = this.snapshotIndex;
            this.snapshotIndex = snapshotIndex;
            ProcessSnapshot (materialMode, isNewSeed);
            // TODO: selectively reprocess.
            this.snapshotIndex = _branchDescriptorIndex;
        }
        */
        /// <summary>
        /// Regenerates a preview for the loaded snapshot.
        /// </summary>
        /// <param name="materialMode">Materials mode to apply.</param>
        /// <param name="isNewSeed"><c>True</c> to create a new preview (new seed).</param>
        public void ProcessSnapshot (int snapshotIndex, bool hasChanged = false, MaterialMode materialMode = MaterialMode.Composite, bool isNewSeed = false) {
            this.snapshotIndex = snapshotIndex;

            if (snapshotIndex < 0 || snapshotIndex >= branchDescriptorCollection.snapshots.Count) return;

            BranchDescriptor snapshot = branchDescriptorCollection.snapshots [snapshotIndex];
            
            if (!isNewSeed) {
                treeFactory.localPipeline.seed = snapshot.seed;
                treeFactory.ProcessPipelinePreview (null, true, true);
            } else {
                treeFactory.ProcessPipelinePreview ();
                snapshot.seed = treeFactory.localPipeline.seed;
            }
            // Set submesh indexes index.
            SetSnapshotSubmeshIndexes (snapshot, treeFactory);

            if (hasChanged)
                sproutCompositeManager.RemoveSnapshot (snapshot);

            if (GlobalSettings.showSproutLabTreeFactoryInHierarchy) {
                treeFactory.previewTree.obj.SetActive (true);
            } else {
                treeFactory.previewTree.obj.SetActive (false);
            }

            // Get materials.
            MeshRenderer meshRenderer = treeFactory.previewTree.obj.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = treeFactory.previewTree.obj.GetComponent<MeshFilter> ();
            Material[] compositeMaterials = meshRenderer.sharedMaterials;
            if (materialMode == MaterialMode.Albedo) { // Albedo
                meshRenderer.sharedMaterials = GetAlbedoMaterials (
                    snapshot, compositeMaterials,
                    branchDescriptorCollection.branchColorShade,
                    branchDescriptorCollection.branchColorSaturation);
            } else if (materialMode == MaterialMode.Normals) { // Normals
                meshRenderer.sharedMaterials = GetNormalMaterials (compositeMaterials, isPrefabExport?true:false);
            } else if (materialMode == MaterialMode.Extras) { // Extras
                meshRenderer.sharedMaterials = GetExtraMaterials (
                    snapshot,
                    compositeMaterials);
            } else if (materialMode == MaterialMode.Subsurface) { // Subsurface
                meshRenderer.sharedMaterials = GetSubsurfaceMaterials (
                    snapshot,
                    compositeMaterials,
                    branchDescriptorCollection.branchColorSaturation,
                    branchDescriptorCollection.branchSubsurface
                    );
            } else if (materialMode == MaterialMode.Composite) { // Composite
                meshRenderer.sharedMaterials = GetCompositeMaterials (
                    snapshot,
                    compositeMaterials);
            }

            snapshotTree = treeFactory.previewTree;
            snapshotTreeMesh = meshFilter.sharedMesh;
        }
        /// <summary>
        /// Sets the index for submeshes belonging to sprouts on the snapshot.
        /// </summary>
        /// <param name="snapshot">Snapshot to set info about the submesh indexes.</param>
        /// <param name="treeFactory">TreeFactory instance that produced the snapshot.</param>
        private void SetSnapshotSubmeshIndexes (BranchDescriptor snapshot, TreeFactory treeFactory) {
            snapshot.sproutASubmeshIndex = -1;
            snapshot.sproutBSubmeshIndex = -1;
            snapshot.sproutCrownSubmeshIndex = -1;
            snapshot.sproutASubmeshCount = 0;
            snapshot.sproutBSubmeshCount = 0;
            snapshot.sproutCrownSubmeshCount = 0;
            foreach (BranchDescriptor.SproutStructure sproutStructure in snapshot.sproutStructures) {
                sproutStructure.submeshIndex = -1;
                sproutStructure.submeshCount = 0;
            }
            Dictionary<int, MeshManager.MeshData> meshDatas = treeFactory.meshManager.GetMeshDatas ();
            var enumMeshDatas = meshDatas.GetEnumerator ();
            MeshManager.MeshData meshData;
            int submeshIndex = 0;
            while (enumMeshDatas.MoveNext ()) {
                meshData = enumMeshDatas.Current.Value;
                if (meshData.type == MeshManager.MeshData.Type.Sprout) {
                    for (int sproutStructureI = 0; sproutStructureI < snapshot.sproutStructures.Count; sproutStructureI++) {
                        if (sproutStructureI + 1 == meshData.groupId) {
                            BranchDescriptor.SproutStructure sproutStructure = snapshot.sproutStructures[sproutStructureI];
                            sproutStructure.submeshCount++;
                            if (sproutStructure.submeshIndex == -1)
                                sproutStructure.submeshIndex = submeshIndex;
                            break;
                        }
                    }
                    /*
                    if (meshData.groupId == 1) {
                        snapshot.sproutASubmeshCount++;
                        if (snapshot.sproutASubmeshIndex == -1)
                            snapshot.sproutASubmeshIndex = submeshIndex;
                    }
                    if (meshData.groupId == 2 && snapshot.sproutBSubmeshIndex == -1) {
                        snapshot.sproutBSubmeshCount++;
                        if (snapshot.sproutBSubmeshIndex == -1)
                            snapshot.sproutBSubmeshIndex = submeshIndex;
                    }
                    if (meshData.groupId == 3 && snapshot.sproutCrownSubmeshIndex == -1) {
                        snapshot.sproutCrownSubmeshCount++;
                        if (snapshot.sproutCrownSubmeshIndex == -1)
                            snapshot.sproutCrownSubmeshIndex = submeshIndex;
                    }
                    */
                }
                submeshIndex++;
            }
            // TODOOOOOOOOOOOOOOOO
            // TODOOOOOOOOOOOOOOOO
            // TODOOOOOOOOOOOOOOOO
            /*
            snapshot.sproutASubmeshIndex = -1;
            snapshot.sproutBSubmeshIndex = -1;
            snapshot.sproutCrownSubmeshIndex = -1;
            snapshot.sproutASubmeshCount = 0;
            snapshot.sproutBSubmeshCount = 0;
            snapshot.sproutCrownSubmeshCount = 0;
            Dictionary<int, MeshManager.MeshData> meshDatas = treeFactory.meshManager.GetMeshDatas ();
            var enumMeshDatas = meshDatas.GetEnumerator ();
            MeshManager.MeshData meshData;
            int submeshIndex = 0;
            while (enumMeshDatas.MoveNext ()) {
                meshData = enumMeshDatas.Current.Value;
                if (meshData.type == MeshManager.MeshData.Type.Sprout) {
                    if (meshData.groupId == 1) {
                        snapshot.sproutASubmeshCount++;
                        if (snapshot.sproutASubmeshIndex == -1)
                            snapshot.sproutASubmeshIndex = submeshIndex;
                    }
                    if (meshData.groupId == 2 && snapshot.sproutBSubmeshIndex == -1) {
                        snapshot.sproutBSubmeshCount++;
                        if (snapshot.sproutBSubmeshIndex == -1)
                            snapshot.sproutBSubmeshIndex = submeshIndex;
                    }
                    if (meshData.groupId == 3 && snapshot.sproutCrownSubmeshIndex == -1) {
                        snapshot.sproutCrownSubmeshCount++;
                        if (snapshot.sproutCrownSubmeshIndex == -1)
                            snapshot.sproutCrownSubmeshIndex = submeshIndex;
                    }
                }
                submeshIndex++;
            }
            */
        }
        /// <summary>
        /// Creates the polygons for a snapshot. It saves their textures to the
        /// snapshotTextures buffer.
        /// It should be called with after ProcessSnapshot to have the mesh, materials and tree
        /// corresponding to the last snapshot processed.
        /// </summary>
        /// <param name="snapshot">Branch Descriptor to process as snapshot.</param>
        public void ProcessSnapshotPolygons (BranchDescriptor snapshot, bool force = false) {
            // Validate the snapshot has been processed.
            if (!sproutCompositeManager.HasSnapshot (snapshot.id) || force) {
                SnapshotSettings snapshotSettings = SnapshotSettings.Get (snapshot.snapshotType);

                // Generate curve.
                GenerateSnapshotCurve (snapshot, snapshotSettings);

                // Clear reference bounds.
                _refFragBounds.Clear ();

                // Generate polygons per LOD.
                snapshot.polygonAreas.Clear ();
                for (int lodLevel = snapshot.lodCount - 1; lodLevel >= 0; lodLevel--) {
                    GenerateSnapshotPolygonsPerLOD (lodLevel, snapshot);
                }

                sproutCompositeManager.AddSnapshot (snapshot);
                _refFragBounds.Clear ();
            }
        }
        /// <summary>
        /// Gets a snapshot processor given and id.
        /// </summary>
        /// <param name="processorId">Snapshot processor id.</param>
        /// <returns>Processor instance or null if not found.</returns>
        public SnapshotProcessor GetSnapshotProcessor (int processorId) {
            if (_snapshotProcessors.ContainsKey (processorId)) {
                return _snapshotProcessors [processorId];
            }
            return null;
        }
        /// <summary>
        /// Creates the curve as axis for the snapshot of a snapshot.
        /// </summary>
        /// <param name="snapshot">Branch Descriptor to create the curve to.</param>
        public void GenerateSnapshotCurve (BranchDescriptor snapshot, SnapshotSettings snapshotSettings) {
            // Creal existing snapshot curve.
            snapshot.curve.RemoveAllNodes ();

            // Traverse the tree to add nodes.
            BroccoTree.Branch currentBranch = null;
            bool isFirstBranch = true;
            Vector3 offset = Vector3.zero;
            BezierNode node = null;

            if (snapshotTree != null && snapshotTree.branches.Count > 0) {
                currentBranch = snapshotTree.branches [0];
            }
            float noiseScaleAtFirstNode = 1;
            float noiseFactorAtFirstNode = 0;
            float noiseScaleAtLastNode = 1;
            float noiseFactorAtLastNode = 0;
            float noiseOffset = 0;
            float noiseResolution = 4f;
            float noiseStrength = 0.2f;
            bool spareFirstNode = true;

            int steps = 4;
            float step = 1f / (float)steps;
            Vector3 branchFirstP;
            Vector3 branchLastP;
            Vector3 branchP;
            Vector3 nodeP;

            float girthAtBase = currentBranch.GetGirthAtPosition (0f);
            float girthAtTop = girthAtBase;
            float accumLengthAtBase = currentBranch.GetLengthAtPos (0f, true);
            float accumLengthAtTop = accumLengthAtBase;
            while (currentBranch != null) {
                branchFirstP = currentBranch.GetPointAtPosition (0f);
                branchLastP = currentBranch.GetPointAtPosition (1f);
                if (isFirstBranch) {
                    //node = new BezierNode (currentBranch.GetPointAtPosition (0f));
                    nodeP = currentBranch.GetPointAtPosition (0f);
                    nodeP.x = branchFirstP.x;
                    node = new BezierNode (nodeP);
                    snapshot.curve.AddNode (node, false);
                }
                for (float i = 1; i <= steps; i++) {
                    //node = new BezierNode (currentBranch.GetPointAtPosition (i * step));
                    nodeP = currentBranch.GetPointAtPosition (i * step);
                    branchP = Vector3.Lerp (branchFirstP, branchLastP, i * step);
                    nodeP.x = branchP.x;
                    node = new BezierNode (nodeP);
                    snapshot.curve.AddNode (node, false);
                }
                isFirstBranch = false; 

                girthAtTop = currentBranch.GetGirthAtPosition (1f);
                accumLengthAtTop = currentBranch.GetLengthAtPos (1f, true);
                if (snapshotSettings.curveLevelLimit > -1 && 
                    currentBranch.GetLevel () == snapshotSettings.curveLevelLimit)
                {
                    currentBranch = null; 
                } else {
                    currentBranch = currentBranch.followUp;
                }
            }
            snapshot.curve.Process ();
            snapshot.curve.SetNoise (noiseFactorAtFirstNode, noiseFactorAtLastNode,
                noiseScaleAtFirstNode, noiseScaleAtLastNode, 
                girthAtBase, girthAtTop,
                accumLengthAtBase, accumLengthAtTop,
                spareFirstNode, noiseResolution, noiseStrength, noiseOffset);
        }
        /// <summary>
        /// Generates and registers polygon areas for a snapshot at a specific LOD.
        /// </summary>
        /// <param name="lodLevel">Level of detail.</param>
        /// <param name="snapshot">Snapshot of the snapshot.</param>
        public void GenerateSnapshotPolygonsPerLOD (int lodLevel, BranchDescriptor snapshot) {
            // Get Snapshot Processor.
            SnapshotProcessor processor = GetSnapshotProcessor (snapshot.processorId);
            if (processor == null) {
                Debug.LogWarning ("No Snapshot Processor found with id " + snapshot + ", skipping processing.");
                return;
            }
            // Begin usage.
            processor.BeginUsage (snapshotTree, snapshotTreeMesh, factoryScale);
            processor.simplifyHullEnabled = simplifyHullEnabled;
            polygonBuilder.BeginUsage (snapshotTree, snapshotTreeMesh, factoryScale);
            sproutCompositeManager.BeginUsage (snapshotTree, factoryScale);

            List<SnapshotProcessor.Fragment> fragments = 
                processor.GenerateSnapshotFragments (lodLevel, snapshot);

            // Process each fragment to:
            //
            SnapshotProcessor.Fragment fragment;

            Transform parentTransform = treeFactory.previewTree.obj.transform.parent;
            treeFactory.previewTree.obj.transform.parent = null;

            sproutCompositeManager.SaveColorAlphaToUV6s ();

            for (int fragIndex = 0; fragIndex < fragments.Count; fragIndex++) {
                //int resolution = 0;
                for (int resolution = 0; resolution <= PolygonArea.MAX_RESOLUTION; resolution ++) {
                    fragment = fragments [fragIndex];

                    // Create polygon per fragment/resolution.
                    PolygonArea polygonArea = new PolygonArea (snapshot.id, fragIndex, lodLevel, resolution);
                    polygonArea.resolution = resolution;
                    polygonArea.fragmentOffset = fragment.offset;

                    Hash128 _hash = Hash128.Compute (fragment.IncludesExcludesToString (snapshot.id));
                    polygonArea.hash = _hash;

                    // Generate hull points.
                    //polygonBuilder.GenerateHullPoints (polygonArea, fragment);
                    processor.GenerateHullPoints (polygonArea, fragment);

                    // Get bounds.
                    //polygonBuilder.ProcessPolygonAreaBounds (polygonArea, fragment);
                    processor.GenerateBounds (polygonArea, fragment);

                    // Additional points for the fragment.
                    //polygonBuilder.ProcessPolygonDetailPoints (polygonArea, fragment);
                    processor.ProcessPolygonDetailPoints (polygonArea, fragment);

                    // Set the triangles and build the mesh.
                    Bounds refBounds = polygonArea.aabb;
                    if (!_refFragBounds.ContainsKey (polygonArea.hash)) {
                        _refFragBounds.Add (polygonArea.hash, polygonArea.aabb);
                    } else {
                        refBounds = _refFragBounds [polygonArea.hash];
                    }
                    // Create mesh data (vertices, normals, tangents)
                    //polygonBuilder.ProcessPolygonAreaMesh (polygonArea, refBounds);
                    processor.ProcessPolygonAreaMesh (polygonArea, refBounds, fragment);

                    // Adds the unique polygon to the snapshot.
                    snapshot.polygonAreas.Add (polygonArea);

                    // Add polygon area to the SproutCompositeManager.
                    sproutCompositeManager.ManagePolygonArea (polygonArea, snapshot);

                    if (resolution == 0) {
                        // Generate Textures and materials.
                        sproutCompositeManager.GenerateTextures (polygonArea, snapshot, fragment, this);
                        sproutCompositeManager.GenerateMaterials (polygonArea, snapshot, true);
                    }
                }
            }
            
            sproutCompositeManager.ShowAllBranchesInMesh ();

            treeFactory.previewTree.obj.transform.parent = parentTransform;
            sproutCompositeManager.EndUsage ();
            polygonBuilder.EndUsage ();
            processor.EndUsage ();
        }        
        #endregion

        #region Variation Processing
        /// <summary>
        /// Regenerates a preview for the selected variation.
        /// </summary>
        /// <param name="isNewSeed"><c>True</c> to create a new preview (new seed).</param>
        public void ProcessVariation (bool isNewSeed = false) {
            // Process snapshots and cache them. 
            ProcessSnapshots ();
        }
        public void ProcessSnapshots (bool force = false) {
            BranchDescriptor snapshot;
            for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
                snapshot = branchDescriptorCollection.snapshots [i];
                if (!sproutCompositeManager.HasSnapshot (snapshot.id) || force) {
                    snapshotIndex = i;
                    SnapshotToPipeline ();
                    ProcessSnapshot (i, true);
                    ProcessSnapshotPolygons (snapshot);
                }
            }
        }
        #endregion

        #region Texture Processing
        public bool GeneratePolygonTexture (
            BranchDescriptor snapshot,
            Mesh mesh, 
            Bounds bounds,
            Vector3 planeNormal,
            Vector3 planeUp,
            Vector3 planeOffset,
            Material[] originalMaterials,
            MaterialMode materialMode,
            int width,
            int height,
            out Texture2D texture)
        {
            texture = null;

            // Apply material mode.
            GameObject previewTree = TreeFactory.GetActiveInstance ().previewTree.obj;
            MeshRenderer meshRenderer = previewTree.GetComponent<MeshRenderer> ();
            if (materialMode == MaterialMode.Albedo) { // Albedo
                meshRenderer.sharedMaterials = GetAlbedoMaterials (
                    snapshot, originalMaterials,
                    branchDescriptorCollection.branchColorShade,
                    branchDescriptorCollection.branchColorSaturation,
                    true);
            } else if (materialMode == MaterialMode.Normals) { // Normals
                meshRenderer.sharedMaterials = GetNormalMaterials (originalMaterials, isPrefabExport);
            } else if (materialMode == MaterialMode.Extras) { // Extras
                meshRenderer.sharedMaterials = GetExtraMaterials (
                    snapshot,
                    originalMaterials);
            } else if (materialMode == MaterialMode.Subsurface) { // Subsurface
                meshRenderer.sharedMaterials = GetSubsurfaceMaterials (
                    snapshot,
                    originalMaterials,
                    branchDescriptorCollection.branchColorSaturation,
                    branchDescriptorCollection.branchSubsurface);
            } else if (materialMode == MaterialMode.Composite) { // Composite
                meshRenderer.sharedMaterials = GetCompositeMaterials (
                    snapshot, originalMaterials);
            }

            // Prepare texture builder according to the material mode.
            TextureBuilder tb = new TextureBuilder ();
            bool useTextureDilation = false;
            Color textureDilationBgColor = Color.clear;
            if (hasTransparentTextureBackground) {
                tb.backgroundColor = Color.clear;
                tb.textureFormat = TextureFormat.RGBA32;
                useTextureDilation = textureDilationEnabled;
                if (materialMode == MaterialMode.Normals) {
                    tb.backgroundColor = NORMAL_BG_COLOR;
                } else if (materialMode == MaterialMode.Subsurface) {
                    tb.backgroundColor = SUBSURFACE_BG_COLOR;
                } else if (materialMode == MaterialMode.Extras) {
                    tb.backgroundColor = EXTRAS_BG_COLOR;
                } else {
                    useTextureDilation = false;
                }
            } else {
                if (materialMode == MaterialMode.Normals) {
                    tb.backgroundColor = NORMAL_BG_COLOR;
                } else if (materialMode == MaterialMode.Subsurface) {
                    tb.backgroundColor = SUBSURFACE_BG_COLOR;
                } else if (materialMode == MaterialMode.Extras) {
                    tb.backgroundColor = EXTRAS_BG_COLOR;
                }
                tb.textureFormat = TextureFormat.RGB24;
                useTextureDilation = false;
            }

            // Set the mesh..
            tb.useTextureSizeToTargetRatio = true;
            tb.BeginUsage (previewTree, mesh);
            tb.textureSize = new Vector2 (width, height);
            
            Vector3 cameraCenter = bounds.center - planeOffset;
            cameraCenter = Quaternion.LookRotation (Vector3.Cross (planeNormal, planeUp), planeUp) * cameraCenter;
            cameraCenter += planeOffset;

            bool allowMSAA = materialMode == MaterialMode.Albedo;

            texture = tb.GetTexture (cameraCenter, planeNormal, planeUp, bounds, string.Empty, allowMSAA);
            if (useTextureDilation) {
                Texture2D dilatedTex = TextureDilationUtility.DilateTexture (texture, 20, textureDilationBgColor, false, IsLinearColorSpace);
                UnityEngine.Object.DestroyImmediate (texture);
                texture = dilatedTex; 
            }
            
            tb.EndUsage ();

            return true;
        }
        public bool GenerateSnapshopTextures (int snapshotIndex, BranchDescriptorCollection branchDescriptorCollection,
            int width, int height, string albedoPath, string normalPath, string extrasPath, string subsurfacePath, string compositePath) {
            return GenerateSnapshopTextures (snapshotIndex, branchDescriptorCollection, width, height, GetPreviewTreeBounds (),
                albedoPath, normalPath, extrasPath, subsurfacePath, compositePath);
        }
        public bool GenerateSnapshopTextures (int snapshotIndex, BranchDescriptorCollection branchDescriptorCollection,
            int width, int height, Bounds bounds,
            string albedoPath, string normalPath, string extrasPath, string subsurfacePath, string compositePath) {
            BeginSnapshotProgress (branchDescriptorCollection);
            // ALBEDO
            if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                ReportProgress ("Processing albedo texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex, 
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Albedo, 
                    width,
                    height,
                    bounds,
                    albedoPath);
                ReportProgress ("Processing albedo texture.", 20f);
            }
            // NORMALS
            if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) {
                ReportProgress ("Processing normal texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex,
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Normals, 
                    width,
                    height,
                    bounds,
                    normalPath);
                ReportProgress ("Processing normal texture.", 20f);
            }
            // EXTRAS
            if ((branchDescriptorCollection.exportTexturesFlags & 4) == 4) {
                ReportProgress ("Processing extras texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex, 
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Extras, 
                    width,
                    height,
                    bounds,
                    extrasPath);
                ReportProgress ("Processing extras texture.", 20f);
            }
            // SUBSURFACE
            if ((branchDescriptorCollection.exportTexturesFlags & 8) == 8) {
                ReportProgress ("Processing subsurface texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex, 
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Subsurface, 
                    width,
                    height,
                    bounds,
                    subsurfacePath);
                ReportProgress ("Processing subsurface texture.", 20f);
            }
            // COMPOSITE
            if ((branchDescriptorCollection.exportTexturesFlags & 16) == 16) {
                ReportProgress ("Processing composite texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex, 
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Composite, 
                    width,
                    height,
                    bounds,
                    compositePath);
                ReportProgress ("Processing composite texture.", 20f);
            }
            FinishSnapshotProgress ();
            
            // Cleanup.
            MeshFilter meshFilter = treeFactory.previewTree.obj.GetComponent<MeshFilter>();
            UnityEngine.Object.DestroyImmediate (meshFilter.sharedMesh);

            return true;
        }
        /// <summary>
        /// Generates the texture for a giver snapshot.
        /// </summary>
        /// <param name="snapshotIndex">Index for the snapshot.</param>
        /// <param name="materialMode">Mode mode: composite, albedo, normals, extras or subsurface.</param>
        /// <param name="width">Maximum width for the texture.</param>
        /// <param name="height">Maximum height for the texture.</param>
        /// <param name="texturePath">Path to save the texture.</param>
        /// <returns>Texture generated.</returns>
        public Texture2D GenerateSnapshopTexture (
            int snapshotIndex, 
            BranchDescriptorCollection branchDescriptorCollection, 
            MaterialMode materialMode, 
            int width, 
            int height,
            Bounds bounds,
            string texturePath = "") 
        {
            if (snapshotIndex >= branchDescriptorCollection.snapshots.Count) {
                Debug.LogWarning ("Could not generate branch snapshot texture. Index out of range.");
            } else {
                // Regenerate branch mesh and apply material mode.
                this.snapshotIndex = snapshotIndex;
                ProcessSnapshot (snapshotIndex, false, materialMode);
                // Build and save texture.
                TextureBuilder tb = new TextureBuilder ();
                if (materialMode == MaterialMode.Normals) {
                    tb.backgroundColor = new Color (0.5f, 0.5f, 1f, 1f);
                    tb.textureFormat = TextureFormat.RGB24;
                } else if (materialMode == MaterialMode.Subsurface) {
                    tb.backgroundColor = new Color (0f, 0f, 0f, 1f);
                    tb.textureFormat = TextureFormat.RGB24;
                } else if (materialMode == MaterialMode.Extras) {
                    tb.backgroundColor = new Color (0f, 0f, 1f, 1f);
                    tb.textureFormat = TextureFormat.RGB24;
                }
                // Get tree mesh.
                GameObject previewTree = treeFactory.previewTree.obj;
                tb.useTextureSizeToTargetRatio = true;
                tb.BeginUsage (previewTree);
                tb.textureSize = new Vector2 (width, height);
                Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up, bounds, texturePath);
                tb.EndUsage ();
                return sproutTexture;
            }
            return null;
        }
        Bounds GetPreviewTreeBounds () {
            GameObject previewTree = treeFactory.previewTree.obj;
            MeshFilter meshFilder = previewTree.GetComponent<MeshFilter> ();
            if (meshFilder != null) {
                return meshFilder.sharedMesh.bounds;
            }
            return new Bounds ();
        }
        /// <summary>
        /// Generates an atlas texture from a snapshot at each snapshot in the collection.
        /// </summary>
        /// <param name="branchDescriptorCollection">Collection of snapshot.</param>
        /// <param name="width">Width in pixels for the atlas.</param>
        /// <param name="height">Height in pixels for the atlas.</param>
        /// <param name="padding">Padding in pixels between each atlas sprite.</param>
        /// <param name="albedoPath">Path to save the albedo texture.</param>
        /// <param name="normalsPath">Path to save the normals texture.</param>
        /// <param name="extrasPath">Path to save the extras texture.</param>
        /// <param name="subsurfacePath">Path to save the subsurface texture.</param>
        /// <param name="compositePath">Path to save the composite texture.</param>
        /// <returns><c>True</c> if the atlases were created.</returns>
        public bool GenerateAtlasTexture (
            BranchDescriptorCollection branchDescriptorCollection, 
            int width, 
            int height, 
            int padding,
            string albedoPath, 
            string normalPath, 
            string extrasPath, 
            string subsurfacePath, 
            string compositePath,
            int dilationItertations) 
        {
            #if UNITY_EDITOR
            if (branchDescriptorCollection.snapshots.Count == 0) {
                Debug.LogWarning ("Could not generate atlas texture, no branch snapshots were found.");
            } else {
                // 1. Generate each snapshot mesh.
                float largestMeshSize = 0f; 
                List<Mesh> meshes = new List<Mesh> (); // Save the mesh for each snapshot.
                List<BranchDescriptor> snapshots = new List<BranchDescriptor> ();
                BranchDescriptor snapshot;
                List<Material[]> materials = new List<Material[]> ();
                List<Texture2D> texturesForAtlas = new List<Texture2D> ();
                Material[] modeMaterials;
                TextureBuilder tb = new TextureBuilder ();
                Texture2D atlas;
                tb.useTextureSizeToTargetRatio = true;

                double editorTime = UnityEditor.EditorApplication.timeSinceStartup;

                BeginAtlasProgress (branchDescriptorCollection);

                MeshFilter meshFilter = treeFactory.previewTree.obj.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = treeFactory.previewTree.obj.GetComponent<MeshRenderer>();
                for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
                    ReportProgress ("Creating mesh for snapshot " + i + ".", 0f);
                    snapshotIndex = i;
                    SnapshotToPipeline ();
                    ProcessSnapshot (i, false);
                    meshes.Add (UnityEngine.Object.Instantiate (meshFilter.sharedMesh));
                    snapshots.Add (branchDescriptorCollection.snapshots [i]);
                    materials.Add (meshRenderer.sharedMaterials);
                    ReportProgress ("Creating mesh for snapshot " + i + ".", 10f);
                }

                // 2. Get the larger snapshot.
                for (int i = 0; i < meshes.Count; i++) {
                    if (meshes [i].bounds.max.magnitude > largestMeshSize) {
                        largestMeshSize = meshes [i].bounds.max.magnitude;
                    }
                }

                // Generate each mode texture.
                GameObject previewTree = treeFactory.previewTree.obj;

                // Prepare texture packing parameters.
                TexturePacker.TexturePackParams texPackParams = 
					new TexturePacker.TexturePackParams (padding, TexturePacker.GetPixelSize (width), Color.clear, false);

                // ALBEDO
                if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                    for (int i = 0; i < meshes.Count; i++) {
                        snapshot = snapshots [i];
                        ReportProgress ("Creating albedo texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.1 Albedo.
                        modeMaterials = GetAlbedoMaterials (
                            snapshot,
                            materials [i],
							branchDescriptorCollection.branchColorShade,
							branchDescriptorCollection.branchColorSaturation);
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = new Color (0.5f, 0.5f, 0.5f, 0f);
                        tb.textureFormat = TextureFormat.RGBA32;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up);
                        texturesForAtlas.Add (sproutTexture);
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating albedo texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating albedo atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    TexturePacker.PackTextures (out atlas, texturesForAtlas.ToArray (), defaultPackMode, texPackParams);
                    atlas.alphaIsTransparency = true;
                    TextureUtil.SaveTextureToFile (atlas, albedoPath);
                    TextureUtil.CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating albedo atlas texture.", 10f);
                }

                // NORMALS
                if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) {
                    for (int i = 0; i < meshes.Count; i++) {
                        ReportProgress ("Creating normal texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.2 Normals.
                        modeMaterials = GetNormalMaterials (materials [i], true);
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = Color.clear;
                        tb.textureFormat = TextureFormat.RGBA32;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up, string.Empty, false);
                        if (textureDilationEnabled) {
                            Texture2D dilatedSproutTexture = TextureDilationUtility.DilateTexture (sproutTexture, dilationItertations, NORMAL_BG_COLOR, false, IsLinearColorSpace);
                            texturesForAtlas.Add (dilatedSproutTexture);
                            UnityEngine.Object.DestroyImmediate (sproutTexture);
                        } else {
                            texturesForAtlas.Add (sproutTexture);
                        }
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating extra texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating normal atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    texPackParams.bgColor = NORMAL_BG_COLOR;
                    TexturePacker.PackTextures (out atlas, texturesForAtlas.ToArray (), defaultPackMode, texPackParams);
                    atlas.alphaIsTransparency = false;
                    TextureUtil.SaveTextureToFile (atlas, normalPath);
                    TextureUtil.SetTextureAsNormalMap (atlas, normalPath);
                    TextureUtil.CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating normal atlas texture.", 10f);
                }

                // EXTRAS
                if ((branchDescriptorCollection.exportTexturesFlags & 4) == 4) {
                    for (int i = 0; i < meshes.Count; i++) {
                        snapshot = snapshots [i];
                        ReportProgress ("Creating extras texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.3 Extra.
                        modeMaterials = GetExtraMaterials (
                            snapshot,
                            materials [i]);
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = Color.clear;
                        tb.textureFormat = TextureFormat.RGBA32;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up, string.Empty, false);
                        if (textureDilationEnabled) {
                            Texture2D dilatedSproutTexture = TextureDilationUtility.DilateTexture (sproutTexture, dilationItertations, EXTRAS_BG_COLOR, false, IsLinearColorSpace);
                            texturesForAtlas.Add (dilatedSproutTexture);
                            UnityEngine.Object.DestroyImmediate (sproutTexture);
                        } else {
                            texturesForAtlas.Add (sproutTexture);
                        }
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating extras texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating extras atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    texPackParams.bgColor = EXTRAS_BG_COLOR;
                    TexturePacker.PackTextures (out atlas, texturesForAtlas.ToArray (), defaultPackMode, texPackParams);
                    atlas.alphaIsTransparency = false;
                    TextureUtil.SaveTextureToFile (atlas, extrasPath);
                    TextureUtil.CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating extras atlas texture.", 10f);
                }

                // SUBSURFACE
                if ((branchDescriptorCollection.exportTexturesFlags & 8) == 8) {
                    for (int i = 0; i < meshes.Count; i++) {
                        snapshot = snapshots [i];
                        ReportProgress ("Creating subsurface texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.4 Subsurface.
                        modeMaterials = GetSubsurfaceMaterials (
                            snapshot,
                            materials [i],
                            branchDescriptorCollection.branchColorSaturation,
                            branchDescriptorCollection.branchSubsurface);
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = Color.clear;
                        tb.textureFormat = TextureFormat.RGBA32;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up);
                        if (textureDilationEnabled) {
                            Texture2D dilatedSproutTexture = TextureDilationUtility.DilateTexture (sproutTexture, dilationItertations, SUBSURFACE_BG_COLOR, false, IsLinearColorSpace);
                            texturesForAtlas.Add (dilatedSproutTexture);
                            UnityEngine.Object.DestroyImmediate (sproutTexture);
                        } else {
                            texturesForAtlas.Add (sproutTexture);
                        }
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating subsurface texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating subsurface atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    texPackParams.bgColor = SUBSURFACE_BG_COLOR;
                    TexturePacker.PackTextures (out atlas, texturesForAtlas.ToArray (), defaultPackMode, texPackParams);
                    atlas.alphaIsTransparency = false;
                    TextureUtil.SaveTextureToFile (atlas, subsurfacePath);
                    TextureUtil.CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating subsurface atlas texture.", 10f);
                }

                // COMPOSITE
                if ((branchDescriptorCollection.exportTexturesFlags & 16) == 16) {
                    for (int i = 0; i < meshes.Count; i++) {
                        ReportProgress ("Creating composite texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.5 Composite.
                        modeMaterials = materials [i];
                        for (int k = 0; k < modeMaterials.Length; k++) {
                            modeMaterials [k].EnableKeyword ("_WINDQUALITY_NONE");
                        }
                        /*
                        GetCompositeMaterials (materials [i],
                            GetMaterialAStartIndex (branchDescriptorCollection), GetMaterialBStartIndex (branchDescriptorCollection));
                            */
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = new Color (0.5f, 0.5f, 0.5f, 0f);
                        tb.textureFormat = TextureFormat.RGBA32;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up);
                        texturesForAtlas.Add (sproutTexture);
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating composite texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating composite atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    //atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    texPackParams.bgColor = Color.clear;
                    TexturePacker.PackTextures (out atlas, texturesForAtlas.ToArray (), defaultPackMode, texPackParams);
                    atlas.alphaIsTransparency = true;
                    TextureUtil.SaveTextureToFile (atlas, compositePath);
                    TextureUtil.CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating composite atlas texture.", 10f);
                }

                // Cleanup, destroy meshes, materials and textures.
                for (int i = 0; i < meshes.Count; i++) {
                    UnityEngine.Object.DestroyImmediate (meshes [i]);
                }
                for (int i = 0; i < materials.Count; i++) {
                    for (int j = 0; j < materials [i].Length; j++) {
                        UnityEngine.Object.DestroyImmediate (materials [i][j]);   
                    }
                }
                FinishAtlasProgress ();
                return true;
            }
            #endif
            return false;
        }
        /// <summary>
        /// Generates an atlas texture from the textures registered at the SproutCompositeManager.
        /// </summary>
        /// <param name="branchDescriptorCollection">Collection of snapshot.</param>
        /// <param name="width">Width in pixels for the atlas.</param>
        /// <param name="height">Height in pixels for the atlas.</param>
        /// <param name="padding">Padding in pixels between each atlas sprite.</param>
        /// <param name="albedoPath">Path to save the albedo texture.</param>
        /// <param name="normalsPath">Path to save the normals texture.</param>
        /// <param name="extrasPath">Path to save the extras texture.</param>
        /// <param name="subsurfacePath">Path to save the subsurface texture.</param>
        /// <param name="compositePath">Path to save the composite texture.</param>
        /// <returns><c>True</c> if the atlases were created.</returns>
        public bool GenerateAtlasTextureFromPolygons (
            BranchDescriptorCollection branchDescriptorCollection, 
            int width, 
            int height, 
            int padding,
            string albedoPath, 
            string normalsPath, 
            string extrasPath, 
            string subsurfacePath, 
            string compositePath) 
        {
            #if UNITY_EDITOR
            if (branchDescriptorCollection.snapshots.Count == 0) {
                Debug.LogWarning ("Could not generate atlas texture, no branch snapshots were found.");
            } else {
                // 1. Save the mesh and materials for each snapshot.
                List<Mesh> meshes = new List<Mesh> (); // Save the mesh for each snapshot.
                List<Material[]> materials = new List<Material[]> ();
                List<Texture2D> texturesForAtlas = new List<Texture2D> ();
                List<BroccoTree> trees = new List<BroccoTree> ();

                // 2. Create atlas texture.
                Texture2D atlas;

                // 3. Init helper vars.
                float largestMeshSize = 0f;
                Rect[] atlasRects = null;

                // 4. Begin atlas creation process.
                BeginAtlasProgress (branchDescriptorCollection);

                // 5. For each snapshot create its snapshot.
                MeshFilter meshFilter = treeFactory.previewTree.obj.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = treeFactory.previewTree.obj.GetComponent<MeshRenderer>();

                sproutCompositeManager.Clear ();

                isPrefabExport = true;
                ProcessSnapshots ();
                isPrefabExport = false;
                
                for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
                    ReportProgress ("Creating mesh for snapshot " + i + ".", 0f);
                    snapshotIndex = i;
                    SnapshotToPipeline ();
                    ProcessSnapshot (i, false);

                    // 5.1 Save the snapshot tree, mesh and snapshot materials.
                    meshes.Add (UnityEngine.Object.Instantiate (meshFilter.sharedMesh));
                    materials.Add (meshRenderer.sharedMaterials);
                    trees.Add (treeFactory.previewTree);
                    treeFactory.previewTree.obj.transform.parent = null;
                    treeFactory.previewTree.obj.hideFlags = HideFlags.None;
                    treeFactory.previewTree = null;
                    ReportProgress ("Creating mesh for snapshot " + i + ".", 10f);

                }

                // 6. Get the snapshot with the largest area.
                for (int i = 0; i < meshes.Count; i++) {
                    if (meshes [i].bounds.max.magnitude > largestMeshSize) {
                        largestMeshSize = meshes [i].bounds.max.magnitude;
                    }
                }

                // 7. For each snapshot create its polygons.
                for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
                    treeFactory.previewTree = trees [i];
                    snapshotTree = treeFactory.previewTree;
                    snapshotTreeMesh = meshFilter.sharedMesh;
                    sproutCompositeManager.textureGlobalScale = snapshotTreeMesh.bounds.max.magnitude / largestMeshSize; 
                    ProcessSnapshotPolygons (branchDescriptorCollection.snapshots [i]);
                }

                // 8.0 Prepare texture packing parameters.
                TexturePacker.TexturePackParams texPackParams = 
					new TexturePacker.TexturePackParams (padding, TexturePacker.GetPixelSize (width), Color.clear, false);

                // 8.1 Generate the ALBEDO texture.
                branchDescriptorCollection.atlasAlbedoTexture = null;
                if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                    List<Texture2D> albedoTextures = sproutCompositeManager.GetAlbedoTextures ();
                    for (int i = 0; i < albedoTextures.Count; i++) {
                        texturesForAtlas.Add (albedoTextures [i]);
                    }
                    ReportProgress ("Creating albedo atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlasRects = TexturePacker.PackTextures (out atlas, texturesForAtlas.ToArray (), defaultPackMode, texPackParams);
                    atlas.alphaIsTransparency = true;
                    branchDescriptorCollection.atlasAlbedoTexture = TextureUtil.SaveTextureToFile (atlas, albedoPath, true);
                    TextureUtil.CleanTextures (texturesForAtlas);
                    ReportProgress ("Creating albedo atlas texture.", 10f);
                }

                // 8.2 Generate the NORMALS texture.
                branchDescriptorCollection.atlasNormalsTexture = null;
                if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) {
                    List<Texture2D> normalsTextures = sproutCompositeManager.GetNormalsTextures ();
                    for (int i = 0; i < normalsTextures.Count; i++) {
                        texturesForAtlas.Add (normalsTextures [i]);
                    }
                    ReportProgress ("Creating normals atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    texPackParams.bgColor = new Color (0.5f, 0.5f, 1f, 1f);
                    atlasRects = TexturePacker.PackTextures (out atlas, texturesForAtlas.ToArray (), defaultPackMode, texPackParams);
                    atlas.alphaIsTransparency = false;
                    branchDescriptorCollection.atlasNormalsTexture = TextureUtil.SaveTextureToFile (atlas, normalsPath, true);
                    TextureUtil.SetTextureAsNormalMap (atlas, normalsPath);
                    TextureUtil.CleanTextures (texturesForAtlas);
                    ReportProgress ("Creating normals atlas texture.", 10f);
                }

                // 8.3 Generate the EXTRAS texture.
                branchDescriptorCollection.atlasExtrasTexture = null;
                if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                    List<Texture2D> extrasTextures = sproutCompositeManager.GetExtrasTextures ();
                    for (int i = 0; i < extrasTextures.Count; i++) {
                        texturesForAtlas.Add (extrasTextures [i]);
                    }
                    ReportProgress ("Creating extras atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    texPackParams.bgColor = new Color (0f, 0f, 1f, 1f);
                    atlasRects = TexturePacker.PackTextures (out atlas, texturesForAtlas.ToArray (), defaultPackMode, texPackParams);
                    atlas.alphaIsTransparency = false;
                    branchDescriptorCollection.atlasExtrasTexture = TextureUtil.SaveTextureToFile (atlas, extrasPath, true);
                    TextureUtil.CleanTextures (texturesForAtlas);
                    ReportProgress ("Creating extras atlas texture.", 10f);
                }

                // 8.4 Generate the SUBSURFACE texture.
                branchDescriptorCollection.atlasSubsurfaceTexture = null;
                if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                    List<Texture2D> subsurfaceTextures = sproutCompositeManager.GetSubsurfaceTextures ();
                    for (int i = 0; i < subsurfaceTextures.Count; i++) {
                        texturesForAtlas.Add (subsurfaceTextures [i]);
                    }
                    ReportProgress ("Creating subsurface atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    texPackParams.bgColor = new Color (0f, 0f, 0f, 1f);
                    atlasRects = TexturePacker.PackTextures (out atlas, texturesForAtlas.ToArray (), defaultPackMode, texPackParams);
                    atlas.alphaIsTransparency = false;
                    branchDescriptorCollection.atlasSubsurfaceTexture = TextureUtil.SaveTextureToFile (atlas, subsurfacePath, true);
                    TextureUtil.CleanTextures (texturesForAtlas);
                    ReportProgress ("Creating subsurface atlas texture.", 10f);
                }

                // 8.5 Finish atlases creation.
                FinishAtlasProgress ();

                // 9. Set atlas rects to meshes.
                if (atlasRects != null) {
                    sproutCompositeManager.SetAtlasRects (atlasRects);
                    sproutCompositeManager.ApplyAtlasUVs ();
                }

                // 11. Clean up atlas building.
                sproutCompositeManager.textureGlobalScale = 1f;
                treeFactory.previewTree = null;
                for (int i = 0; i < trees.Count; i++) {
                    UnityEngine.Object.DestroyImmediate (trees[i].obj);
                }
                UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate ();
                return true;
            }
            #endif
            return false;
        }
        public Texture2D GetSproutTexture (int group, int index) {
            string textureId = GetSproutTextureId (group, index);
            return textureManager.GetTexture (textureId);
        }
        Texture2D GetOriginalSproutTexture (int group, int index) {
            // Forced sproutstructure index to sprout style index
            Texture2D texture = null;
            List<SproutMap.SproutMapArea> sproutMapAreas = null;
            sproutMapAreas = branchDescriptorCollection.sproutStyles[group].sproutMapAreas;
            if (sproutMapAreas != null && sproutMapAreas.Count >= index) {
                texture = sproutMapAreas[index].texture;
            }
            return texture;
        }
        public void ProcessTextures () {
            // Forced sproutstructure index to sprout style index
            string textureId;
            BranchDescriptorCollection.SproutStyle sproutStyle;
            for (int styleI = 0; styleI < branchDescriptorCollection.sproutStyles.Count; styleI++) {
                sproutStyle = branchDescriptorCollection.sproutStyles [styleI];
                for (int mapI = 0; mapI < sproutStyle.sproutMapAreas.Count; mapI++) {
                    textureId = GetSproutTextureId (styleI, mapI);
                    if (!textureManager.HasTexture (textureId)) {
                        textureManager.AddOrReplaceTexture (textureId, 
                            sproutStyle.sproutMapAreas [mapI].texture);
                    }
                }
            }
        }
        public void ProcessTexture (Texture2D texture, int group, int index) {
            #if UNITY_EDITOR
            string textureId = GetSproutTextureId (group, index);
            textureManager.AddOrReplaceTexture (textureId, texture);
            SnapshotToPipeline ();
            #endif
        }
        Texture2D ApplyTextureTransformations (Texture2D originTexture, float alpha) {
            if (originTexture != null) {
                Texture2D tex = textureManager.GetCopy (originTexture, alpha);
                return tex;
            }
            return null;
        }
        public static string GetTextureFileName (string path, string subfolder, string prefix, int take, SproutSubfactory.MaterialMode materialMode, bool isAtlas) {
			string _path = "";
			string takeString = FileUtils.GetFileSuffix (take);
			string modeString;
			if (materialMode == SproutSubfactory.MaterialMode.Albedo) {
				modeString = "Albedo";
			} else if (materialMode == SproutSubfactory.MaterialMode.Normals) {
				modeString = "Normals";
			} else if (materialMode == SproutSubfactory.MaterialMode.Extras) {
				modeString = "Extras";
			} else if (materialMode == SproutSubfactory.MaterialMode.Subsurface) {
				modeString = "Subsurface";
			} else if (materialMode == SproutSubfactory.MaterialMode.Mask) {
				modeString = "Mask";
			} else if (materialMode == SproutSubfactory.MaterialMode.Thickness) {
				modeString = "Thickness";
			} else {
				modeString = "Composite";
			}
			_path = "Assets" + path + "/" + subfolder + "/" + 
				prefix + takeString + (isAtlas?"_Atlas":"_Snapshot") + "_" + modeString + ".png";
			return _path;
		}
        public string GetSproutTextureId (int group, int index) {
            return  "sprout_" + group + "_" + index;
        }
        #endregion

        #region Material Processing
        public Material[] GetCompositeMaterials (BranchDescriptor snapshot, Material[] originalMaterials) 
        {
            Material[] mats = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++) {
                if (originalMaterials [i] != null) {
                    if (i == 0) {
                        mats[0] = originalMaterials [0];
                        mats[0].shader = GetSpeedTree8Shader ();
                        mats[0].SetFloat ("_WindQuality", 0f);
                        mats[0].enableInstancing = true;
                    } else {
                        Material m = new Material (originalMaterials[i]);
                        m.shader = GetSpeedTree8Shader ();
                        m.enableInstancing = true;
                        m.SetFloat ("_WindQuality", 0f);
                        m.EnableKeyword ("GEOM_TYPE_LEAF");
                        m.SetFloat ("_SubsurfaceKwToggle", 1f);
                        float subsurfaceIndirect = 0.5f;
                        if (originalMaterials[i].HasProperty("_SubsurfaceScale")) {
                            subsurfaceIndirect = originalMaterials[i].GetFloat ("_SubsurfaceScale") * 0.5f;
                        } else if (originalMaterials[i].HasProperty("_SubsurfaceIndirect")) {
                            subsurfaceIndirect = originalMaterials[i].GetFloat ("_SubsurfaceIndirect");
                        }
                        m.SetFloat ("_SubsurfaceIndirect", subsurfaceIndirect);
                        mats [i] = m;
                    }
                }
            }
            UpdateCompositeMaterials (snapshot, mats);
            return mats;
        }
        public void UpdateCompositeMaterials (BranchDescriptor snapshot, Material[] compositeMaterials) 
        {
            BranchDescriptorCollection.SproutStyle sproutStyle = null;
            foreach (BranchDescriptor.SproutStructure sproutStructure in snapshot.sproutStructures) {
                sproutStyle = branchDescriptorCollection.GetSproutStyle (sproutStructure.styleId);
                if (sproutStyle != null && sproutStructure.submeshCount > 0) {
                    for (int matI = sproutStructure.submeshIndex; matI < sproutStructure.submeshIndex + sproutStructure.submeshCount; matI++) {
                        Material m = compositeMaterials [matI];
                        m.SetFloat ("_Metallic", sproutStyle.metallic);
                        m.SetFloat ("_Glossiness", sproutStyle.glossiness);
                        m.SetFloat ("_SubsurfaceIndirect", sproutStyle.subsurface);
                        m.SetColor ("_SubsurfaceColor", GetSubsurfaceColor (sproutStyle.subsurface));
                    }
                }
            }
        }
        public Material[] GetAlbedoMaterials (
            BranchDescriptor snapshot,
            Material[] originalMaterials,
            float branchMaterialShade = 1f,
            float branchMaterialSaturation = 1f,
            bool extraSaturation = false)
        {
            Material[] mats = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++) {
                Material m;
                if (originalMaterials [i] == null) {
                    m = originalMaterials [i];
                } else {
                    m = new Material (originalMaterials[i]);
                    m.shader = Shader.Find ("Hidden/Broccoli/SproutLabAlbedo");
                    m.enableInstancing = true;
                }
                mats [i] = m;
            }
            UpdateAlbedoMaterials (snapshot, mats, branchMaterialShade, branchMaterialSaturation, extraSaturation);
            return mats;
        }
        public void UpdateAlbedoMaterials (
            BranchDescriptor snapshot,
            Material[] albedoMaterials,
            float branchMaterialShade,
            float branchMaterialSaturation,
            bool extraSaturation = false)
        {
            Material m;
            for (int i = 0; i < albedoMaterials.Length; i++) {
                m = albedoMaterials [i];
                if (albedoMaterials [i] != null) {
                    m.SetFloat ("_BranchShade", branchMaterialShade);
                    m.SetFloat ("_BranchSat", branchMaterialSaturation);
                    //m.SetFloat ("_ApplyExtraSat", applyExtraSaturation?1f:0f);
                    #if UNITY_EDITOR
                    m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                    #endif
                }
            }
            BranchDescriptorCollection.SproutStyle sproutStyle = null;
            foreach (BranchDescriptor.SproutStructure sproutStructure in snapshot.sproutStructures) {
                sproutStyle = branchDescriptorCollection.GetSproutStyle (sproutStructure.styleId);
                if (sproutStyle != null && sproutStructure.submeshCount > 0) {
                    for (int matI = sproutStructure.submeshIndex; matI < sproutStructure.submeshIndex + sproutStructure.submeshCount; matI++) {
                        m = albedoMaterials [matI];
                        m.SetColor ("_TintColor", sproutStyle.colorTint);
                        m.SetFloat ("_MinSproutTint", sproutStyle.minColorTint);
                        m.SetFloat ("_MaxSproutTint", sproutStyle.maxColorTint);
                        m.SetInt ("_SproutTintMode", (int)sproutStyle.sproutTintMode);
                        m.SetFloat ("_InvertSproutTintMode", (sproutStyle.invertSproutTintMode?1f:0f));
                        m.SetFloat ("_SproutTintVariance", sproutStyle.sproutTintVariance);
                        m.SetFloat ("_MinSproutSat", sproutStyle.minColorSaturation);
                        m.SetFloat ("_MaxSproutSat", sproutStyle.maxColorSaturation);
                        m.SetInt ("_SproutSatMode", (int)sproutStyle.sproutSaturationMode);
                        m.SetFloat ("_InvertSproutSatMode", (sproutStyle.invertSproutSaturationMode?1f:0f)); 
                        m.SetFloat ("_SproutSatVariance", sproutStyle.sproutSaturationVariance);
                        m.SetFloat ("_ApplyExtraSat", (extraSaturation?1f:0f));
                    }
                }
            }
        }
        public Material[] GetNormalMaterials (Material[] originalMaterials, bool isGammaDisplay) {
            isGammaDisplay = true;
            Material[] mats = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++) {
                Material m = new Material (originalMaterials[i]);
                m.shader = Shader.Find ("Hidden/Broccoli/SproutLabNormals");
                m.SetFloat ("_IsGammaDisplay", isGammaDisplay?1f:0f);
                #if UNITY_EDITOR
                float linearSpace = (UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                m.SetFloat ("_IsLinearColorSpace", linearSpace);
                #endif
                mats [i] = m; 
            }
            return mats;
        }
        public Material[] GetExtraMaterials (
            BranchDescriptor snapshot,
            Material[] originalMaterials)
        { 
            Material[] mats = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++) {
                Material m = new Material (originalMaterials[i]);
                m.shader = Shader.Find ("Hidden/Broccoli/SproutLabExtra");
                #if UNITY_EDITOR
                m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                #endif
                mats [i] = m;
            }
            UpdateExtrasMaterials (snapshot, mats);
            return mats;
        }
        public void UpdateExtrasMaterials (BranchDescriptor snapshot, Material[] extrasMaterials) 
        {
            for (int i = 0; i < extrasMaterials.Length; i++) {
                if (extrasMaterials [i] != null) {
                    Material m = extrasMaterials [i];
                    if (extrasMaterials [i] != null) {
                        #if UNITY_EDITOR
                        m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                        #endif
                    }
                }
            }
            BranchDescriptorCollection.SproutStyle sproutStyle = null;
            foreach (BranchDescriptor.SproutStructure sproutStructure in snapshot.sproutStructures) {
                sproutStyle = branchDescriptorCollection.GetSproutStyle (sproutStructure.styleId);
                if (sproutStyle != null && sproutStructure.submeshCount > 0) {
                    for (int matI = sproutStructure.submeshIndex; matI < sproutStructure.submeshIndex + sproutStructure.submeshCount; matI++) {
                        Material m = extrasMaterials [matI];
                        m.SetFloat ("_Metallic", sproutStyle.metallic);
                        m.SetFloat ("_Glossiness", sproutStyle.glossiness);
                    }
                }
            }
        }
        public Material[] GetSubsurfaceMaterials (
            BranchDescriptor snapshot,
            Material[] originalMaterials,
            float branchSaturation,
            float branchSubsurface) 
        {
            Material[] mats = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++) {
                Material m = new Material (originalMaterials[i]);
                m.shader = Shader.Find ("Hidden/Broccoli/SproutLabSubsurface");
                m.SetFloat ("_BranchSat", branchSaturation);
                m.SetFloat ("_BranchSub", branchSubsurface);
                #if UNITY_EDITOR
                m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                #endif
                mats [i] = m;
            }
            UpdateSubsurfaceMaterials (snapshot, mats, branchSaturation, branchSubsurface);
            return mats;
        }
        public void UpdateSubsurfaceMaterials (
            BranchDescriptor snapshot,
            Material[] subsurfaceMaterials,
            float branchSaturation,
            float branchSubsurface) 
        {
            Material m;
            for (int i = 0; i < subsurfaceMaterials.Length; i++) {
                m = subsurfaceMaterials [i];
                if (subsurfaceMaterials [i] != null) {
                    m.SetFloat ("_BranchSat", branchSaturation);
                    m.SetFloat ("_BranchSub", branchSubsurface);
                    #if UNITY_EDITOR
                    m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                    #endif
                }
            }
            BranchDescriptorCollection.SproutStyle sproutStyle = null;
            foreach (BranchDescriptor.SproutStructure sproutStructure in snapshot.sproutStructures) {
                sproutStyle = branchDescriptorCollection.GetSproutStyle (sproutStructure.styleId);
                if (sproutStyle != null && sproutStructure.submeshCount > 0) {
                    for (int matI = sproutStructure.submeshIndex; matI < sproutStructure.submeshIndex + sproutStructure.submeshCount; matI++) {
                        m = subsurfaceMaterials [matI];
                        m.SetColor ("_TintColor", sproutStyle.colorTint);
                        m.SetFloat ("_MinSproutTint", sproutStyle.minColorTint);
                        m.SetFloat ("_MaxSproutTint", sproutStyle.maxColorTint);
                        m.SetInt ("_SproutTintMode", (int)sproutStyle.sproutTintMode);
                        m.SetFloat ("_InvertSproutTintMode", (sproutStyle.invertSproutTintMode?1f:0f));
                        m.SetFloat ("_SproutTintVariance", sproutStyle.sproutTintVariance);
                        m.SetFloat ("_MinSproutSat", sproutStyle.minColorSaturation);
                        m.SetFloat ("_MaxSproutSat", sproutStyle.maxColorSaturation);
                        m.SetInt ("_SproutSatMode", (int)sproutStyle.sproutSaturationMode);
                        m.SetFloat ("_InvertSproutSatMode", (sproutStyle.invertSproutSaturationMode?1f:0f)); 
                        m.SetFloat ("_SproutSatVariance", sproutStyle.sproutSaturationVariance);
                        m.SetFloat ("_SproutSubsurface", sproutStyle.subsurface);
                    }
                }
            }
        }
        public void DestroyMaterials (Material[] materials) {
            for (int i = 0; i < materials.Length; i++) {
                UnityEngine.Object.DestroyImmediate (materials [i]);
            }
        }
        private Shader GetSpeedTree8Shader () {
            Shader st8Shader = null;
            st8Shader = Shader.Find ("Hidden/Broccoli/SproutLabComposite"); 
            return st8Shader;
        }
        #endregion

        #region Processing Progress
        public delegate void OnReportProgress (string msg, float progress);
        public delegate void OnFinishProgress ();
        public OnReportProgress onReportProgress;
        public OnFinishProgress onFinishProgress;
        float progressGone = 0f;
        float progressToGo = 0f;
        public string progressTitle = "";
        public void BeginSnapshotProgress (BranchDescriptorCollection branchDescriptorCollection) {
            progressGone = 0f;
            progressToGo = 0f;
            if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) progressToGo += 20; // Albedo
            if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) progressToGo += 20; // Normals
            if ((branchDescriptorCollection.exportTexturesFlags & 4) == 4) progressToGo += 20; // Extras
            if ((branchDescriptorCollection.exportTexturesFlags & 8) == 8) progressToGo += 20; // Subsurface
            if ((branchDescriptorCollection.exportTexturesFlags & 16) == 16) progressToGo += 20; // Composite
            progressTitle = "Creating Snapshot Textures";
        }
        public void FinishSnapshotProgress () {
            progressGone = progressToGo;
            ReportProgress ("Finish " + progressTitle, 0f);
            onFinishProgress?.Invoke ();
        }
        public void BeginAtlasProgress (BranchDescriptorCollection branchDescriptorCollection) {
            progressGone = 0f;
            progressToGo = branchDescriptorCollection.snapshots.Count * 10f;
            if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) progressToGo += 30; // Albedo
            if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) progressToGo += 30; // Normals
            if ((branchDescriptorCollection.exportTexturesFlags & 4) == 4) progressToGo += 30; // Extras
            if ((branchDescriptorCollection.exportTexturesFlags & 8) == 8) progressToGo += 30; // Subsurface
            if ((branchDescriptorCollection.exportTexturesFlags & 16) == 16) progressToGo += 30; // Composite
            progressTitle = "Creating Atlas Textures";
        }
        public void FinishAtlasProgress () {
            progressGone = progressToGo;
            ReportProgress ("Finish " + progressTitle, 0f);
            onFinishProgress?.Invoke ();
        }
        void ReportProgress (string title, float progressToAdd) {
            progressGone += progressToAdd;
            onReportProgress?.Invoke (title, progressGone/progressToGo);
        }
        #endregion
    }
}