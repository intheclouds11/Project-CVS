using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
namespace Broccoli.Examples 
{
    public class RuntimeSceneUIController : MonoBehaviour
    {
        // Scene object references
        public RuntimeSceneController runtimeSceneController;
        public StyleSheet styleSheet;

        // UI Toolkit references
        private UIDocument uiDocument;
        private Button clearButton;

        private const string clearButtonName = "clearButton";

        void Start()
        {
            // Get the UIDocument component
            uiDocument = GetComponent<UIDocument>();
            if (styleSheet != null) {
                uiDocument.rootVisualElement.styleSheets.Add (styleSheet);
            }

            clearButton = uiDocument.rootVisualElement.Q<Button>(clearButtonName);

            clearButton.clicked += OnRandomizeButtonClicked;
        }

        // Callback for when the reset button is clicked
        private void OnRandomizeButtonClicked()
        {
            runtimeSceneController?.ClearInstances ();
        }

        void Update()
        {
            /*
            if (objectToRotate != null)
            {
                objectToRotate.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            */
        }


        // Clean up event listeners (good practice)
        void OnDisable()
        {
            //lightIntensitySlider.UnregisterValueChangedCallback(OnLightIntensityChanged);
            //rotationSpeedSlider.UnregisterValueChangedCallback(OnRotationSpeedChanged);
            clearButton.clicked -= OnRandomizeButtonClicked;
        }
        public float ToAngle (float x, float y)
        {
            // Calculate the angle in radians using Atan2
            float angleRadians = Mathf.Atan2(y, x);

            // Convert radians to degrees
            float angleDegrees = angleRadians * Mathf.Rad2Deg;

            // Adjust the angle to be in the range [0, 360)
            if (angleDegrees < 0)
            {
                angleDegrees += 360;
            }

            return angleDegrees;
        }

        public Vector3 ToVector3(float angleDegrees)
        {
            // Convert degrees to radians
            float angleRadians = angleDegrees * Mathf.Deg2Rad;

            // Calculate the x and y components of the vector
            float x = Mathf.Cos(angleRadians);
            float y = Mathf.Sin(angleRadians);

            // Create and return the Vector2
            return new Vector3(x, 0, y);
        }
    }
}
#endif