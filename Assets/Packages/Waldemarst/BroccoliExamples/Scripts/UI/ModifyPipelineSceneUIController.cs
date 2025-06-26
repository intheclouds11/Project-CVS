using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace Broccoli.Examples 
{
    public class ModifyPipelineSceneUIController : MonoBehaviour
    {
        // Scene object references
        public ModifyPipeline modifyPipelineController;
        public StyleSheet styleSheet;
        bool listenChanges = true;

        // UI Toolkit references
        private UIDocument uiDocument;
        private Label infoLabel;
        private Slider branchGirthSlider;
        private Slider factoryScaleSlider;
        private Slider cameraPositionSlider;
        private Button switchSproutsButton;
        private Button toggleSproutsButton;
        private Button randomizeButton;

        private const string infoLabelName = "infoLabel";
        private const string branchGirthSliderName = "branchGirthSlider";
        private const string factoryScaleSliderName = "factoryScaleSlider";
        private const string cameraPositionSliderName = "cameraPositionSlider";
        private const string switchSproutsButtonName = "sproutMapperButton";
        private const string toggleSproutsButtonName = "toggleSproutsButton";
        private const string randomizeButtonName = "randomizeButton";

        public Camera mainCamera = null;
        public float cameraDistance = 13f;
        public float cameraHeight = 8f;
        public float cameraRotationOffset = 90f;
        public Vector3 cameraTarget = new Vector3 (0, 2.5f, 0);
        public float cameraHemiArcAngle = 135f;

        void Start()
        {
            // Get the UIDocument component
            uiDocument = GetComponent<UIDocument>();
            if (styleSheet != null) {
                uiDocument.rootVisualElement.styleSheets.Add (styleSheet);
            }

            // Get the info label.
            infoLabel = uiDocument.rootVisualElement.Q<Label>(infoLabelName);

            // Wind Main Slider
            branchGirthSlider = uiDocument.rootVisualElement.Q<Slider>(branchGirthSliderName);
            if (branchGirthSlider != null) {
                branchGirthSlider.value = modifyPipelineController.girthValue;
                branchGirthSlider.RegisterValueChangedCallback (OnBranchGirthChanged);
            }
            factoryScaleSlider = uiDocument.rootVisualElement.Q<Slider>(factoryScaleSliderName);
            if (factoryScaleSlider != null) {
                factoryScaleSlider.value = modifyPipelineController.factoryScale;
                factoryScaleSlider.RegisterValueChangedCallback (OnFactoryScaleChanged);
            }
            cameraPositionSlider = uiDocument.rootVisualElement.Q<Slider>(cameraPositionSliderName);
            if (cameraPositionSlider != null) {
                cameraPositionSlider.RegisterValueChangedCallback (OnCameraPositionChanged);
            }

            switchSproutsButton = uiDocument.rootVisualElement.Q<Button>(switchSproutsButtonName);
            switchSproutsButton.clicked += OnSwitchSproutsButtonClicked;

            toggleSproutsButton = uiDocument.rootVisualElement.Q<Button>(toggleSproutsButtonName);
            toggleSproutsButton.clicked += OnToggleSproutsButtonClicked;

            randomizeButton = uiDocument.rootVisualElement.Q<Button>(randomizeButtonName);
            randomizeButton.clicked += OnRandomizeButtonClicked;

            if (mainCamera != null) {
                UpdateCamera (0f);
            }
            UpdateInfo ();
        }
        private void OnBranchGirthChanged(ChangeEvent<float> evt)
        {
            if (listenChanges) {
                modifyPipelineController?.SetGirth (evt.newValue);
                UpdateInfo ();
            }
        }
        private void OnFactoryScaleChanged(ChangeEvent<float> evt)
        {
            if (listenChanges) {
                modifyPipelineController?.SetFactoryScale (evt.newValue);
                UpdateInfo (); 
            }
        }
        private void OnCameraPositionChanged(ChangeEvent<float> evt)
        {
            UpdateCamera (evt.newValue);
        }
        private void OnSwitchSproutsButtonClicked ()
        {
            modifyPipelineController?.SwitchSproutMappers ();
            UpdateInfo ();
        }
        private void OnToggleSproutsButtonClicked ()
        {
            modifyPipelineController?.ToggleSproutMeshGenerator ();
            UpdateInfo ();
        }
        private void OnRandomizeButtonClicked ()
        {
            modifyPipelineController?.ProcessPipeline ();
        }

        private void UpdateInfo ()
        {
            string sproutsInfo = "";
            if (modifyPipelineController.isSproutMeshGeneratorActive) {
                if (modifyPipelineController.sproutMapperSelected == 0) sproutsInfo = "autumn";
                else sproutsInfo = "green";
            } else {
                sproutsInfo = "inactive";
            }
            string info = $"Factory Scale: {factoryScaleSlider.value:F2}, Girth: {branchGirthSlider.value:F2}, Sprouts: {sproutsInfo}";
            infoLabel.text = info;
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

        /// <summary>
        /// Updates the camera position.
        /// </summary>
        /// <param name="offsetAngles">Values from -1 to 1</param>
        public void UpdateCamera (float offsetAngles) {
            if (mainCamera != null) {
                float newAngle = Mathf.Deg2Rad * (cameraRotationOffset + cameraHemiArcAngle * offsetAngles);
                mainCamera.transform.position = new Vector3 (
                    Mathf.Cos (newAngle) * cameraDistance, 
                    cameraHeight,
                    Mathf.Sin (newAngle) * cameraDistance);
                mainCamera.transform.LookAt (cameraTarget);
            }
        }
    }
}