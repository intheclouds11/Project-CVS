using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Model;
using System.Net.WebSockets;

namespace Broccoli.Builder {
    /// <summary>
    /// This class provides methods to build the surface of a branch.
    /// If an instance of an implementation of this class is set on a branch,
    /// methods to get the girth, normal and surface point at at a given position
    /// will be privided by this instance.
    /// </summary>
    [BranchShaper (BranchShaper.CUSTOM_SHAPER, "Custom", 1, false)]
    public class CustomBranchShaper : BranchShaper {
        #region Vars
        /// <summary>
        /// Initialization flag.
        /// </summary>
        private bool _isInit = false;
        public List<float> segPos = new List<float>();
        private Dictionary<float, int> segToIndex = new Dictionary<float, int> ();
        public List<List<float>> radialPositions = new List<List<float>>();
        public List<List<Vector3>> points = new List<List<Vector3>>();
        public List<List<Vector3>> normals = new List<List<Vector3>>();
        private Dictionary<float, List<Vector3>> cachedNormals = new Dictionary<float, List<Vector3>> ();
        private Dictionary<float, List<Vector3>> cachedPoints = new Dictionary<float, List<Vector3>> ();
        #endregion

        #region Methods
        /// <summary>
        /// Initializes this branch shaper. Called everytime the shaper gets set in a branch.
        /// </summary>
        public override void Init () {
            if (!_isInit) {
                _isInit = true;
            }
        }
        public float GetNearestPosition (float position) {
            int posIndex = segPos.BinarySearch (position);
            // Exact position match
            if (posIndex < 0) {
                // No exact match, find closest
                int insertionPoint = ~posIndex;

                if (insertionPoint == 0) {
                    // Target is smaller than the first element
                    posIndex = 0;
                }
                else if (insertionPoint == segPos.Count) {
                    // Target is larger than the last element
                    posIndex = segPos.Count - 1;
                }
                else {
                    // Compare distances to neighbors
                    float distanceToPrev = Mathf.Abs(position - segPos[insertionPoint - 1]);
                    float distanceToNext = Mathf.Abs(position - segPos[insertionPoint]);

                    posIndex = distanceToPrev < distanceToNext ? insertionPoint - 1 : insertionPoint;
                }   
            }
            return segPos[posIndex];
        }
        private bool GetNormals (float position, out List<Vector3> outNormals, BroccoTree.Branch branch) {
            if (position < 0f) position = 0f;
            else if (position > 1f) position = 1f;
            // First see if the vector list has already been cached for the position, if so return this value.
            if (cachedNormals.ContainsKey (position)) {
                outNormals = cachedNormals [position];
                if (outNormals == null) return false;
                return true;
            }
            // If not cached, search the list or generate/cache.
            float nearPos = GetNearestPosition (position);
            if (nearPos == position) {
                if (segToIndex[nearPos] < 0) { 
                    outNormals = null; 
                    cachedNormals[position] = null;  
                    return false;
                } else { 
                    outNormals = normals [segToIndex[nearPos]]; return true; 
                }
            } else {
                float sidePos;
                int nearDataI, sideDataI;
                if (position > nearPos) sidePos = segPos[segPos.IndexOf (nearPos) + 1];
                else sidePos = segPos[segPos.IndexOf (nearPos) - 1];

                nearDataI = segToIndex[nearPos];
                sideDataI = segToIndex[sidePos];
                // If either of the near of side positions are default, return false.
                if (nearDataI < 0 || sideDataI < 0) {
                    outNormals = null;
                    cachedNormals[position] = null;
                    return false;
                }
                // If both near and side positions are custom, lerp the lists.
                if (nearDataI >= 0 && sideDataI >= 0) {
                    outNormals = LerpVectorLists (position, nearPos, sidePos, normals[nearDataI], normals[sideDataI], normals[nearDataI].Count);
                    cachedNormals[position] = outNormals;
                    return true;
                }
                /*
                // If either the near or side position is a default pos
                if (nearDataI < 0 ^ sideDataI < 0) {
                    List<Vector3> nearList;
                    List<Vector3> sideList;
                    // If near is a default segment. Create a list and using with lerp with custom side.
                    if (nearDataI < 0) {
                        nearList = GetDefaultNormals (nearPos, normals[sideDataI].Count - 1, branch);
                        sideList = normals[sideDataI];
                    }
                    // If side is a default segment. Create a list and using with lerp with custom near.
                    else {
                        sideList = GetDefaultNormals (sidePos, normals[nearDataI].Count - 1, branch);
                        nearList = normals[nearDataI];
                    }
                    outNormals = LerpVectorLists (position, nearPos, sidePos, nearList, sideList, nearList.Count);
                    cachedNormals[position] = outNormals;
                    return true;
                }
                */
            }
            outNormals = null; return false;
        }
        private bool GetPoints (float position, out List<Vector3> outPoints, BroccoTree.Branch branch) {
            // First see if the vector list has already been cached for the position, if so return this value.
            if (cachedPoints.ContainsKey (position)) {
                outPoints = cachedPoints [position];
                if (outPoints == null) return false;
                return true;
            }
            // If not cached, search the list or generate/cache.
            float nearPos = GetNearestPosition (position);
            if (ApproximatelyEqual (nearPos, position, 0.001f)) {
                if (segToIndex[nearPos] < 0) { 
                    outPoints = null; 
                    cachedPoints[position] = null;  
                    return false;
                } else { 
                    outPoints = points [segToIndex[nearPos]]; return true; 
                }
            } else {
                float sidePos;
                int nearDataI, sideDataI;
                if (position > nearPos) sidePos = segPos[segPos.IndexOf (nearPos) + 1];
                else sidePos = segPos[segPos.IndexOf (nearPos) - 1];

                nearDataI = segToIndex[nearPos];
                sideDataI = segToIndex[sidePos];
                // If both near and side positions are default, return false.
                if (nearDataI < 0 && sideDataI < 0) {
                    outPoints = null;
                    cachedPoints[position] = null;
                    return false;
                }
                // If both near and side positions are custom, lerp the lists.
                if (nearDataI >= 0 && sideDataI >= 0) {
                    outPoints = LerpVectorLists (position, nearPos, sidePos, points[nearDataI], points[sideDataI], points[nearDataI].Count);
                    cachedPoints[position] = outPoints;
                    return true;
                }
                // If either the near or side position is a default pos
                if (nearDataI < 0 ^ sideDataI < 0) {
                    List<Vector3> nearList;
                    List<Vector3> sideList;
                    // If near is a default segment. Create a list and using with lerp with custom side.
                    if (nearDataI < 0) {
                        nearList = GetDefaultPoints (nearPos, points[sideDataI].Count - 1, branch);
                        sideList = points[sideDataI];
                    }
                    // If side is a default segment. Create a list and using with lerp with custom near.
                    else {
                        sideList = GetDefaultPoints (sidePos, points[nearDataI].Count - 1, branch);
                        nearList = points[nearDataI];
                    }
                    outPoints = LerpVectorLists (position, nearPos, sidePos, nearList, sideList, nearList.Count);
                    cachedPoints[position] = outPoints;
                    return true;
                }
            }
            outPoints = null; return false;
        }
        private List<Vector3> GetDefaultNormals (float pos, int numSegments, BroccoTree.Branch branch) {
            List<Vector3> result = new List<Vector3> ();
            float stepAngle = Mathf.PI * 2f / numSegments;
            float rollAngle;
            for (int i = 0; i <= numSegments; i++) {
                rollAngle = i * stepAngle;
                if (rollAngle == 0f) {
                    result.Add (branch.curve.GetPointAt (pos).normal);
                } else {
                    CurvePoint point = branch.curve.GetPointAt (pos);
                    result.Add (Quaternion.AngleAxis (rollAngle * Mathf.Rad2Deg, point.forward) * point.normal);
                }
            }
            return result;
        }
        private List<Vector3> GetDefaultPoints (float pos, int numSegments, BroccoTree.Branch branch) {
            List<Vector3> result = new List<Vector3> ();
            float stepAngle = Mathf.PI * 2f / numSegments;
            float rollAngle;
            for (int i = 0; i <= numSegments; i++) {
                rollAngle = i * stepAngle;
                float baseGirth = branch.GetGirthAtPosition (pos);
                result.Add (GetDefaultSurfacePoint (baseGirth, rollAngle));
            }
            return result;
        }
        public static bool ApproximatelyEqual(float a, float b, float tolerance) {
            return Mathf.Abs(a - b) <= tolerance;
        }
        private List<Vector3> LerpVectorLists (float pos, float posA, float posB, List<Vector3> listA, List<Vector3> listB, float numSteps) {
            List<Vector3> result = new List<Vector3>();
            float stepAngle = Mathf.PI * 2f / numSteps;
            float radialPos;
            Vector3 pointA, pointB;
            for (int i = 0; i <= numSteps; i++) {
                radialPos = stepAngle * i;
                pointA = GetVectorAtRadialPosition (radialPos, listA);
                pointB = GetVectorAtRadialPosition (radialPos, listB);
                result.Add (Vector3.Lerp (pointA, pointB, Mathf.InverseLerp (posA, posB, pos)));
            }
            return result;
        }
        private Vector3 GetVectorAtRadialPosition (float radialPos, List<Vector3> points, List<float> radialPositions = null) {
            Vector3 point = new Vector3();
            if (radialPositions == null) {
                float normalizedRadialPos = NormalizeAngle (radialPos);
                if (ApproximatelyEqual (normalizedRadialPos, 0f, Mathf.Epsilon)) return points[0];
                float stepAngle = Mathf.PI * 2f / (points.Count - 1);
                for (int i = 0; i < points.Count; i++) {
                    if (i * stepAngle >= normalizedRadialPos) {
                        return Vector3.Lerp (points[i - 1], points[i], Mathf.InverseLerp ((i - 1) * stepAngle, i * stepAngle, normalizedRadialPos));
                    }
                }
            }
            return point;
        }
        public static float NormalizeAngle (float angle) {
            angle = angle % (Mathf.PI * 2); // Modulo operation to wrap around
            if (angle < 0) {
                angle += Mathf.PI * 2; // Add 2*PI if the angle is negative
            }
            return angle;
        }
        /// <summary>
        /// Gets the normal at a branch position given a roll angle in radiands.
        /// </summary>
        /// <param name="position">Relative position (0-1) at a branch length.</param>
        /// <param name="rollAngle">Roll angle in radians.</param>
        /// <param name="branch">Branch instance.</param>
        /// <returns>Normal vector at a branch position and roll angle.</returns>
        public override Vector3 GetNormalAt (float position, float rollAngle, BroccoTree.Branch branch) {
            Vector3 normal = Vector3.forward;
            if (branch.curve != null) {
                if (rollAngle == 0f) {
                    return branch.curve.GetPointAt (position).normal;
                } else {
                    CurvePoint point = branch.curve.GetPointAt (position);
                    return Quaternion.AngleAxis (rollAngle * Mathf.Rad2Deg, point.forward) * point.normal;
                }
            }
            return normal;
        }
        /// <summary>
        /// Gets the distance from the center of the branch to its mesh surface given a relative position and roll angle.
        /// </summary>
        /// <param name="position">Relative position (0-1) at a branch length.</param>
        /// <param name="rollAngle">Roll angle in radians.</param>
        /// <param name="branch">Branch instance.</param>
        /// <returns>Distance from the center of the branch to is surface.</returns>
        public override float GetSurfaceDistanceAt (float position, float rollAngle, BroccoTree.Branch branch) {
            float radiusA = 1f;
            float radiusB = 1f;
            // Get the base girth of the branch at the given position.
            float baseGirth = branch.GetGirthAtPosition (position);
            // Get the shaper length position.
            float shaperLengthPos = branch.shaperOffset + position * branch.length;
            // If the length is outside the sections range, return default surface point.
            if (shaperLengthPos < 0f || shaperLengthPos > shaperLength) {
                return baseGirth;
            } else {
                // Get the section the point falls into.
                BranchShaper.Section section = GetSection (shaperLengthPos);
                if (section != null) {
                    // Normalize the shaperLengthPos to the local section relative position.
                    float sectionPos = Mathf.InverseLerp (section.fromLength, section.toLength, shaperLengthPos);
                    baseGirth *= section.GetScale (sectionPos, out radiusA, out radiusB);
                }
                //point = GetDefaultSurfacePoint (baseGirth, rollAngle);
                Vector3 point = GetEllipsePoint (rollAngle, baseGirth * radiusA, baseGirth * radiusB , 0f);
                return point.magnitude;
            }
        }
        /// <summary>
        /// Gets the surface branch point given a relative position and roll angle.
        /// </summary>
        /// <param name="position">Relative position (0-1) at a branch length.</param>
        /// <param name="rollAngle">Roll angle in radians.</param>
        /// <param name="branch">Branch instance.</param>
        /// <param name="applyTransforms">If <c>true</c> branch offset, direction and normal rotations are applied.</param>
        /// <returns>Surface point on a branch.</returns>
        public override Vector3 GetSurfacePointAt (float position, float rollAngle, BroccoTree.Branch branch, bool applyTransforms = true) {
            Vector3 point = Vector3.forward;
            if (branch.curve != null) {
                List<Vector3> surfPoints = new List<Vector3>();
                bool isCustomList = GetPoints (position, out surfPoints, branch);
                if (!isCustomList) {
                    // Get the base girth of the branch at the given position.
                    float baseGirth = branch.GetGirthAtPosition (position);
                    point = GetDefaultSurfacePoint (baseGirth, rollAngle);
                } else {
                    point = GetVectorAtRadialPosition (rollAngle, surfPoints);
                }
                if (applyTransforms) {
                    CurvePoint curvePoint = branch.curve.GetPointAt (position);
                    Quaternion rotation = Quaternion.LookRotation (
                        curvePoint.forward, 
                        curvePoint.bitangent);
                    point = (rotation * point) + curvePoint.position + branch.originOffset;
                }
            }
            return point;
        }
        /// <summary>
        /// Gets the surface branch point given a relative position and roll angle.
        /// </summary>
        /// <param name="position">Relative position (0-1) at a branch length.</param>
        /// <param name="rollAngle">Roll angle in radians.</param>
        /// <param name="branch">Branch instance.</param>
        /// <param name="applyTransforms">If <c>true</c> branch offset, direction and normal rotations are applied.</param>
        /// <returns>Surface point on a branch.</returns>
        public override Vector3 GetSurfaceNormalAt (float position, float rollAngle, BroccoTree.Branch branch) {
            Vector3 normal = Vector3.forward;
            if (branch.curve != null) {
                List<Vector3> surfNormals = new List<Vector3>();
                bool isCustomList = GetNormals (position, out surfNormals, branch);
                if (!isCustomList) {
                    normal = GetNormalAt (position, rollAngle, branch);
                } else {
                    normal = GetVectorAtRadialPosition (rollAngle, surfNormals);
                    CurvePoint curvePoint = branch.curve.GetPointAt (position);
                    Quaternion rotation = Quaternion.LookRotation (
                        curvePoint.forward, 
                        curvePoint.bitangent);
                    normal = rotation * normal;
                }
                /*
                if (applyTransforms) {
                    CurvePoint curvePoint = branch.curve.GetPointAt (position);
                    Quaternion rotation = Quaternion.LookRotation (
                        curvePoint.forward, 
                        curvePoint.bitangent);
                    normal = (rotation * normal) + curvePoint.position + branch.originOffset;
                }
                */
            }
            return normal;
        }
        public int GetNumberOfSegments (float position) {
            float nearestPos = GetNearestPosition (position);
			int dataIndex = segToIndex[nearestPos];
            if (dataIndex >= 0) {
                return points[dataIndex].Count;
            } else {
                return -1;
            }
        }
        public Section GetSection (float length) {
            if (sections.Count == 0) return null;
            Section section = sections [0];
            for (int i = 1; i < sections.Count; i++) {
                if (length < sections [i].fromLength) break;
                section = sections [i];
            }
            return section;
        }
        protected Vector3 GetDefaultSurfacePoint (float baseGirth, float rollAngle) {
            return new Vector3 (
                Mathf.Cos (rollAngle) * baseGirth,
                Mathf.Sin (rollAngle) * baseGirth,
                0f);
        }
        static public Vector3 GetEllipsePoint (float angle, float xRadius, float yRadius, float rotationAngle)
        {
            Vector3 point = Vector3.zero;
            point.x = Mathf.Cos (angle) * xRadius;
            point.y = Mathf.Sin (angle) * yRadius;
            float cos = Mathf.Cos (rotationAngle);
            float sin = Mathf.Sin (rotationAngle);
            float pointX = point.x * cos - point.y * sin;
            float pointY = point.x * sin + point.y * cos;
            point.x = pointX;
            point.y = pointY;
            return point;
        }
        protected float GetGirthScale (Section section, float position) {
            if (position < 0f) return section.bottomScale;
            if (position > 1f) return section.topScale;
            if (position < section.bottomCapPos) {
                // Bottom Cap.
                float capPos = Mathf.InverseLerp (0f, section.bottomCapPos, position);
                return Mathf.Lerp (section.bottomScale, section.bottomCapScale, capPos);
            } else if (position > section.topCapPos) {
                // Top Cap.
                float capPos = Mathf.InverseLerp (1f, section.topCapPos, position);
                return Mathf.Lerp (section.topScale, section.topCapScale, capPos);
            } else {
                // Middle Section.
                float midPos = Mathf.InverseLerp (section.bottomCapPos, section.topCapPos, position);
                return Mathf.Lerp (section.bottomCapScale, section.topCapScale, midPos);
            }
        }
        /// <summary>
        /// Get all the relevant positions along the BranchShaper instance, from 0 to 1.
        /// </summary>
        /// <param name="toleranceAngle">Angle tolerance to calculate the relevant positions.</param>
        /// <returns>List of relevant positions from 0 to 1 to the length of the shaper.</returns>
        public override List<float> GetRelevantPositions (float toleranceAngle = 5f) {
            //toleranceAngle = 180 ultra low poly, 32 ultra high poly
            List<float> positions = new List<float> ();
            float length = 0f;
            BranchShaper.Section section;
            if (sections.Count > 0) {
                length = sections [sections.Count - 1].toLength;
                for (int i = 0; i < sections.Count; i++) {
                    section = sections [i];
                    if (section.bottomCapPos > 0f) {
                        // Add inbetween bottom and bottomCap.
                        int capSteps = Mathf.RoundToInt (Mathf.Lerp (2f, 4f, Mathf.InverseLerp (180f, 32f, toleranceAngle)));
                        float capStep = (section.bottomCapPos * section.length) / (float)capSteps;
                        for (int j = 1; j <= capSteps; j++) {
                            positions.Add ((section.fromLength + (capStep * j)) / length);  
                        }
                    }
                    if (section.topCapPos < 1f) {
                        int capSteps = Mathf.RoundToInt (Mathf.Lerp (2f, 4f, Mathf.InverseLerp (180f, 32f, toleranceAngle)));
                        float capStep = ((1f - section.topCapPos) * section.length) / (float)capSteps;
                        for (int j = 0; j < capSteps; j++) {
                            positions.Add ((section.fromLength + section.topCapPos * section.length + (capStep * j)) / length);
                        }
                    }
                    if (i < sections.Count - 1)
                        positions.Add (sections [i].toLength / length);
                }
            }
            return positions;
        }
        public void AddSegment (float pos, List<float> radialPosition, List<Vector3> segPoints, List<Vector3> segNormals) {
            if (!segToIndex.ContainsKey (pos)) {
                int insertionIndex = segPos.BinarySearch (pos);
                if (insertionIndex < 0) {
                    insertionIndex = ~insertionIndex; // Bitwise complement for insertion point
                }
                segToIndex.Add (pos, points.Count);
                segPos.Insert (insertionIndex, pos);
                radialPositions.Add (radialPosition);
                points.Add (segPoints);
                normals.Add (segNormals);    
            }
        }
        public void AddDefaultSegment (float pos) {
            if (!segToIndex.ContainsKey (pos)) {
                int insertionIndex = segPos.BinarySearch (pos);
                if (insertionIndex < 0) {
                    insertionIndex = ~insertionIndex; // Bitwise complement for insertion point
                }
                segToIndex.Add (pos, -1);
                segPos.Insert (insertionIndex, pos);
            }
        }
        public bool HasSegment (float pos, bool includeDefaultSegments = false) {
            if (segToIndex.ContainsKey (pos)) {
                if (includeDefaultSegments) return true;
                else return segToIndex[pos] >= 0;
            }
            return false;
        }
        public List<float> GetRadialPos (float pos) {
            if (segToIndex.ContainsKey (pos)) {
                int index = segToIndex [pos];
                return radialPositions [index];
            }
            return new List<float> ();
        }
        public List<Vector3> GetPoints (float pos) {
            if (segToIndex.ContainsKey (pos)) {
                int index = segToIndex [pos];
                return points[index];
            }
            return new List<Vector3> ();
        }
        public List<Vector3> GetNormals (float pos) {
            if (segToIndex.ContainsKey (pos)) {
                int index = segToIndex [pos];
                return normals[index];
            }
            return new List<Vector3> ();
        }
        #endregion

