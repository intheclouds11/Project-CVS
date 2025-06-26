using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;

using Broccoli.Base;
using Broccoli.Model;
using Broccoli.Utils;
using UnityEngine.PlayerLoop;
using System.Collections.Specialized;
using System;
using System.Runtime.Serialization.Json;
using Unity.VisualScripting;

namespace Broccoli.Builder
{
	/// <summary>
	/// Mesh building for sprouts.
	/// </summary>
	public class TrunkBuilder {
        protected class ProgressiveBranch {
            public enum Mode {
                Parent,
                Child
            }
            public Mode mode = Mode.Parent;
            public Vector2 parentCenterAtBase;
            public Vector2 parentCenterAtTop;
            public Easing.Mode parentCenterEasing;
            public float parentGirthAtBase;
            public float parentGirthAtTop;
            public AnimationCurve parentGirthCurve = new AnimationCurve();
            public float rollAngleAtBase;
            public float rollAngleAtTop;
            public float twirl;
            public float girthAtBase;
            public float girthAtTop;
            public Easing.Mode girthEasing;
            public float exposureAtBase;
            public float exposureAtTop;
            public Easing.Mode exposureEasing;
            public float from;
            public float to;
            public ProgressiveBranch (
                Vector2 centerAtBase, Vector2 centerAtTop, Easing.Mode centerEasing,
                float girthAtBase, float girthAtTop, AnimationCurve girthCurve,
                float from, float to)
            {
                this.mode = Mode.Parent;
                this.parentCenterAtBase = centerAtBase;
                this.parentCenterAtTop = centerAtTop;
                this.parentCenterEasing = centerEasing;
                this.parentGirthAtBase = girthAtBase;
                this.parentGirthAtTop = girthAtTop;
                this.parentGirthCurve = girthCurve;
                this.from = from;
                this.to = to;
            }
            public ProgressiveBranch (
                Vector2 parentCenterAtBase, Vector2 parentCenterAtTop, Easing.Mode parentCenterEasing,
                float parentGirthAtBase, float parentGirthAtTop, AnimationCurve girthCurve,
                float rollAngleAtBase, float rollAngleAtTop, float twirl,
                float girthAtBase, float girthAtTop, Easing.Mode girthEasing,
                float exposureAtBase, float exposureAtTop, Easing.Mode exposureEasing,
                float from, float to)
            {
                this.mode = Mode.Child;
                this.parentCenterAtBase = parentCenterAtBase;
                this.parentCenterAtTop = parentCenterAtTop;
                this.parentCenterEasing = parentCenterEasing;
                this.parentGirthAtBase = parentGirthAtBase;
                this.parentGirthAtTop = parentGirthAtTop;
                this.parentGirthCurve = girthCurve;
                this.rollAngleAtBase = rollAngleAtBase;
                this.rollAngleAtTop = rollAngleAtTop;
                this.twirl = twirl;
                this.girthAtBase = girthAtBase;
                this.girthAtTop = girthAtTop;
                this.girthEasing = girthEasing;
                this.exposureAtBase = exposureAtBase;
                this.exposureAtTop = exposureAtTop;
                this.exposureEasing = exposureEasing;
                this.from = from;
                this.to = to;
            }
        }
        #region Vars
        /// <summary>
        /// Modes to process roots and branches.
        /// </summary>
        public enum Mode {
            SimulateRoots,
            Roots
        }
        /// <summary>
        /// Current process mode.
        /// </summary>
        protected Mode mode = Mode.SimulateRoots;
        /// <summary>
        /// Set on pseudo mode to take the number of roots to simulate.
        /// </summary>
        protected int rootCount = 0;
        /// <summary>
        /// How much are roots radially distanced at the base of the trunk.
        /// Only taken in Pseudo because the placement of roots at the base is given
        /// by exiting root branches in other modes.
        /// </summary>
        protected float angleVarianceAtBase = 0;
        /// <summary>
        /// How much are roots radially distances at the top of the trunk.
        /// The place is modified for existing roots when not simulated.
        /// </summary>
        protected float angleVarianceAtTop = 0;
        protected Vector2 scaleAtBase = Vector2.one;
        protected Vector2 scaleAtTop = Vector2.one;
        protected Vector2 exposureAtBase = Vector2.zero;
        protected Vector2 exposureAtTop = Vector2.zero;
        protected Vector2 reach = Vector2.zero;
        protected float minReach = 0.8f;
        protected float maxReach = 1f;
        protected float twirl = 0;
        /// <summary>
        /// Length of the trunk to simulate.
        /// </summary>
        protected float trunkLength;
        /// <summary>
        /// Girth for the trunk at the base.
        /// </summary>
        protected float trunkGirthAtBase;
        /// <summary>
        /// Girth for the trunk at the top.
        /// </summary>
        protected float trunkGirthAtTop;
        /// <summary>
        /// Girth curve to transition from base to top.
        /// </summary>
        protected AnimationCurve trunkGirthCurve;
        /// <summary>
        /// Scale factor to apply to the trunk points.
        /// </summary>
        protected float trunkScale;
        protected List<ProgressiveBranch> progressiveBranches = new List<ProgressiveBranch>();
        protected MarchingSquaresCircles marchingSquaresCircles = new MarchingSquaresCircles ();
        protected MarchingSquaresCircles marchingSquaresCirclesCtrl = new MarchingSquaresCircles ();
        #endregion

