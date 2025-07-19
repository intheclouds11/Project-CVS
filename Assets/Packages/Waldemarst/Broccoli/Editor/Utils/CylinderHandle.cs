using UnityEngine;
using UnityEditor;

/// <summary>
/// Contains editor-specific classes for the Broccoli Tree Engine.
/// </summary>
namespace Broccoli.BroccoEditor
{
    /// <summary>
    /// Encapsulates the drawing and interaction logic for a cylinder-shaped handle in the Scene view.
    /// </summary>
    public class CylinderHandle
    {
        #region Properties and Variables
        /// <summary>
        /// A local position offset to apply to the handle when drawing.
        /// </summary>
        public Vector3 offset = Vector3.zero;

        /// <summary>
        /// A local scale multiplier to apply to the handle when drawing.
        /// </summary>
        public float scale = 1f;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new CylinderHandle instance.
        /// </summary>
        public CylinderHandle() {}
        #endregion

        #region Draw Methods
        /// <summary>
        /// Draws an interactive cylinder handle in the Scene view that allows modification of its height and radius.
        /// Changes are registered for Undo/Redo.
        /// </summary>
        /// <param name="position">The center of the cylinder's base.</param>
        /// <param name="direction">The upward direction of the cylinder.</param>
        /// <param name="height">The current height of the cylinder. This value will be modified by user input.</param>
        /// <param name="radius">The current radius of the cylinder. This value will be modified by user input.</param>
        /// <param name="color">The color to draw the handle with.</param>
        public void Draw(Vector3 position, Vector3 direction, ref float height, ref float radius, Color color)
        {
            // Set the handle's color
            Handles.color = color;

            // Apply the manager's offset and scale by setting the Handles matrix
            Matrix4x4 handleMatrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            using (new Handles.DrawingScope(handleMatrix))
            {
                // Draw the non-interactive part of the handle (the cylinder body)
                DrawCylinderVolume(position, direction, height, radius);

                // --- Handle Interaction ---
                EditorGUI.BeginChangeCheck();

                // Height handle (slider along the direction vector)
                Vector3 topPosition = position + direction.normalized * height;
                Vector3 newTopPosition = Handles.Slider(topPosition, direction);

                // Radius handle (slider perpendicular to the direction vector)
                Quaternion handleRotation = Quaternion.LookRotation(direction);
                Vector3 radiusDirection = handleRotation * Vector3.right;
                Vector3 radiusHandlePosition = position + radiusDirection * radius;
                Vector3 newRadiusHandlePosition = Handles.Slider(radiusHandlePosition, radiusDirection);
                
                if (EditorGUI.EndChangeCheck())
                {
                    // Record the state before making changes for Undo functionality
                    // Note: The object calling this method should be recorded, e.g., Undo.RecordObject(myScriptableObject, "Change Cylinder Handle");

                    // Calculate the new height and radius from the handle positions
                    height = Vector3.Dot(newTopPosition - position, direction.normalized);
                    radius = Vector3.Distance(position, newRadiusHandlePosition);
                }
            }
        }

        /// <summary>
        /// Draws a non-interactive, read-only representation of the cylinder in the Scene view.
        /// </summary>
        /// <param name="position">The center of the cylinder's base.</param>
        /// <param name="direction">The upward direction of the cylinder.</param>
        /// <param name="height">The height of the cylinder.</param>
        /// <param name="radius">The radius of the cylinder.</param>
        /// <param name="color">The color to draw the handle with.</param>
        public void DrawReadOnly(Vector3 position, Vector3 direction, float height, float radius, Color color)
        {
            Handles.color = color;

            // Apply the manager's offset and scale
            Matrix4x4 handleMatrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            using (new Handles.DrawingScope(handleMatrix))
            {
                DrawCylinderVolume(position, direction, height, radius);
            }
        }
        #endregion

        #region Private Drawing Helper
        /// <summary>
        /// Helper function to draw the wireframe representation of a cylinder.
        /// </summary>
        private void DrawCylinderVolume(Vector3 position, Vector3 direction, float height, float radius)
        {
            if (radius <= 0 || height <= 0) return;

            Vector3 topPosition = position + direction.normalized * height;
            Quaternion handleRotation = Quaternion.LookRotation(direction);

            // Draw the base and top discs
            Handles.DrawWireDisc(position, direction, radius);
            Handles.DrawWireDisc(topPosition, direction, radius);

            // Draw four lines connecting the discs to give it volume
            Handles.DrawLine(position + (handleRotation * Vector3.right * radius), topPosition + (handleRotation * Vector3.right * radius));
            Handles.DrawLine(position + (handleRotation * Vector3.left * radius), topPosition + (handleRotation * Vector3.left * radius));
            Handles.DrawLine(position + (handleRotation * Vector3.forward * radius), topPosition + (handleRotation * Vector3.forward * radius));
            Handles.DrawLine(position + (handleRotation * Vector3.back * radius), topPosition + (handleRotation * Vector3.back * radius));
        }
        #endregion
    }
}