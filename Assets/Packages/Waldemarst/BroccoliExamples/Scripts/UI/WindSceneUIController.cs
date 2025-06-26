using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace Broccoli.Examples 
{
    public class WindSceneUIController : MonoBehaviour
    {
        // Scene object references
        public WindSceneController windSceneController;
        public StyleSheet styleSheet;
        bool listenChanges = true;

        // UI Toolkit references
        private UIDocument uiDocument;
        private Label windInfoLabel;
        private Slider windMainSlider;
        private Slider windTurbulenceSlider;
        private Slider windDirectionSlider;
        private Slider cameraPositionSlider;
        private Button randomizeButton;

        private const string windInfoLabelName = "windInfo";
        private const string windMainSliderName = "windMainSlider";
        private const string windTurbulenceSliderName = "windTurbulenceSlider";
        private const string windDirectionSliderName = "windDirectionSlider";
        private const string cameraPositionSliderName = "cameraPositionSlider";
        private const string randomizeButtonName = "randomizeButton";

        void Start()
        {
            // Get the UIDocument component
            uiDocument = GetComponent<UIDocument>();
            if (styleSheet != null) {
                uiDocument.rootVisualElement.styleSheets.Add (styleSheet);
            }

            // Get the info label.
            windInfoLabel = uiDocument.rootVisualElement.Q<Label>(windInfoLabelName);

            // Wind Main Slider
            windMainSlider = uiDocument.rootVisualElement.Q<Slider>(windMainSliderName);
            if (windMainSlider != null) {
                windMainSlider.lowValue = WindSceneController.minWindMain;
                windMainSlider.highValue = WindSceneController.maxWindMain;
                windMainSlider.RegisterValueChangedCallback (OnWindMainChanged);
            }
            windTurbulenceSlider = uiDocument.rootVisualElement.Q<Slider>(windTurbulenceSliderName);
            if (windTurbulenceSlider != null) {
                windTurbulenceSlider.lowValue = WindSceneController.minWindTurbulence;
                windTurbulenceSlider.highValue = WindSceneController.maxWindTurbulence;
                windTurbulenceSlider.RegisterValueChangedCallback (OnWindTurbulenceChanged);
            }
            windDirectionSlider = uiDocument.rootVisualElement.Q<Slider>(windDirectionSliderName);
            if (windDirectionSlider != null) {
                windDirectionSlider.lowValue = 0f;
                windDirectionSlider.highValue = 360f;
                windDirectionSlider.RegisterValueChangedCallback (OnWindDirectionChanged);
            }
            cameraPositionSlider = uiDocument.rootVisualElement.Q<Slider>(cameraPositionSliderName);
            if (cameraPositionSlider != null) {
                cameraPositionSlider.RegisterValueChangedCallback (OnCameraPositionChanged);
            }
            randomizeButton = uiDocument.rootVisualElement.Q<Button>(randomizeButtonName);

            randomizeButton.clicked += OnRandomizeButtonClicked; // Use clicked for buttons

            windSceneController.onWindUpdateRequest -= OnWindUpdateRequest;
            windSceneController.onWindUpdateRequest += OnWindUpdateRequest;
        }
        private void OnWindUpdateRequest (float windMain, float windTurbulence, Vector3 windDirection)
        {
            listenChanges = false;
            windMainSlider.value = windMain;
            windTurbulenceSlider.value = windTurbulence;
            float degrees = ToAngle(windDirection.x, windDirection.z);
            windDirectionSlider.value = degrees;
            string info = $"Force: {windMain:F2}, Turbulence: {windTurbulence:F2}, Direction: {degrees:F2}";
            windInfoLabel.text = info;
            listenChanges = true;
        }
        private void OnWindMainChanged(ChangeEvent<float> evt)
        {
            if (listenChanges) {
                windSceneController?.UpdateWind (evt.newValue, windTurbulenceSlider.value, ToVector3 (windDirectionSlider.value));
            }
        }
        private void OnWindTurbulenceChanged(ChangeEvent<float> evt)
        {
            if (listenChanges) {
                windSceneController?.UpdateWind (windMainSlider.value, evt.newValue, ToVector3 (windDirectionSlider.value));
            }
        }
        private void OnWindDirectionChanged(ChangeEvent<float> evt)
        {
            if (listenChanges) {
                windSceneController?.UpdateWind (windMainSlider.value, windTurbulenceSlider.value, ToVector3 (evt.newValue));
            }
        }
        private void OnCameraPositionChanged(ChangeEvent<float> evt)
        {
            windSceneController?.UpdateCamera (evt.newValue);
        }

        // Callback for when the reset button is clicked
        private void OnRandomizeButtonClicked()
        {
            windSceneController?.UpdateWind ();
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
            randomizeButton.clicked -= OnRandomizeButtonClicked;
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