        #region Constructor
        public TrunkBuilder (float trunkLength, float trunkGirthAtBase, float trunkGirthAtTop, AnimationCurve trunkGirthCurve, float trunkScale) {
            this.mode = Mode.Roots;
            this.trunkLength = trunkLength;
            this.trunkGirthAtBase = trunkGirthAtBase;
            this.trunkGirthAtTop = trunkGirthAtTop;
            this.trunkGirthCurve = trunkGirthCurve;
            this.trunkScale = trunkScale;
        }
        #endregion

        #region Configuration
        public void InitRootsMode (
            BroccoTree.Branch trunk,
            float angleVarianceAtTop,
            Vector2 scaleAtBase,
            Vector2 scaleAtTop,
            Vector2 exposureAtBase,
            Vector2 exposureAtTop,
            Vector2 reach,
            float twirl)
        {
            this.angleVarianceAtTop = angleVarianceAtTop;
            this.scaleAtBase = scaleAtBase;
            this.scaleAtTop = scaleAtTop;
            this.exposureAtBase = exposureAtBase;
            this.exposureAtTop = exposureAtTop;
            this.reach = reach;
            this.twirl = twirl;
            InitializeMarchingSquares ();
            InitializeTrunk (trunk);
        }
        public void InitSimulateRootsMode (
            BroccoTree.Branch trunk,
            int rootCount, 
            float angleVarianceAtBase, 
            float angleVarianceAtTop,
            Vector2 scaleAtBase,
            Vector2 scaleAtTop,
            Vector2 exposureAtBase,
            Vector2 exposureAtTop,
            Vector2 reach,
            float twirl)
        {
            this.mode = Mode.SimulateRoots;
            this.rootCount = rootCount;
            this.angleVarianceAtBase = angleVarianceAtBase;
            this.angleVarianceAtTop = angleVarianceAtTop;
            this.scaleAtBase = scaleAtBase;
            this.scaleAtTop = scaleAtTop;
            this.exposureAtBase = exposureAtBase;
            this.exposureAtTop = exposureAtTop;
            this.reach = reach;
            this.twirl = twirl;
            InitializeMarchingSquares ();
            InitializeTrunk (trunk);
        }
        #endregion

