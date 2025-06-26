using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

namespace Broccoli.Pipe {
	/// <summary>
	/// Class with directives to construct meshes for sprouts.
	/// </summary>
	[System.Serializable]
	public class SproutMesh {
		#region Vars
		/// <summary>
		/// Sprout group identifier.
		/// </summary>
		public int groupId = 0;
		/// <summary>
		/// Active subgroups for this sprout mesh.
		/// </summary>
		[System.NonSerialized]
		public int[] subgroups = new int[0];
		/// <summary>
		/// Modes available to mesh sprouts.
		/// </summary>
		public enum MeshingMode {
			Shape = 0,
			//Mesh = 1,
			BranchCollection = 2
		}
		/// <summary>
		/// Mode to mesh sprouts.
		/// </summary>
		public MeshingMode meshingMode = MeshingMode.Shape;
		/// <summary>
		/// Modes available for the shape mode.
		/// </summary>
		public enum ShapeMode
		{
			Plane = 0,
			Cross = 1,
			Tricross = 2,
			//Billboard = 3, // Deprecated, mode for Unity Tree Creator
			Mesh = 4,
			PlaneX = 5,
			GridPlane = 6
		}
		/// <summary>
		/// Mode for the sprout shape mesh.
		/// </summary>
		[FormerlySerializedAs("mode")]
		public ShapeMode shapeMode = ShapeMode.Plane;
		/// <summary>
		/// The horizontal alignment (perpendicular to gravity) for sprouts at the base of the parent branch.
		/// </summary>
		public float horizontalAlignAtBase = 0f;
		/// <summary>
		/// The horizontal alignment (perpendicular to gravity) for sprouts at the top of the parent branch.
		/// </summary>
		public float horizontalAlignAtTop = 0f;
		/// <summary>
		/// The horizontal alignment curve, from base (left) to top (right).
		/// </summary>
		public AnimationCurve horizontalAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
		/// <summary>
		/// Pattern of wind to apply to the sprout.
		/// </summary>
		public enum WindPattern {
			Sprout1 = 0,
			Sprout2 = 1,
		}
		/// <summary>
		/// Pattern of wind to apply to the sprout.
		/// </summary>
		public WindPattern windPattern = WindPattern.Sprout1;
		/// <summary>
		/// Modes to calculate the scale of sprout meshes.
		/// </summary>
		public enum ScaleMode {
			Hierarchy, // The scale is based on the sprout position on the hierarchy.
			Branch, // The scale is based on the branch length (0 to 1).
			Range, // The scale is based on the sprout position on a branch limited by a range.
		}
		/// <summary>
		/// Scale mode to calculate the sprout mesh size.
		/// </summary>
		public ScaleMode scaleMode = ScaleMode.Hierarchy;
		/// <summary>
		/// The scale of the sprout mesh at the base of the parent branch.
		/// </summary>
		public float scaleAtBase = 1f;
		/// <summary>
		/// The scale of the sprout mesh at the top of the parent branch.
		/// </summary>
		public float scaleAtTop = 1f;
		/// <summary>
		/// How much the scale will randomly vary from its calculated size and
		/// within the values of scaleAtBase and scaleAtTop.
		/// </summary>
		public float scaleVariance = 0f;
		/// <summary>
		/// The scale curve.
		/// </summary>
		public AnimationCurve scaleCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
		/// <summary>
		/// Mesh created for internal use on building process.
		/// </summary>
		[System.NonSerialized]
		public Mesh processedMesh = null;
		/// <summary>
		/// Plane is double sided.
		/// </summary>
		public bool isTwoSided = true;
		/// <summary>
		/// Branch Collection asset, used to load sprout meshes to populate the tree.
		/// </summary>
		public ScriptableObject branchCollection = null;
		/// <summary>
		/// List to flag a BranchCollection items as enabled or disabled to be used as sprout meshes.
		/// The count for the list should be the same as the length of the snapshots or variations
		/// contained by the branchCollection.
		/// </summary>
		[SerializeField]
		public List<bool> branchCollectionItemsEnabled = new List<bool> ();
		/// <summary>
		/// Sets the type of BranchCollection set on this SproutMesh instance.
		/// This value is not persisted, it is set when processing the mesh.
		/// </summary>
		[System.NonSerialized]
		public int meshType = 0;
		/// <summary>
		/// Noise pattern to apply to the XY plane of the Sprout.
		/// </summary>
		public enum NoisePattern {
			None = 0, // No noise applied.
			Front = 1, // The noise is distributed to the front of the plane.
			Side = 2, // The noise is distributed to the sides of the plane.
			Combined = 3 // The noise is distributed to both the front and the sides of the plane.
		}
		/// <summary>
		/// The Sprout noise pattern.
		/// </summary>
		public NoisePattern noisePattern = NoisePattern.None;
		/// <summary>
		/// Modes to calculate the noise of sprout meshes.
		/// </summary>
		public enum NoiseDistribution {
			Hierarchy, // The noise is based on the sprout position on the hierarchy.
			Branch, // The noise is based on the branch length (0 to 1).
			Range, // The noise is based on the sprout position on a branch limited by a range.
		}
		/// <summary>
		/// Noise mode to calculate the sprout mesh size.
		/// </summary>
		public NoiseDistribution noiseDistribution = NoiseDistribution.Hierarchy;
		/// <summary>
		/// The noise resolution of the sprout mesh at the base of the parent branch.
		/// </summary>
		public float noiseResolutionAtBase = 1f;
		/// <summary>
		/// The noise resolution of the sprout mesh at the top of the parent branch.
		/// </summary>
		public float noiseResolutionAtTop = 1f;
		/// <summary>
		/// How much the noise will randomly vary from its calculated resolution and
		/// within the values of noiseResolutionAtBase and noiseResolutionAtTop.
		/// </summary>
		public float noiseResolutionVariance = 0f;
		/// <summary>
		/// The noise resolution curve.
		/// </summary>
		public AnimationCurve noiseResolutionCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
		/// <summary>
		/// The noise strength of the sprout mesh at the base of the parent branch.
		/// </summary>
		public float noiseStrengthAtBase = 1f;
		/// <summary>
		/// The noise strength of the sprout mesh at the top of the parent branch.
		/// </summary>
		public float noiseStrengthAtTop = 1f;
		/// <summary>
		/// How much the noise will randomly vary from its calculated strength and
		/// within the values of noiseStrengthAtBase and noiseStrengthAtTop.
		/// </summary>
		public float noiseStrengthVariance = 0f;
		/// <summary>
		/// The noise strength curve.
		/// </summary>
		public AnimationCurve noiseStrengthCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
		#endregion

