using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

namespace Broccoli.Pipe {
	/// <summary>
	/// Girth transform element.
	/// </summary>
	[System.Serializable]
	public class TrunkMeshGeneratorElement : PipelineElement {
		#region Vars
		/// <summary>
		/// Gets the type of the connection.
		/// </summary>
		/// <value>The type of the connection.</value>
		public override ConnectionType connectionType {
			get { return PipelineElement.ConnectionType.Transform; }
		}
		/// <summary>
		/// Gets the type of the element.
		/// </summary>
		/// <value>The type of the element.</value>
		public override ElementType elementType {
			get { return PipelineElement.ElementType.MeshGenerator; }
		}
		/// <summary>
		/// Gets the type of the class.
		/// </summary>
		/// <value>The type of the class.</value>
		public override ClassType classType {
			get { return PipelineElement.ClassType.TrunkMeshGenerator; }
		}
		/// <summary>
		/// Value used to position elements in the pipeline. The greater the more towards the end of the pipeline.
		/// </summary>
		/// <value>The position weight.</value>
		public override int positionWeight {
			get { return PipelineElement.meshGeneratorWeight + 10; }
		}
		/// <summary>
		/// Integration mode used for the trunk mesh to simulate or blend with existing roots.
		/// </summary>
		public enum IntegrationMode {
			/// <summary>
			/// No roots effect on the trunk mesh.
			/// </summary>
			None = 0,
			/// <summary>
			/// Even if the tree structure has no roots, the trunk mesh simulates crest efects from simulated roots.
			/// </summary>
			SimulateRoots = 1,
			/// <summary>
			/// Each root coming from the trunk applies a crest effect on its mesh surface.
			/// </summary>
			Adaptative = 2,
		}
		/// <summary>
		/// Default integration mode.
		/// </summary>
		public IntegrationMode integrationMode = IntegrationMode.Adaptative;
		/// <summary>
		/// How much the trunk mesh spreads across the main trunk structure, minimum range to use in randomization.
		/// </summary>
		[Range (0,1)]
		public float minSpread = 0.2f;
		/// <summary>
		/// How much the trunk mesh spreads across the main trunk structure, maximum range to use in randomization.
		/// </summary>
		[Range (0,1)]
		public float maxSpread = 0.4f;
		/// <summary>
		/// When using the SimulateRoots IntegrationMode, the minimum number of roots to simulate on the trunk mesh.
		/// </summary>
		[Range (1, 15)]
		[FormerlySerializedAs("minDisplacementPoints")]
		public int minRootsCount = 3;
		/// <summary>
		/// When using the SimulateRoots IntegrationMode, the maximum number of roots to simulare on the trunk mesh.
		/// </summary>
		[Range (1, 15)]
		[FormerlySerializedAs("maxDisplacementPoints")]
		public int maxRootsCount = 6;
		/// <summary>
		/// Minimum scale for simulated roots from the total girth of the parent trunk, or scale applied to existing roots at the base of the trunk structure.
		/// </summary>
		[Range (0.1f, 1f)]
		public float minRootScaleAtBase = 0.3f;
		/// <summary>
		/// Maximum scale for simulated roots from the total girth of the parent trunk, or scale applied to existing roots at the base of the trunk structure.
		/// </summary>
		[Range (0.1f, 1f)]
		public float maxRootScaleAtBase = 0.6f;
		/// <summary>
		/// Minimum scale to apply to girth of root crests on the trunk at the top of the trunk spread range.
		/// </summary>
		[Range (0.1f, 1f)]
		public float minRootScaleAtTop = 0.3f;
		/// <summary>
		/// Maximum scale to apply to girth of root crests on the trunk at the top of the trunk spread range.
		/// </summary>
		[Range (0.1f, 1f)]
		public float maxRootScaleAtTop = 0.6f;
		/// <summary>
		/// Minimum strength to apply to the crest to protrude from the trunk mesh at the base of the trunk structure.
		/// </summary>
		[Range (0.1f, 0.95f)]
		public float minRootExposureAtBase = 0.5f;
		/// <summary>
		/// Maximum strength to apply to the crest to protrude from the trunk mesh at the base of the trunk structure.
		/// </summary>
		[Range (0.1f, 0.95f)]
		public float maxRootExposureAtBase = 0.5f;
		/// <summary>
		/// Minimum strength to apply to the crest to protrude from the trunk mesh at the upper limit of the trunk spread range.
		/// </summary>
		[Range (0.1f, 0.9f)]
		public float minRootExposureAtTop = 0.1f;
		/// <summary>
		/// Maximum strength to apply to the crest to protrude from the trunk mesh at the upper limit of the trunk spread range.
		/// </summary>
		[Range (0.1f, 0.9f)]
		public float maxRootExposureAtTop = 0.1f;
		/// <summary>
		/// Minimum length for a root crest to extent along the trunk spread range (1 being the upper limit of the range).
		/// </summary>
		[Range (0.2f, 1f)]
		public float minRootReach = 0.8f;
		/// <summary>
		/// Maximum length for a root crest to extent along the trunk spread range (1 being the upper limit of the range).
		/// </summary>
		[Range (0.2f, 1f)]
		public float maxRootReach = 1f;
		/// <summary>
		/// When applying the SimulateRoots IntegrationMode the minimum randomization between the angles of neighbour roots.
		/// </summary>
		[Range (0f, 0.5f)]
		[FormerlySerializedAs("minDisplacementAngleVariance")]
		public float minAngleVariance = 0.1f;
		/// <summary>
		/// When applying the SimulateRoots IntegrationMode the maximum randomization between the angles of neighbour roots.
		/// </summary>
		[Range (0f, 1f)]
		[FormerlySerializedAs("maxDisplacementAngleVariance")]
		public float maxAngleVariance = 0.5f;
		/// <summary>
		/// Minimum value to twirl the crest along the trunk spread range.
		/// </summary>
		[Range (-2f, 2f)]
		[FormerlySerializedAs("minDisplacementTwirl")]
		public float minTwirl = 0f;
		/// <summary>
		/// Maximum value to twirl the crest along the trunk spread range.
		/// </summary>
		[Range (-2f, 2f)]
		[FormerlySerializedAs("maxDisplacementTwirl")]
		public float maxTwirl = 0f;
		/// <summary>
		/// Scaling factor to use at the base of the trunk, minimum value in the randomized range.
		/// </summary>s
		[Range (0.75f, 2f)]
		[FormerlySerializedAs("minDisplacementScaleAtBase")]
		public float minTrunkScaleAtBase = 1.2f;
		/// <summary>
		/// Scaling factor to use at the base of the trunk, maximum value in the randomized range.
		/// </summary>
		[Range (1f,3f)]
		[FormerlySerializedAs("maxDisplacementScaleAtBase")]
		public float maxTrunkScaleAtBase = 1.8f;
		/// <summary>
		/// The transition curve for scale.
		/// </summary>
		public AnimationCurve scaleCurve = 
			AnimationCurve.Linear(0f, 0f, 1f, 1f);
		/// <summary>
		/// How much the trunk crests reflect on the mesh.
		/// </summary>
		[Range (0,1)]
		public float strength = 0.3f;
		/// <summary>
		/// Resolution factor to increase the number of sides in a cross section of the trunk mesh.
		/// </summary>
		[Range (1f,3f)]
		public float radialResolutionFactor = 1f;
		/// <summary>
		/// Resolution factor to increate the number of sections in the trunk range length.
		/// </summary>
		[Range (1f,4f)]
		public float lengthResolutionFactor = 1f;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Broccoli.Pipe.TrunkMeshGeneratorElement"/> class.
		/// </summary>
		public TrunkMeshGeneratorElement () {
			this.elementName = "Trunk Mesh Generator";
			this.elementHelpURL = "https://docs.google.com/document/d/1Nr6Z808i7X2zMFq8PELezPuSJNP5IvRx9C5lJxZ_Z-A/edit#heading=h.qpkoisw82dlr";
			this.elementDescription = "This nodes provides parameters to build the trunk mesh of a tree. This section of the tree often" +
				" has displacement coming from the roots.";
		}
		#endregion