        #region Segments
        public void GetSegment (
            float length, 
            int numSteps, 
            ref List<Vector3> points,
            ref List<Vector3> normals,
            ref List<float> radialPos)
        {
            ProgressiveBranch progressiveBranch;
            float position;
            float absolutePosition;
            float ctrlRadius = 1f;
            float parentRadius = 0f;
            Vector2 parentCenter = Vector2.zero;
            Vector2 childCenter = Vector2.zero;
            // 1. Modify all the progressive braches (roots and branches affecting the trunk shape) on the marching squares utility.
            for (int i = 0; i < progressiveBranches.Count; i++) {
                progressiveBranch = progressiveBranches [i];
                position = length / (this.trunkLength * progressiveBranch.to);
                absolutePosition = length /this.trunkLength;
                if (position > 1f) {
                    position = 1f;
                }
                if (absolutePosition > 1f) {
                    absolutePosition = 1f;
                }
                if (progressiveBranch.mode == ProgressiveBranch.Mode.Parent) {
                    parentCenter = Easing.Ease (progressiveBranch.parentCenterEasing, progressiveBranch.parentCenterAtBase, progressiveBranch.parentCenterAtTop, absolutePosition) * trunkScale;
                    parentRadius =  Mathf.Lerp (progressiveBranch.parentGirthAtBase, progressiveBranch.parentGirthAtTop, trunkGirthCurve.Evaluate (absolutePosition)) * trunkScale;
                    marchingSquaresCircles.ModifyCircle (
                        i, 
                        parentCenter,
                        parentRadius,
                        false
                    );
                } else {
                    parentCenter = Easing.Ease (progressiveBranch.parentCenterEasing, progressiveBranch.parentCenterAtBase, progressiveBranch.parentCenterAtTop, absolutePosition) * trunkScale;
                    parentRadius =  Mathf.Lerp (progressiveBranch.parentGirthAtBase, progressiveBranch.parentGirthAtTop, trunkGirthCurve.Evaluate (absolutePosition)) * trunkScale;
                    float twirlOffset = Mathf.Lerp (0f, Mathf.PI * 0.5f * twirl, absolutePosition);
                    float rollAngle = Mathf.Lerp (progressiveBranch.rollAngleAtBase, progressiveBranch.rollAngleAtTop, position) + twirlOffset;
                    float childRadius = Mathf.Lerp (progressiveBranch.girthAtBase, progressiveBranch.girthAtTop, position);
                    float exposureOffset = Mathf.Lerp (progressiveBranch.exposureAtBase, progressiveBranch.exposureAtTop, position) - 0.5f;
                    childCenter.x = Mathf.Cos (rollAngle) * (parentRadius + childRadius * exposureOffset * 2f);
                    childCenter.y = Mathf.Sin (rollAngle) * (parentRadius + childRadius * exposureOffset * 2f);
                    marchingSquaresCircles.ModifyCircle (
                        i, 
                        childCenter,
                        childRadius,
                        false
                    );
                }
                // Modify ctrl reference.
                if (i == 0) {
                    ctrlRadius = parentRadius;
                    marchingSquaresCirclesCtrl.ModifyCircle (
                        i, 
                        parentCenter,
                        parentRadius,
                        false
                    );
                }
            }

            // 1.1 Debug draw trunk segments.
            //DebugDrawMarchingSquares (length);

            // 2. Run marching squares and ctrl marching squares.
            marchingSquaresCirclesCtrl.MarchingSquares ();
            marchingSquaresCircles.MarchingSquares ();

            //DebugDrawMarchingSquaresPaths (length);

            // 3. Get ctrl factor to adjust the path.
            float ctrlFactor = 1f;
            if (marchingSquaresCirclesCtrl.paths.Count > 0) {
                float greatMag = marchingSquaresCirclesCtrl.paths[0][0].magnitude;
                ctrlFactor = ctrlRadius / greatMag;
            }

            // 4. Add the resulting path.
            Vector2 point, normal;
            float distance;
            if (marchingSquaresCircles.paths.Count > 0) {
                List<Vector2> path = marchingSquaresCircles.paths [0];
                float stepAngle = Mathf.PI * 2f / (float)numSteps;
                float step = 1f / numSteps;
                for (int i = 0; i <= numSteps; i++) {
                    marchingSquaresCircles.GetPoint (0, i * step, true, out point, out normal, out distance);
                    points.Add (new Vector3 (point.x * ctrlFactor, point.y * ctrlFactor));
                    normals.Add (normal);
                    radialPos.Add (stepAngle * i);
                }
            }
        }
        #endregion

        #region Debug
        Vector3 circlesCenter = new Vector3 (1.5f, 0, 1.5f);
        int circleResolution = 10;
        float displayTime = 5f;
        List<Color> debugColors = new List<Color>() {
            Color.white,
            Color.yellow,
            Color.Lerp (Color.yellow, Color.red, 0.33f),
            Color.Lerp (Color.yellow, Color.red, 0.66f),
            Color.red,
            Color.Lerp (Color.red, Color.blue, 0.33f),
            Color.Lerp (Color.red, Color.blue, 0.66f),
            Color.blue
        };
        private void DebugDrawMarchingSquares (float height)
        {
            circlesCenter.y = height;
            float angleStep = Mathf.PI * 2f / (float)circleResolution;
            Vector3 lastPoint = Vector3.zero;
            Vector3 currentPoint = Vector3.zero;
            int iColor = 0;
            foreach (Vector3 circle in marchingSquaresCircles.Circles) {
                for (int i = 0; i <= circleResolution; i++) {
                    currentPoint.x = circle.x + Mathf.Cos (angleStep * i) * circle.z;
                    currentPoint.y = 0;
                    currentPoint.z = circle.y + Mathf.Sin (angleStep * i) * circle.z;
                    if (i > 0) {
                        Debug.DrawLine (lastPoint + circlesCenter, currentPoint + circlesCenter, debugColors[iColor], displayTime);
                    }
                    lastPoint = currentPoint;
                }
                iColor++;
                if (iColor >= debugColors.Count) iColor = 0;
            }
        }