		#region Planes mode
		/// <summary>
		/// Width of the sprout mesh.
		/// </summary>
		public float width = 1f;
		/// <summary>
		/// Height of the sprout mesh.
		/// </summary>
		public float height = 1f;
		/// <summary>
		/// Height after override texture has been applied.
		/// </summary>
		[System.NonSerialized]
		public float overridedHeight = 1f;
		/// <summary>
		/// The x coordinate on the mesh to be the sprout point of origin.
		/// </summary>
		public float pivotX = 0.5f;
		/// <summary>
		/// The y coordinate on the mesh to be the sprout point of origin.
		/// </summary>
		public float pivotY = 0f;
		/// <summary>
		/// If true then height is adjusted to the assigned texture dimension ratio.
		/// </summary>
		public bool overrideHeightWithTexture = false;
		/// <summary>
		/// If true then the texture mapping is checked against other sprouts coming from
		/// the same texture atlas; a scale is applied according to its size on the atlas
		/// relative to the biggest mapping on that particular atlas.
		/// </summary>
		public bool includeScaleFromAtlas = false;
		#endregion

		#region Mesh mode
		/// <summary>
		/// The mesh game object to copy the mesh from, default LOD.
		/// </summary>
		public GameObject meshGameObject;
		/// <summary>
		/// True to set additional meshes for LOD levels.
		/// </summary>
		public bool useMultiLOD = false;
		/// <summary>
		/// The mesh game object to copy the mesh from at LOD1.
		/// </summary>
		public GameObject meshGameObjectLOD1;
		/// <summary>
		/// The mesh game object to copy the mesh from at LOD2.
		/// </summary>
		public GameObject meshGameObjectLOD2;
		/// <summary>
		/// The mesh scale.
		/// </summary>
		public Vector3 meshScale = Vector3.one;
		/// <summary>
		/// The mesh rotation.
		/// </summary>
		public Vector3 meshRotation = Vector3.zero;
		/// <summary>
		/// Offset from the mesh center.
		/// </summary>
		public Vector3 meshOffset = Vector3.zero;
		#endregion

