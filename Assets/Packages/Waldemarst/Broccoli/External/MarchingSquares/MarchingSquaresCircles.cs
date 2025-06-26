using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace Broccoli.Utils
{
    /// <summary>
    /// Uses marching-squares to extract silhouette for circle shapes.
    /// Usage:
    /// 1. Set the squares grid: gridCells
    /// 2. Add the circular shapes: AddCircle(), AddCircles() (processing optional)
    /// 3. Set the smooth path iterations: applySmoothPath, contourSmoothIterations
    /// 4. Set cardinal offset: cardinal, cardinalCenter
    /// 5. Process: MarchingSquares
    /// 6. Access paths information: paths, openDistances, closedDistances, GetPoint()
    /// 7. Access grid information: Cells, cellWidth, cellHeight, Points, bounds, rows, columns
    /// </summary>
	public class MarchingSquaresCircles {
        #region Job
		/// <summary>
		/// Job structure to process branch skins.
		/// </summary>
		struct MarchingSquaresJobImpl : IJobParallelFor {
			#region Params
            public Bounds bounds;
            public int cols;
            public float cellWidth;
            public float cellHeight;
			#endregion

			#region Input
            public NativeArray<int> points;
            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> circles;
			#endregion

			#region Job Methods
			/// <summary>
			/// Executes one per sprout.
			/// </summary>
			/// <param name="i"></param>
			public void Execute (int i) {
                Vector2 tmpPoint = new Vector2 (
                    bounds.min.x + i % cols * cellWidth, 
                    bounds.min.y + Mathf.FloorToInt (i / cols) * cellHeight);
                for (int k = 0; k < circles.Length; k++) {
                    float distanceSquared = (tmpPoint - (Vector2)circles[k]).sqrMagnitude;
                    if (distanceSquared <= circles[k].z * circles[k].z) {
                        points [i] = 1;
                        break;
                    } else {
                        points [i] = 0;
                    }
                }
			}
			#endregion
		}
		#endregion

        #region Constants
        public static readonly int DIR_RIGHT_UP = 0;
        public static readonly int DIR_LEFT_DOWN = 1;
        public static readonly int DIR_RIGHT = 0;
        public static readonly int DIR_DOWN = 1;
        public static readonly int DIR_LEFT = 2;
        public static readonly int DIR_UP = 3;
        #endregion

        #region Circles & Bounds Vars
        /// <summary>
        /// Circles hold on this instance.
        /// </summary>
        protected List<Vector3> _circles = new();
        /// <summary>
        /// Circles hold by this instance. Each circle is represented by a Vector3 where
        /// x and y are the center coordinates and the radius is represented by the z value.
        /// </summary>
        public List<Vector3> Circles { get { return _circles; } }
        /// <summary>
        /// Bounds containing the circles of this instance.
        /// </summary>
        protected Bounds _bounds = new();
        /// <summary>
        /// Bounds containing the circles of this instance.
        /// </summary>
        public Bounds Bounds { get { return _bounds; } }
        /// <summary>
        /// Grid width.
        /// </summary>
        public float Width { get { return _bounds.size.x; }}
        /// <summary>
        /// Grid height.
        /// </summary>
        public float Height { get { return _bounds.size.y;}}
        #endregion

		#region Marching Squares Vars
        /// <summary>
        /// How many squares the grid will have on its shortest side.
        /// </summary>
        public int gridCells = 25;
        /// <summary>
        /// Final number of rows on the grid after all subdivisions have been applied.
        /// </summary>
        protected int _rows = 0;
        /// <summary>
        /// Final number of rows on the grid after all subdivisions have been applied.
        /// </summary>
        public int Rows { get { return _rows;}}
        /// <summary>
        /// Final number of columns on the grid after all subdivisions have been applied.
        /// </summary>
        protected int _cols = 0;
        /// <summary>
        /// Final number of columns on the grid after all subdivisions have been applied.
        /// </summary>
        public int Cols { get { return _cols;}}
        /// <summary>
        /// The grid points containing the marching square values (-1 if the point has not been evaluated). 
        /// </summary>
        protected int[] _points;
        /// <summary>
        /// The grid points containing the marching square values. 
        /// </summary>
        public int[] Points { get { return _points; } } 
        /// <summary>
        /// Relative grid cell width.
        /// </summary>
        protected float _cellWidth = 0f;
        /// <summary>
        /// Relative grid cell height.
        /// </summary>
        protected float _cellHeight = 0f;
        /// <summary>
        /// Cell width on the grid.
        /// </summary>
        public float CellWidth { get { return _cellWidth;}}
        /// <summary>
        /// Cell height on the grid.
        /// </summary>
        public float CellHeight { get { return _cellHeight;}}
        /// <summary>
        /// The grid cells containing the marching square values. 
        /// </summary>
        protected int[] _cells;
        /// <summary>
        /// The grid cells containing the marching square values. 
        /// </summary>
        public int[] Cells { get { return _cells; } }
        /// <summary>
        /// List of path points found using marching squares.
        /// </summary>
        public List<List<Vector2>> paths = new List<List<Vector2>> ();
        /// <summary>
        /// List of normals for the paths.
        /// </summary>
        public List<List<Vector2>> normals = new List<List<Vector2>> ();
        /// <summary>
        /// List of path distances found using marching squares.
        /// </summary>
        public List<List<float>> pointDistances = new List<List<float>> ();
        /// <summary>
        /// Distance for each point on the open path.
        /// </summary>
        public List<float> openDistances = new List<float> ();
        /// <summary>
        /// Distance for each point on the closed path.
        /// </summary>
        public List<float> closedDistances = new List<float> ();
        /// <summary>
        /// Apply smoothing to the extracted paths.
        /// </summary>
        public bool applySmoothPath = true;
        /// <summary>
        /// Number of iterations to path smooth.
        /// </summary>
        public int smoothPathIterations = 5;
        public enum Cardinal {
            None,
            South,
            East,
            North,
            West
        }
        public Cardinal cardinal = Cardinal.None;
        public Vector2 cardinalCenter = Vector2.zero;
        public enum NormalType {
            None = 0,
            Center = 1,
            Curve = 2
        }
        public NormalType normalType = NormalType.None;
        #endregion

        #region Temp Vars
        Vector2 _tmpPoint = Vector2.zero;
        #endregion

		#region Contructor
        /// <summary>
        /// Class constructor.
        /// </summary>
		public MarchingSquaresCircles () {}
        #endregion

        #region Cicles Methods
        /// <summary>
        /// Clear all the circles on this instance and optionally process marching cubes.
        /// </summary>
        /// <param name="processing">True if after clearing the circules the instance should process marching squares.</param>
        public void ClearCircles (bool processing = true) {
            _circles.Clear ();
            _bounds = new();
            if (processing)
                MarchingSquares ();
        }
        /// <summary>
        /// Adds a circle to this instance and optionally process marching cubes.
        /// </summary>
        /// <param name="center">Center for the circle.</param>
        /// <param name="radius">Radius for the circle.</param>
        /// <param name="processing">True if after adding the circle the instance should process marching squares.</param>
        public void AddCircle (Vector2 center, float radius, bool processing = true) {
            AddCircle (new Vector3 (center.x, center.y, radius), processing);
        }
        /// <summary>
        /// Adds a circle to this instance and optionally process marching cubes.
        /// </summary>
        /// <param name="processing">True if after adding the circle the instance should process marching squares.</param>
        public void AddCircle (Vector3 circle, bool processing = true) {
            if (circle.z < 0) circle.z = -circle.z;
            _circles.Add (circle);
            CalculateBounds ();
            if (processing)
                MarchingSquares ();
        }
        /// <summary>
        /// Adds a list of circles to this instance and optionally process marching cubes.
        /// </summary>
        /// <param name="processing">True if after adding the circles the instance should process marching squares.</param>
        public void AddCircles (List<Vector3> circles, bool processing = true) {
            for (int i = 0; i < circles.Count; i++) {
                AddCircle (circles[i], false);
                _bounds.Encapsulate (new Vector3 (Mathf.CeilToInt (circles[i].x + circles[i].z), Mathf.CeilToInt (circles[i].y + circles[i].z)));
                _bounds.Encapsulate (new Vector3 (Mathf.FloorToInt (circles[i].x - circles[i].z), Mathf.FloorToInt (circles[i].y - circles[i].z)));
            }
            if (processing)
                MarchingSquares ();
        }
        /// <summary>
        /// Modifies a circle hold by this instance.
        /// </summary>
        /// <param name="index">Index for the circle.</param>
        /// <param name="center">Center for the circle.</param>
        /// <param name="radius">Radius for the circle.</param>
        /// <param name="processing">True if after editing the circle the instance should process marching squares.</param>
        /// <returns></returns>
        public bool ModifyCircle (int index, Vector2 center, float radius, bool processing = true) {
            return ModifyCircle (index, new Vector3 (center.x, center.y, radius), processing);
        }
        /// <summary>
        /// Modifies a circle hold by this instance.
        /// </summary>
        /// <param name="index">Index of the circle.</param>
        /// <param name="circle">Circle values (x,y coordinates, z radius).</param>
        /// <param name="processing">True to process marching squares after modifying the circle.</param>
        /// <returns>True if the circle has been modified.</returns>
        public bool ModifyCircle (int index, Vector3 circle, bool processing = true) {
            if (index >= 0 && index < _circles.Count) {
                if (circle.z < 0) circle.z = -circle.z;
                _circles [index] = circle;
                CalculateBounds ();
                if (processing)
                    MarchingSquares ();
            }
            return false;
        }
        /// <summary>
        /// Removes a circle from this instance.
        /// </summary>
        /// <param name="index">Index of the circle.</param>
        /// <param name="processing">True to process marching squares after removing the circle.</param>
        /// <returns>True if the circle has been removed.</returns>
        public bool RemoveCircle (int index, bool processing = true) {
            if (index >= 0 && index < _circles.Count) {
                _circles.RemoveAt (index);
                if (processing)
                    MarchingSquares ();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Calculates the bounds of the circles.
        /// </summary>
        public void CalculateBounds () {
            _bounds = new Bounds ();
            float minX ,minY, maxX, maxY;
            float boundRadiusScaleFactor = 1.2f;
            if (_circles.Count > 0) {
                minX = _circles [0].x - _circles [0].z;
                minY = _circles [0].y - _circles [0].z;
                maxX = _circles [0].x + _circles [0].z;
                maxY = _circles [0].y + _circles [0].z;
                for (int i = 1; i < _circles.Count; i++) {
                    if (_circles [i].x - _circles [i].z < minX) minX = _circles [i].x - _circles [i].z * boundRadiusScaleFactor;
                    if (_circles [i].y - _circles [i].z < minY) minY = _circles [i].y - _circles [i].z * boundRadiusScaleFactor;
                    if (_circles [i].x + _circles [i].z > maxX) maxX = _circles [i].x + _circles [i].z * boundRadiusScaleFactor;
                    if (_circles [i].y + _circles [i].z > maxY) maxY = _circles [i].y + _circles [i].z * boundRadiusScaleFactor;
                }
                _bounds.center = new Vector3 (minX + ((maxX - minX) / 2f), minY + ((maxY - minY) / 2f));
                _bounds.Encapsulate (new Vector3 (maxX, maxY));
                _bounds.Encapsulate (new Vector3 (minX, minY));
            }
        }
        #endregion

        #region Marching Squares Processing
        /// <summary>
        /// Process marching squares with the circules on this instance.
        /// </summary>
        public void MarchingSquares () {
            _cols = 0;
            _rows = 0;
            if (_bounds != null && _bounds.size.magnitude > 0 && gridCells > 0) {
                // Create Grid Points and Cells
                if (_bounds.size.x < _bounds.size.y) {
                    _cols = gridCells;
                    _rows = Mathf.RoundToInt (_bounds.size.y * gridCells / _bounds.size.x);
                } else {
                    _rows = gridCells;
                    _cols = Mathf.RoundToInt (_bounds.size.x * gridCells / _bounds.size.y);
                }
                _cellWidth = _bounds.size.x / (float)_cols;
                _cellHeight = _bounds.size.y / (float)_rows;
                _points = new int[(_cols + 1) * (_rows + 1)];
                _cells = new int[_cols * _rows];
                for (int i = 0; i <= _rows; i++) {
                    for (int j = 0; j <= _cols; j++) {
                        _points [i * _cols + j] = 0;
                    }
                }
                for (int i = 0; i < _cols; i++) {
                    for (int j = 0; j < _rows; j++) {
                        _cells[_rows * i + j] = 0;
                    }
                }

                /*
                LINEAR PROCESSING
                // Evaluate Points inside Circles.
                for (int i = 0; i <= _rows; i++) {
                    for (int j = 0; j <= _cols; j++) {
                        if (_points [i * _cols + j] == 0) {
                            // Get point coordinates.
                            _tmpPoint.x = _bounds.min.x + j * _cellWidth;
                            _tmpPoint.y = _bounds.min.y + i * _cellHeight;
                            // Check if the point its contained by a circle.
                            for (int k = 0; k < _circles.Count; k++) {
                                float distanceSquared = (_tmpPoint - (Vector2)_circles[k]).sqrMagnitude;
                                if (distanceSquared <= _circles[k].z * _circles[k].z) {
                                    _points [i * _cols + j] = 1;
                                    break;
                                } else {
                                    _points [i * _cols + j] = 0;
                                }
                            }
                        }
                    }
                }
                */

                MarchingSquaresJobImpl _msJob = new MarchingSquaresJobImpl() {
                    bounds = _bounds,
                    cols = _cols,
                    cellWidth = _cellWidth,
                    cellHeight = _cellHeight,
                    points = new NativeArray<int> (_points, Allocator.TempJob),
                    circles = new NativeArray<Vector3> (_circles.ToArray (), Allocator.TempJob)
                };

                JobHandle _msJobHandle = _msJob.Schedule (_points.Length, 32);
                _msJobHandle.Complete ();

                _points = _msJob.points.ToArray ();

                _msJob.points.Dispose ();
                _msJob.circles.Dispose ();
                
                // Set Cell values.
                int cellValue;
                for (int i = 0; i < _rows; i++) {
                    for (int j = 0; j < _cols; j++) {
                        cellValue = 0;
                        cellValue |= _points [i * _cols + j]==1?1:0;
                        cellValue |= _points [i * _cols + j + 1]==1?2:0;
                        cellValue |= _points [(i + 1) * _cols + j + 1]==1?4:0;
                        cellValue |= _points [(i + 1) * _cols + j]==1?8:0;
                        _cells [i * _cols + j] = cellValue;
                    }
                }

                ClearPaths ();
                ExtractPaths ();
                AnalyzePaths ();
            }
        }
        private void ClearPaths () {
            paths = new List<List<Vector2>> ();
            normals = new List<List<Vector2>> ();
            openDistances = new List<float> ();
            closedDistances = new List<float> ();
            pointDistances = new List<List<float>> ();
        }
        private void ExtractPaths () {
            for (int i = 0; i < _rows; i++) {
                for (int j = 0; j < _cols; j++) {
                    if (_cells [i * _cols + j] > 0 && 
                        _cells [i * _cols + j] < 15 &&
                        _cells [i * _cols + j] != 5 &&
                        _cells [i * _cols + j] != 10) 
                    {
                        ExtractPath (i, j);
                    } else if (_cells [i * _cols + j] < 1000) {
                        _cells [i * _cols + j] += 1000;
                    }
                }
            }

            // Restore cell values
            int cellVal;
            for (int i = 0; i < _rows; i++) {
                for (int j = 0; j < _cols; j++) {
                    cellVal = _cells [i * _cols + j];
                    if (cellVal >= 1000) {
                        _cells [i * _cols + j] -= 1000;
                    }
                }
            }
        }
        private void ExtractPath (int row, int col) {
            int dir = DIR_RIGHT;
            List<Vector2> path = new List<Vector2>();
            int currRow = row;
            int currCol = col;
            int cellVal;
            do {
                // 2.1 Get start point, add to path.
                if (currCol >= _cols || currRow >= _rows) {
                    Debug.Log ("Index out of bounds");
                }
                path.Add (GetCellStartPoint (currRow, currCol, dir));
                // 2.2 Get next cell direction.
                dir = GetNextCellDirection (currRow, currCol, dir);
                // 2.3 Add 1000 to cellVal.
                cellVal = _cells [currRow * _cols + currCol];
                if (cellVal != 5 && cellVal != 10)
                    _cells [currRow * _cols + currCol] += 1000;
                // 2.4 Get next currRow and currCol
                GetNextRowCol (currRow, currCol, out currRow, out currCol, dir);
            } while (row != currRow || col != currCol);
            if (applySmoothPath)
                path = SmoothPath (path, smoothPathIterations);
            if (cardinal != Cardinal.None)
                CardinalShift (ref path, _bounds, cardinal);
            paths.Add (path);
            if (normalType != NormalType.None) {
                List<Vector2> _normals = ProcessNormals (ref path, normalType, cardinalCenter);
                normals.Add (_normals);
            }
        }
        private void GetNextRowCol (int inputRow, int inputCol, out int row, out int col, int direction) {
            row = inputRow;
            col = inputCol;
            if (direction == DIR_RIGHT) { col = inputCol + 1; }
            else if (direction == DIR_UP) { row = inputRow + 1; } 
            else if (direction == DIR_LEFT) { col = inputCol - 1; if (col < 0) col = 0; }
            else { row = inputRow - 1; if (row < 0) row = 0; }
        }
        private Vector2 GetCellStartPoint (int row, int col, int direction) {
            int index = row * _cols + col;
            if (index < 0 || index >= _cells.Length) {
                Debug.Log ("Invalid index");
            }
            int cellVal = _cells [row * _cols + col];
            switch (cellVal) {
                case 1:
                    if (direction == DIR_RIGHT) { return GetCellPointLeft (row, col); }
                    else  { return GetCellPointBottom (row, col); }
                case 2:
                    if (direction == DIR_UP) { return GetCellPointBottom (row, col); }
                    else  { return GetCellPointRight (row, col); }
                case 3:
                    if (direction == DIR_RIGHT) { return GetCellPointLeft (row, col); }
                    else  { return GetCellPointRight (row, col); }
                case 4:
                    if (direction == DIR_DOWN || direction == DIR_RIGHT) { return GetCellPointTop (row, col); }
                    else  { return GetCellPointRight (row, col); }
                case 5:
                    if (direction == DIR_RIGHT) { return GetCellPointLeft (row, col); }
                    else if (direction == DIR_DOWN) { return GetCellPointTop (row, col); }
                    else if (direction == DIR_LEFT) { return GetCellPointRight (row, col); }
                    else { return GetCellPointBottom (row, col); }
                case 6:
                    if (direction == DIR_UP) { return GetCellPointBottom (row, col); }
                    else  { return GetCellPointTop (row, col); }
                case 7:
                    if (direction == DIR_RIGHT) { return GetCellPointLeft (row, col); }
                    else  { return GetCellPointTop (row, col); }
                case 8:
                    if (direction == DIR_RIGHT) { return GetCellPointLeft (row, col); }
                    else  { return GetCellPointTop (row, col); }
                case 9:
                    if (direction == DIR_UP) { return GetCellPointBottom (row, col); }
                    else  { return GetCellPointTop (row, col); }
                case 10:
                    if (direction == DIR_RIGHT) { return GetCellPointLeft (row, col); }
                    else if (direction == DIR_DOWN) { return GetCellPointTop (row, col); }
                    else if (direction == DIR_LEFT) { return GetCellPointRight (row, col); }
                    else { return GetCellPointBottom (row, col); }
                case 11:
                    if (direction == DIR_RIGHT) { return GetCellPointTop (row, col); }
                    else  { return GetCellPointRight (row, col); }
                case 12:
                    if (direction == DIR_RIGHT) { return GetCellPointLeft (row, col); }
                    else  { return GetCellPointRight (row, col); }
                case 13:
                    if (direction == DIR_UP) { return GetCellPointBottom (row, col); }
                    else  { return GetCellPointRight (row, col); }
                case 14:
                    if (direction == DIR_RIGHT) { return GetCellPointLeft (row, col); }
                    else  { return GetCellPointBottom (row, col); }
            }
            return Vector2.zero;
        }
        private int GetNextCellDirection (int row, int col, int prevDirection) {
            int celVal = _cells [row * _cols + col];
            switch (celVal) {
                case 1:
                    if (prevDirection == DIR_RIGHT) { return DIR_DOWN; }
                    else  { return DIR_LEFT; }
                case 2:
                    if (prevDirection == DIR_UP) { return DIR_RIGHT; }
                    else  { return DIR_DOWN; }
                case 3:
                    if (prevDirection == DIR_RIGHT) { return DIR_RIGHT; }
                    else  { return DIR_LEFT; }
                case 4:
                    if (prevDirection == DIR_LEFT) { return DIR_UP; }
                    else  { return DIR_RIGHT; }
                case 5:
                    if (prevDirection == DIR_RIGHT) { return DIR_UP; }
                    else if (prevDirection == DIR_UP)  { return DIR_RIGHT; }
                    else if (prevDirection == DIR_LEFT)  { return DIR_DOWN; }
                    else { return DIR_LEFT; }
                case 6:
                    if (prevDirection == DIR_DOWN) { return DIR_DOWN; }
                    else  { return DIR_UP; }
                case 7:
                    if (prevDirection == DIR_RIGHT) { return DIR_UP; }
                    else  { return DIR_LEFT; }
                case 8:
                    if (prevDirection == DIR_RIGHT) { return DIR_UP; }
                    else  { return DIR_LEFT; }
                case 9:
                    if (prevDirection == DIR_DOWN) { return DIR_DOWN; }
                    else  { return DIR_UP; }
                case 10:
                    if (prevDirection == DIR_RIGHT) { return DIR_DOWN; }
                    else if (prevDirection == DIR_UP)  { return DIR_LEFT; }
                    else if (prevDirection == DIR_LEFT)  { return DIR_UP; }
                    else { return DIR_RIGHT; }
                case 11:
                    if (prevDirection == DIR_LEFT) { return DIR_UP; }
                    else  { return DIR_RIGHT; }
                case 12:
                    if (prevDirection == DIR_RIGHT) { return DIR_RIGHT; }
                    else  { return DIR_LEFT; }
                case 13:
                    if (prevDirection == DIR_UP) { return DIR_RIGHT; }
                    else  { return DIR_DOWN; }
                case 14:
                    if (prevDirection == DIR_RIGHT) { return DIR_DOWN; }
                    else  { return DIR_LEFT; }
            }
            return 0;
        }
        public void GetCellPoints (
            int row, 
            int col, 
            out Vector2 pointA, 
            out Vector2 pointB,
            int direction) 
        {
            pointA = Vector2.zero;
            pointB = Vector2.zero;
            int celVal = _cells [row * _cols + col];
            switch (celVal) {
                case 1:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointLeft (row, col); pointB = GetCellPointBottom (row, col); }
                    else  { pointB = GetCellPointLeft (row, col); pointA = GetCellPointBottom (row, col); }
                    break;
                case 2:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointBottom (row, col); pointB = GetCellPointRight (row, col); }
                    else  { pointB = GetCellPointBottom (row, col); pointA = GetCellPointRight (row, col); }
                    break;
                case 3:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointLeft (row, col); pointB = GetCellPointRight (row, col); }
                    else  { pointB = GetCellPointLeft (row, col); pointA = GetCellPointRight (row, col); }
                    break;
                case 4:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointTop (row, col); pointB = GetCellPointRight (row, col); }
                    else  { pointB = GetCellPointTop (row, col); pointA = GetCellPointRight (row, col); }
                    break;
                case 5:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointLeft (row, col); pointB = GetCellPointTop (row, col); }
                    else  { pointA = GetCellPointRight (row, col); pointB = GetCellPointBottom (row, col); }
                    break;
                case 6:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointBottom (row, col); pointB = GetCellPointTop (row, col); }
                    else  { pointB = GetCellPointBottom (row, col); pointA = GetCellPointTop (row, col); }
                    break;
                case 7:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointLeft (row, col); pointB = GetCellPointTop (row, col); }
                    else  { pointB = GetCellPointLeft (row, col); pointA = GetCellPointTop (row, col); }
                    break;
                case 8:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointLeft (row, col); pointB = GetCellPointTop (row, col); }
                    else  { pointB = GetCellPointLeft (row, col); pointA = GetCellPointTop (row, col); }
                    break;
                case 9:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointBottom (row, col); pointB = GetCellPointTop (row, col); }
                    else  { pointB = GetCellPointBottom (row, col); pointA = GetCellPointTop (row, col); }
                    break;
                case 10:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointLeft (row, col); pointB = GetCellPointBottom (row, col); }
                    else  { pointA = GetCellPointRight (row, col); pointB = GetCellPointTop (row, col); }
                    break;
                case 11:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointTop (row, col); pointB = GetCellPointRight (row, col); }
                    else  { pointB = GetCellPointTop (row, col); pointA = GetCellPointRight (row, col); }
                    break;
                case 12:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointLeft (row, col); pointB = GetCellPointRight (row, col); }
                    else  { pointB = GetCellPointLeft (row, col); pointA = GetCellPointRight (row, col); }
                    break;
                case 13:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointBottom (row, col); pointB = GetCellPointRight (row, col); }
                    else  { pointB = GetCellPointBottom (row, col); pointA = GetCellPointRight (row, col); }
                    break;
                case 14:
                    if (direction == DIR_RIGHT_UP) { pointA = GetCellPointLeft (row, col); pointB = GetCellPointBottom (row, col); }
                    else  { pointB = GetCellPointLeft (row, col); pointA = GetCellPointBottom (row, col); }
                    break;
            }
        }
        public Vector2 GetCellPointLeft (int row, int col) {
            return new Vector2 (
                _bounds.min.x + _cellWidth * col,
                _bounds.min.y + _cellHeight * row + _cellHeight / 2f);
        }
        public Vector2 GetCellPointBottom (int row, int col) {
            return new Vector2 (
                _bounds.min.x + _cellWidth * col + _cellWidth / 2f,
                _bounds.min.y + _cellHeight * row);
        }
        public Vector2 GetCellPointRight (int row, int col) {
            return new Vector2 (
                _bounds.min.x + _cellWidth * col + _cellWidth,
                _bounds.min.y + _cellHeight * row + _cellHeight / 2f);
        }
        public Vector2 GetCellPointTop (int row, int col) {
            return new Vector2 (
                _bounds.min.x + _cellWidth * col + _cellWidth / 2f,
                _bounds.min.y + _cellHeight * row + _cellHeight);
        }
        public static Vector2 Lerp(Vector2 p1, Vector2 p2, float t)
        {
            //return p1 + t * (p2 - p1);
            return Vector2.Lerp (p1, p2, t);
        }

        public static List<Vector2> InterpolatePointsSmoothly(List<Vector2> points, int numInterpolations = 10)
        {
            if (points == null || points.Count < 2)
            {
                throw new System.ArgumentException("Invalid input: points list must have at least two points.");
            }

            List<Vector2> interpolatedPoints = new List<Vector2>();
            interpolatedPoints.Add(points[0]); // Add the first point

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector2 p0 = points[i - 1]; // Previous point
                Vector2 p1 = points[i];     // Current point
                Vector2 p2 = points[i + 1]; // Next point

                // Interpolate points between p0 and p2, using p1 as a guide
                for (int j = 1; j <= numInterpolations; j++)
                {
                    float t = j / (float)(numInterpolations + 1);
                    Vector2 midPoint = Lerp(p1, p2, t);
                    Vector2 interpolatedPoint = Lerp(p0, midPoint, t);
                    interpolatedPoints.Add(interpolatedPoint);
                }

                interpolatedPoints.Add(points[i]); // Add the current point
            }

            interpolatedPoints.Add(points[points.Count - 1]); // Add the last point
            return interpolatedPoints;
        }
        public static List<Vector2> SmoothPath (List<Vector2> path, int numIterations = 5)
        {
            if (path == null || path.Count < 3)
            {
                throw new System.ArgumentException("Invalid input: path must have at least 3 points.");
            }

            List<Vector2> smoothedPath = new List<Vector2> (path); // Make a copy

            for (int iteration = 0; iteration < numIterations; iteration++)
            {
                for (int i = 0; i < smoothedPath.Count; i++)
                {
                    Vector2 prevPoint = smoothedPath[(i - 1 + smoothedPath.Count) % smoothedPath.Count];
                    Vector2 currentPoint = smoothedPath[i];
                    Vector2 nextPoint = smoothedPath[(i + 1) % smoothedPath.Count];

                    Vector2 smoothedPoint = (prevPoint + currentPoint + nextPoint) / 3f;
                    smoothedPath[i] = smoothedPoint;
                }
            }

            return smoothedPath;
        }
        public static List<Vector2> ProcessNormals (ref List<Vector2> path, NormalType normalType, Vector2 cardinalCenter)
        {
            List<Vector2> normals = new List<Vector2>();
            if (normalType == NormalType.Center) {
                foreach (Vector2 point in path) {
                    normals.Add ((point - cardinalCenter).normalized);
                }
            } else if (normalType == NormalType.Curve) {
                int pointsCount = path.Count;
                float avgAngle;
                // Normal for first point.
                avgAngle = Mathf.Atan2 (path[0].y - path[path.Count - 1].y, path[0].x - path[path.Count - 1].x);
                avgAngle += -(Mathf.PI / 2f);
                normals.Add (new Vector2 (Mathf.Cos (avgAngle), Mathf.Sin (avgAngle)));
                // Normal for middle points.
                for (int i = 1; i < pointsCount - 1; i++) {
                    avgAngle = Mathf.Atan2 (path[i + 1].y - path[i - 1].y, path[i + 1].x - path[i - 1].x);
                    avgAngle += -(Mathf.PI / 2f);
                    normals.Add (new Vector2 (Mathf.Cos (avgAngle), Mathf.Sin (avgAngle)));
                }
                // Normal for last point.
                int lastPos = path.Count - 1;
                avgAngle = Mathf.Atan2 (path[0].y - path[lastPos - 1].y, path[0].x - path[lastPos - 1].x);
                avgAngle += -(Mathf.PI / 2f);
                normals.Add (new Vector2 (Mathf.Cos (avgAngle), Mathf.Sin (avgAngle)));
            }
            return normals;
        }
        /// <summary>
        /// Extracts data from a path:
        /// Total distance for the path, absolute and relative position of each points.
        /// </summary>
        public void AnalyzePaths () {
            List<Vector2> _path;
            float openDistance;
            float closedDistance;
            pointDistances.Clear ();
            List<float> _pointDistances = new List<float>();
            // Iterate through all the paths.
            for (int pathI = 0; pathI < paths.Count; pathI++) {
                // Get open and closed distances, set distance.
                openDistance = 0f;
                closedDistance = 0f;
                _path = paths[pathI];
                _pointDistances.Clear ();
                _pointDistances.Add (0f);
                for (int i = 1; i < _path.Count; i++) {
                    openDistance += Vector2.Distance (_path[i - 1], _path[i]);
                    _pointDistances.Add (openDistance);
                }
                closedDistance = openDistance + Vector2.Distance (_path[0], _path[_path.Count - 1]);
                openDistances.Add (openDistance);
                closedDistances.Add (closedDistance);
                pointDistances.Add (new List<float>(_pointDistances));
            }
        }
        #endregion

        #region Traversing
        /// <summary>
        /// Gets a point on a path given the path's index and the absolute position (0-1) for the point along the path.
        /// </summary>
        /// <param name="pathIndex">The index of the path to retrieve the point from.</param>
        /// <param name="absolutePosition">The absolute position along the path, where 0 is the start and 1 is the end. Values outside this range will wrap if the path is closed.</param>
        /// <param name="isClosed">Indicates whether the path is closed (looping) or open.</param>
        /// <param name="point">When this method returns, contains the calculated point on the path. Set to Vector2.zero if the path index is invalid.</param>
        /// <param name="normal">When this method returns, contains the normal vector at the calculated point. Set to Vector2.zero if the path index is invalid or if normals are not defined for the path.</param>
        /// <param name="distance">When this method returns, contains the distance along the path corresponding to the calculated point. Set to 0 if the path index is invalid.</param>
        /// <returns>True if the point was successfully retrieved; otherwise, false (e.g., if the path index is out of range).</returns>
        public bool GetPoint (
            int pathIndex, 
            float absolutePosition, 
            bool isClosed, 
            out Vector2 point,
            out Vector2 normal, 
            out float distance)
        {
            point = Vector2.zero;
            normal = Vector2.zero;
            distance = 0f;
            if (pathIndex < paths.Count) {
                float targetDistance;
                int index;
                if (isClosed) {
                    targetDistance = (absolutePosition % 1) * closedDistances[pathIndex];
                } else {
                    targetDistance = (absolutePosition % 1) * openDistances[pathIndex];
                }
                distance = targetDistance;
                index = pointDistances[pathIndex].BinarySearch (targetDistance);
                // Exact match index found.
                if (index >= 0) {
                    point = paths[pathIndex][index];
                    if (normals.Count > 0 && normals[pathIndex].Count > 0) {
                        normal = normals[pathIndex][index];
                    }
                } else {
                    index = -index;
                    index--;
                    // Position is beyond the last point.
                    if (index >= pointDistances[pathIndex].Count) {
                        index--;
                        if (isClosed) { 
                            // Lerp between last and first point.
                            float pointPos = Mathf.InverseLerp (pointDistances[pathIndex][index], closedDistances[pathIndex], targetDistance);
                            point = Vector2.Lerp (paths[pathIndex][index], paths[pathIndex][0], pointPos);
                            if (normals.Count > 0 && normals[pathIndex].Count > 0) {
                                normal = Vector2.Lerp (normals[pathIndex][index], normals[pathIndex][0], pointPos);
                            }
                        } else {
                            point = paths[pathIndex][index];
                            if (normals.Count > 0 && normals[pathIndex].Count > 0) {
                                normal = normals[pathIndex][index];
                            }
                        }
                    }
                    // Position is within the array, index is the top value index.
                    else {
                        float pointPos = Mathf.InverseLerp (pointDistances[pathIndex][index - 1], pointDistances[pathIndex][index], targetDistance);
                        point = Vector2.Lerp (paths[pathIndex][index - 1], paths[pathIndex][index], pointPos);
                        if (normals.Count > 0 && normals[pathIndex].Count > 0) {
                            normal = Vector2.Lerp (normals[pathIndex][index - 1], normals[pathIndex][index], pointPos);
                        }
                    }
                }
                return true;
            }
            return false;
        }
        private void CardinalShift (ref List<Vector2> list, Bounds bounds, Cardinal cardinal) {
            int firstIndex = 0, secondIndex = 0;
            float firstVal, secondVal;
            if (cardinal == Cardinal.South) {
                firstVal = bounds.min.x;
                secondVal = bounds.max.x;
                // Find first and second indexes.
                for (int i = 0; i < list.Count; i++) {
                    if (list[i].y <= cardinalCenter.y) {
                        if (list[i].x < cardinalCenter.x && list[i].x > firstVal) {
                            firstIndex = i;
                            firstVal = list[i].x;
                        }
                         else if (list[i].x > cardinalCenter.x && list[i].x < secondVal) {
                            secondIndex = i;
                            secondVal = list[i].x;
                        } else if (list[i].x == cardinalCenter.x) {
                            firstIndex = i;
                            secondIndex = i;
                            break;
                        }
                    }
                }
            } else if (cardinal == Cardinal.East) {
                firstVal = bounds.min.y;
                secondVal = bounds.max.y;
                // Find first and second indexes.
                for (int i = 0; i < list.Count; i++) {
                    if (list[i].x >= cardinalCenter.x) {
                        if (list[i].y < cardinalCenter.y && list[i].y > firstVal) {
                            firstIndex = i;
                            firstVal = list[i].y;
                        }
                         else if (list[i].y > cardinalCenter.y && list[i].y < secondVal) {
                            secondIndex = i;
                            secondVal = list[i].y;
                        } else if (list[i].y == cardinalCenter.y) {
                            firstIndex = i;
                            secondIndex = i;
                            break;
                        }
                    }
                }
            } else if (cardinal == Cardinal.North) {
                firstVal = bounds.max.x;
                secondVal = bounds.min.x;
                // Find first and second indexes.
                for (int i = 0; i < list.Count; i++) {
                    if (list[i].y >= cardinalCenter.y) {
                        if (list[i].x > cardinalCenter.x && list[i].x < firstVal) {
                            firstIndex = i;
                            firstVal = list[i].x;
                        }
                         else if (list[i].x < cardinalCenter.x && list[i].x > secondVal) {
                            secondIndex = i;
                            secondVal = list[i].x;
                        } else if (list[i].x == cardinalCenter.x) {
                            firstIndex = i;
                            secondIndex = i;
                            break;
                        }
                    }
                }
            } else {
                firstVal = bounds.max.y;
                secondVal = bounds.min.y;
                // Find first and second indexes.
                for (int i = 0; i < list.Count; i++) {
                    if (list[i].x <= cardinalCenter.x) {
                        if (list[i].y > cardinalCenter.y && list[i].y < firstVal) {
                            firstIndex = i;
                            firstVal = list[i].y;
                        }
                         else if (list[i].y < cardinalCenter.y && list[i].y > secondVal) {
                            secondIndex = i;
                            secondVal = list[i].y;
                        } else if (list[i].y == cardinalCenter.y) {
                            firstIndex = i;
                            secondIndex = i;
                            break;
                        }
                    }
                }
            }
            
            // An additional point needs to be created.
            if (firstIndex != secondIndex) {
                // Get new point to add.
                Vector2 newPoint;
                if (cardinal == Cardinal.South || cardinal == Cardinal.North) {
                    newPoint = FindYIntercept (list[firstIndex], list[secondIndex], cardinalCenter);
                } else {
                    newPoint = FindXIntercept (list[firstIndex], list[secondIndex], cardinalCenter);
                }
                // Point need to be added between last and fist index.
                list.Insert (secondIndex, newPoint);
            }
            // Circular shift to make the reference index the first element of the list.
            CircularShiftToFront (ref list, secondIndex);
        }
        public static Vector2 FindYIntercept(Vector2 point1, Vector2 point2, Vector2 offset) {
            // Calculate the slope of the line
            float slope = (point2.y - point1.y) / (point2.x - point1.x);

            // Calculate the y-intercept (b) using the equation y = mx + b
            float yIntercept = (point1.y - offset.y) - slope * (point1.x - offset.x);

            // Return the intersection point (x = 0, y = yIntercept)
            return new Vector2 (offset.x, yIntercept + offset.y);
        }
        public static Vector2 FindXIntercept(Vector2 point1, Vector2 point2, Vector2 offset) {
            // Calculate the slope of the line
            float slope = (point2.y - point1.y) / (point2.x - point1.x);

            // Calculate the x-intercept (a) using the equation y = mx + b
            // Rearrange to solve for a:  a = (y - b) / m 
            float yIntercept = (point1.y - offset.y) - slope * (point1.x - offset.x); // Calculate b first
            float xIntercept = (0 - yIntercept) / slope; 

            // Return the intersection point (x = xIntercept, y = 0)
            return new Vector2(xIntercept + offset.x, offset.y);
        }
        public static void CircularShiftToFront(ref List<Vector2> list, int newFirstIndex) {
            if (list == null || list.Count < 2 || newFirstIndex < 0 || newFirstIndex >= list.Count)
            {
                return; // Invalid input
            }
            List<Vector2> tempList = list.GetRange(newFirstIndex, list.Count - newFirstIndex);
            list.RemoveRange(newFirstIndex, list.Count - newFirstIndex);
            list.InsertRange(0, tempList); 
        }
        #endregion
	}
}