        private void DebugDrawMarchingSquaresPaths (float height)
        {
            circlesCenter.y = height;
            Vector3 lastPoint = Vector3.zero;
            Vector3 currentPoint;
            int iColor = 0;
            foreach (List<Vector2> path in marchingSquaresCircles.paths) {
                int i = 0;
                foreach (Vector3 point in path) {
                    currentPoint = new Vector3 (point.x, 0, point.y);
                    if (i > 0) {
                        Debug.DrawLine (lastPoint + circlesCenter, currentPoint + circlesCenter, Color.magenta, displayTime);
                    }
                    lastPoint = currentPoint;
                    i++;
                }
                iColor++;
                if (iColor >= debugColors.Count) iColor = 0;
            }
        }
        #endregion

        #region Process
        private void InitializeMarchingSquares () {
            marchingSquaresCircles.gridCells = 35;
            marchingSquaresCircles.applySmoothPath = true;
			marchingSquaresCircles.smoothPathIterations = 10;
			marchingSquaresCircles.cardinal = MarchingSquaresCircles.Cardinal.East;
			marchingSquaresCircles.cardinalCenter = Vector3.zero;
            marchingSquaresCircles.normalType = MarchingSquaresCircles.NormalType.Curve;
			marchingSquaresCircles.ClearCircles ();

            marchingSquaresCirclesCtrl.gridCells = 35;
            marchingSquaresCirclesCtrl.applySmoothPath = true;
			marchingSquaresCirclesCtrl.smoothPathIterations = 10;
			marchingSquaresCirclesCtrl.cardinal = MarchingSquaresCircles.Cardinal.East;
			marchingSquaresCirclesCtrl.cardinalCenter = Vector3.zero;
			marchingSquaresCirclesCtrl.ClearCircles ();
        }
        private void InitializeTrunk (BroccoTree.Branch trunk) {
            // Register the trunk as index 0 on this instance.
            ProgressiveBranch progressiveTrunk = 
                new ProgressiveBranch (Vector3.zero, Vector3.zero, Easing.Mode.Linear,
                    trunkGirthAtBase, trunkGirthAtTop, trunkGirthCurve,
                    0f, 1f);
            progressiveBranches.Add (progressiveTrunk);

            // Add to the trunk to the Marching Squares Circles Utility.
            marchingSquaresCircles.AddCircle (progressiveTrunk.parentCenterAtBase * trunkScale, progressiveTrunk.girthAtBase * trunkScale, false);
            marchingSquaresCirclesCtrl.AddCircle (progressiveTrunk.parentCenterAtBase * trunkScale, progressiveTrunk.girthAtBase * trunkScale, false);

            // Add roots to the Marching Squares Utility.
            if (mode == Mode.SimulateRoots) {
                if (rootCount > 0) {
                    float stepAngle = Mathf.PI * 2f / rootCount;
                    float radialAngleAtBase = 0f;
                    float radialAngleAtTop = 0f;
                    float girthAtBase = 0.5f;
                    float girthAtTop = 0.5f;
                    float _exposureAtBase = 0.5f;
                    float _exposureAtTop = 0.1f;
                    float _reach = 0.8f;
                    float rollOffset = UnityEngine.Random.Range (0f, Mathf.PI * 2);
                    for (int rootI = 0; rootI < rootCount; rootI++) {
                        radialAngleAtBase = stepAngle * rootI + UnityEngine.Random.Range (-angleVarianceAtBase * 2f, angleVarianceAtBase * 2f) + rollOffset;
                        radialAngleAtTop = stepAngle * rootI + UnityEngine.Random.Range (-angleVarianceAtTop * 2f, angleVarianceAtTop * 2f) + rollOffset;
                        girthAtBase = trunkGirthAtBase * UnityEngine.Random.Range (scaleAtBase.x, scaleAtBase.y);
                        girthAtTop = girthAtBase * UnityEngine.Random.Range (scaleAtTop.x, scaleAtTop.y);
                        _exposureAtBase = UnityEngine.Random.Range (exposureAtBase.x, exposureAtBase.y);
                        _exposureAtTop = UnityEngine.Random.Range (exposureAtTop.x, exposureAtTop.y);
                        _reach = UnityEngine.Random.Range (reach.x, reach.y);

                        ProgressiveBranch progressiveBranch = new ProgressiveBranch (
                            Vector2.zero, Vector2.zero, Easing.Mode.Linear,
                            trunkGirthAtBase, trunkGirthAtTop, trunkGirthCurve,
                            radialAngleAtBase, radialAngleAtTop, twirl,
                            girthAtBase, girthAtTop, Easing.Mode.Linear,
                            _exposureAtBase, _exposureAtTop, Easing.Mode.Linear,
                            0f, _reach);

                        progressiveBranches.Add (progressiveBranch);
                        marchingSquaresCircles.AddCircle (progressiveBranch.parentCenterAtBase * trunkScale, progressiveBranch.girthAtBase * trunkScale, false);
                    }
                }
            } else {
                BroccoTree.Branch root;
                float radialAngleAtBase = 0f;
                float radialAngleAtTop = 0f;
                float girthAtBase = 0.5f;
                float girthAtTop = 0.5f;
                float _exposureAtBase = 0.5f;
                float _exposureAtTop = 0.1f;
                float _reach = 0.8f;
                foreach (var branch in trunk.branches) {
                    if (branch.isRoot) {
                        root = branch;

                        radialAngleAtBase = root.rollAngle;
                        radialAngleAtTop = root.rollAngle;
                        girthAtBase = root.GetGirthAtPosition (0f) * UnityEngine.Random.Range (scaleAtBase.x, scaleAtBase.y);
                        girthAtTop = girthAtBase * UnityEngine.Random.Range (scaleAtTop.x, scaleAtTop.y);
                        _exposureAtBase = UnityEngine.Random.Range (exposureAtBase.x, exposureAtBase.y);
                        _exposureAtTop = UnityEngine.Random.Range (exposureAtTop.x, exposureAtTop.y);
                        _reach = UnityEngine.Random.Range (reach.x, reach.y);

                        ProgressiveBranch progressiveBranch = new ProgressiveBranch (
                            Vector2.zero, Vector2.zero, Easing.Mode.Linear,
                            trunkGirthAtBase, trunkGirthAtTop, trunkGirthCurve,
                            radialAngleAtBase, radialAngleAtTop, twirl,
                            girthAtBase, girthAtTop, Easing.Mode.Linear,
                            _exposureAtBase, _exposureAtTop, Easing.Mode.Linear,
                            0f, _reach);

                        progressiveBranches.Add (progressiveBranch);
                        marchingSquaresCircles.AddCircle (progressiveBranch.parentCenterAtBase * trunkScale, progressiveBranch.girthAtBase * trunkScale, false);
                        /*
                        float xPos = Mathf.Cos (root.rollAngle) * trunkGirthAtBase * 1.3f;
                        float yPos = Mathf.Sin (root.rollAngle) * trunkGirthAtBase * 1.3f;
                        Vector2 basePosition = new Vector2 (xPos, yPos);
                        //Vector2 topPosition = Vector2.Lerp (basePosition, Vector2.zero, Random.Range (0.4f, 0.8f));
                        Vector2 topPosition = Vector2.zero;
                        ProgressiveBranch progressiveRoot = new ProgressiveBranch (
                            basePosition, 
                            topPosition, 
                            Easing.Mode.Linear, 
                            root.GetGirthAtPosition (0f), 
                            root.GetGirthAtPosition (0f) * 0.5f, 
                            Easing.Mode.Linear,
                            0f,
                            1f);
                        progressiveBranches.Add (progressiveRoot);
                        marchingSquaresCircles.AddCircle (progressiveRoot.parentCenterAtBase * trunkScale, progressiveRoot.girthAtBase * trunkScale, false);
                        */
                    }
                }
            }
        }
        #endregion
	}
}