        #region Exposed Properties
        public override bool bottomScaleExposed { get { return true; } }
        public override bool bottomCapScaleExposed { get { return true; } }
        public override bool topScaleExposed { get { return true; } }
        public override bool topCapScaleExposed { get { return true; } }
        public override bool bottomParam1Exposed { get { return true; } }
        public override bool bottomCapParam1Exposed { get { return true; } }
        public override bool topParam1Exposed { get { return true; } }
        public override bool topCapParam1Exposed { get { return true; } }
        public override bool bottomParam2Exposed { get { return true; } }
        public override bool bottomCapParam2Exposed { get { return true; } }
        public override bool topParam2Exposed { get { return true; } }
        public override bool topCapParam2Exposed { get { return true; } }
        public override bool bottomCapFnExposed { get { return true; } }
        public override bool topCapFnExposed { get { return true; } }
        public override string bottomScaleName { get { return "bottomScale"; } }
        public override string bottomCapScaleName { get { return "bottomCapScale"; } }
        public override string topScaleName { get { return "topScale"; } }
        public override string topCapScaleName { get { return "topCapScale"; } }
        public override string bottomParam1Name { get { return "bottomParam1"; } }
        public override string bottomCapParam1Name { get { return "bottomCapParam1"; } }
        public override string topParam1Name { get { return "topParam1"; } }
        public override string topCapParam1Name { get { return "topCapParam1"; } }
        public override string bottomParam2Name { get { return "bottomParam2"; } }
        public override string bottomCapParam2Name { get { return "bottomCapParam2"; } }
        public override string topParam2Name { get { return "topParam2"; } }
        public override string topCapParam2Name { get { return "topCapParam2"; } }
        public override string bottomCapFnName { get { return "bottomCapFn"; } }
        public override string topCapFnName { get { return "topCapFn"; } }
        #endregion
    }
}