		#region Cloning
		/// <summary>
		/// Clone this instance.
		/// </summary>
		/// <param name="isDuplicate">If <c>true</c> then the clone has elements with new ids.</param>
		/// <returns>Clone of this instance.</returns>
		override public PipelineElement Clone (bool isDuplicate = false) {
			TrunkMeshGeneratorElement clone = ScriptableObject.CreateInstance<TrunkMeshGeneratorElement> ();
			SetCloneProperties (clone, isDuplicate);
			//clone.rootMode = rootMode;
			clone.integrationMode = integrationMode;
			clone.minSpread = minSpread;
			clone.maxSpread = maxSpread;
			clone.minRootsCount = minRootsCount;
			clone.maxRootsCount = maxRootsCount;
			clone.minRootScaleAtBase = minRootScaleAtBase;
			clone.maxRootScaleAtBase = maxRootScaleAtBase;
			clone.minRootScaleAtTop = minRootScaleAtTop;
			clone.maxRootScaleAtTop = maxRootScaleAtTop;
			clone.minRootExposureAtBase = minRootExposureAtBase;
			clone.maxRootExposureAtBase = maxRootExposureAtBase;
			clone.minRootExposureAtTop = minRootExposureAtTop;
			clone.maxRootExposureAtTop = maxRootExposureAtTop;
			clone.minRootReach = minRootReach;
			clone.maxRootReach = maxRootReach;
			clone.minAngleVariance = minAngleVariance;
			clone.maxAngleVariance = maxAngleVariance;
			clone.minTwirl = minTwirl;
			clone.maxTwirl = maxTwirl;
			clone.minTrunkScaleAtBase = minTrunkScaleAtBase;
			clone.maxTrunkScaleAtBase = maxTrunkScaleAtBase;
			clone.scaleCurve = new AnimationCurve(scaleCurve.keys);
			clone.strength = strength;
			clone.radialResolutionFactor = radialResolutionFactor;
			clone.lengthResolutionFactor = lengthResolutionFactor;
			return clone;
		}
		#endregion
	}
}