		#region Billboard mode
		/// <summary>
		/// If true then the billboard mesh is placed at the point of origin of the sprout.
		/// </summary>
		public bool billboardAtOrigin = false;
		/// <summary>
		/// The billboard rotation at top of the parent branch.
		/// </summary>
		public float billboardRotationAtTop = 0f;
		/// <summary>
		/// The billboard rotation at top of the parent branch.
		/// </summary>
		public float billboardRotationAtBase = 0f;
		/// <summary>
		/// The billboard rotation curve, from base (left) to top (right).
		/// </summary>
		public AnimationCurve billboardRotationCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
		#endregion

		#region Plane X
		/// <summary>
		/// Depth of the plane.
		/// </summary>
		public float depth = 0f; //TODO: add atBase, atTop and curve.
		/// <summary>
		/// The size of the inner plane.
		/// </summary>
		public float innerPlaneSize = 1f;
		#endregion

		#region Grid Plane mode
		/// <summary>
		/// Number of segments for the width of the plane.
		/// </summary>
		[Range (1, 10)]
		public int resolutionWidth = 1;
		/// <summary>
		/// Number of segments for the height of the plane.
		/// </summary>
		[Range (1,10)]
		public int resolutionHeight = 1;
		/// <summary>
		/// Bending modes to combine forward and side bending values.
		/// </summary>
		public enum BendMode {
			Add = 0,
			Multiply = 1,
			Stylized = 2
		}
		/// <summary>
		/// Bending mode to combine forward and side bending values.
		/// </summary>
		public BendMode gravityBendMode = BendMode.Add;
		/// <summary>
		/// The gravity bending (perpendicular to gravity) for sprouts at the base of the parent branch.
		/// </summary>
		public float gravityBendingAtBase = 0f;
		/// <summary>
		/// The gravity bending (perpendicular to gravity) for sprouts at the top of the parent branch.
		/// </summary>
		public float gravityBendingAtTop = 0f;
		/// <summary>
		/// The gravity bending curve, from base (left) to top (right).
		/// </summary>
		public AnimationCurve gravityBendingCurve = AnimationCurve.Linear (0f, 1f, 1f, 1f);
		/// <summary>
		/// Gravity bending on the sides along the length of the sprout mesh at the base or the parent branch.
		/// </summary>
		[Range(-1,1)]
		public float sideGravityBendingAtBase = 0f;
		/// <summary>
		/// Gravity bending on the sides along the length of the sprout mesh at the top or the parent branch.
		/// </summary>
		[Range(-1,1)]
		public float sideGravityBendingAtTop = 0f;
		/// <summary>
		/// Distribution of the side gravity bending on a sprout mesh.
		/// </summary>
		public AnimationCurve sideGravityBendingShape = AnimationCurve.Linear (0f, 0f, 1f, 1f);
		#endregion

		#region Accessors
		/// <summary>
		/// <c>True</c> if the mesh should apply a bending transformation.
		/// </summary>
		public bool hasBending {
			get {
				return (meshingMode == MeshingMode.BranchCollection) ||
					(meshingMode == MeshingMode.Shape && shapeMode == ShapeMode.GridPlane) ||
					(meshingMode == MeshingMode.Shape && shapeMode == ShapeMode.Cross);
			}
		}
		#endregion

		#region Processing
		/// <summary>
		/// Set a branch collection to be used when the SproutMode is set to BranchCollection.
		/// </summary>
		/// <param name="targetBranchCollectionSO">Branch Collection Scriptable Object to set.</param>
		public void SetBranchCollectionSO (ScriptableObject targetBranchCollectionSO) {
			branchCollection = targetBranchCollectionSO;
			branchCollectionItemsEnabled.Clear ();
			if (targetBranchCollectionSO != null && targetBranchCollectionSO is BranchDescriptorCollectionSO) {
				BranchDescriptorCollection bdc = ((BranchDescriptorCollectionSO)targetBranchCollectionSO).branchDescriptorCollection;
				int totalItems = 0;
				if (bdc != null) {
					if (bdc.descriptorImplId == BranchDescriptorCollection.BASE_COLLECTION || 
						bdc.descriptorImplId == BranchDescriptorCollection.SNAPSHOT_COLLECTION)
					{
						totalItems = bdc.snapshots.Count;
					} else if (bdc.descriptorImplId == BranchDescriptorCollection.VARIATION_COLLECTION) {
						totalItems = bdc.variations.Count;
					}
					for (int i = 0; i < totalItems; i++) {
						branchCollectionItemsEnabled.Add (true);
					}
				}
			}
		}
		#endregion

		#region Cloning
		/// <summary>
		/// Clone this instance.
		/// </summary>
		public SproutMesh Clone () {
			SproutMesh clone = new SproutMesh();
			clone.groupId = groupId;
			clone.meshingMode = meshingMode;
			clone.shapeMode = shapeMode;
			clone.meshGameObject = meshGameObject;
			clone.useMultiLOD = useMultiLOD;
			clone.meshGameObjectLOD1 = meshGameObjectLOD1;
			clone.meshGameObjectLOD2 = meshGameObjectLOD2;
			clone.meshScale = meshScale;
			clone.meshRotation = meshRotation;
			clone.meshOffset = meshOffset;
			clone.billboardAtOrigin = billboardAtOrigin;
			clone.billboardRotationAtTop = billboardRotationAtTop;
			clone.billboardRotationAtBase = billboardRotationAtBase;
			clone.billboardRotationCurve = new AnimationCurve (billboardRotationCurve.keys);
			clone.depth = depth;
			clone.innerPlaneSize = innerPlaneSize;
			clone.resolutionWidth = resolutionWidth;
			clone.resolutionHeight = resolutionHeight;
			clone.gravityBendingAtTop = gravityBendingAtTop;
			clone.gravityBendingAtBase = gravityBendingAtBase;
			clone.gravityBendingCurve = new AnimationCurve (gravityBendingCurve.keys);
			clone.width = width;
			clone.height = height;
			clone.pivotX = pivotX;
			clone.pivotY = pivotY;
			clone.overrideHeightWithTexture = overrideHeightWithTexture;
			clone.includeScaleFromAtlas = includeScaleFromAtlas;
			clone.horizontalAlignAtBase = horizontalAlignAtBase;
			clone.horizontalAlignAtTop = horizontalAlignAtTop;
			clone.horizontalAlignCurve = new AnimationCurve (horizontalAlignCurve.keys);
			clone.windPattern = windPattern;
			clone.scaleMode = scaleMode;
			clone.scaleAtBase = scaleAtBase;
			clone.scaleAtTop = scaleAtTop;
			clone.scaleVariance = scaleVariance;
			clone.scaleCurve = new AnimationCurve (scaleCurve.keys);
			clone.sideGravityBendingAtBase = sideGravityBendingAtBase;
			clone.sideGravityBendingAtTop = sideGravityBendingAtTop;
			clone.sideGravityBendingShape = new AnimationCurve (sideGravityBendingShape.keys);
			clone.branchCollection = branchCollection;
			for (int i = 0; i < branchCollectionItemsEnabled.Count; i++) {
				clone.branchCollectionItemsEnabled.Add (branchCollectionItemsEnabled [i]);
			}
			clone.noisePattern = noisePattern;
			clone.noiseDistribution = noiseDistribution;
			clone.noiseResolutionAtBase = noiseResolutionAtBase;
			clone.noiseResolutionAtTop = noiseResolutionAtTop;
			clone.noiseResolutionVariance = noiseResolutionVariance;
			clone.noiseResolutionCurve = noiseResolutionCurve;
			clone.noiseStrengthAtBase = noiseStrengthAtBase;
			clone.noiseStrengthAtTop = noiseStrengthAtTop;
			clone.noiseStrengthVariance = noiseStrengthVariance;
			clone.noiseStrengthCurve = noiseStrengthCurve;
			return clone;
		}
		#endregion
	}
}