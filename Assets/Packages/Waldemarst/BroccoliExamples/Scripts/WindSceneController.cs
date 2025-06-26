using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Broccoli.Factory;
using Broccoli.Pipe;
using Broccoli.Controller;

namespace Broccoli.Examples 
{
	using Pipeline = Broccoli.Pipe.Pipeline;
	using Position = Broccoli.Pipe.Position;
	public class WindSceneController : MonoBehaviour {
		#region Vars
        public GameObject windArrow = null;
        public GameObject targetWindArrow = null;
        public Camera mainCamera = null;
        public Coroutine coroutine = null;
        BroccoTreeController2 treeController = null;
        public float cameraDistance = 13f;
        public float cameraHeight = 8f;
        public float cameraRotationOffset = 90f;
        public Vector3 cameraTarget = new Vector3 (0, 2.5f, 0);
        public float cameraHemiArcAngle = 135f;
		#endregion

        #region Delegates
        public delegate void OnWindUpdate (float windMain, float windTurbulence, Vector3 windDirection);
        public OnWindUpdate onWindUpdateRequest;
        #endregion

		#region Events
		/// <summary>
		/// Start event.
		/// </summary>
		void Start ()
        {
            // Get only one of the BroccoTreeController2 in the scene to update wind.
            // The rest of the tree controllers should have instance to global and source to self in order to receive wind updates.
            // 1. GLOBAL: wind calculations shared by all instances.
            // 2. SELF: wind values are set by script and not taken from a WindZone.
            BroccoTreeController2[] treeControllers = FindObjectsOfType<BroccoTreeController2> ();
            if (treeControllers != null && treeControllers.Length > 0) {
                treeController = treeControllers [0];
            }

            // If a tree controller was found, set the initial wind values.
            // This script will update the wind with random values every time a click is received.
            if (treeController != null) {
                treeController.windInstance = BroccoTreeController2.WindInstance.Global;
                treeController.globalWindSource = BroccoTreeController2.WindSource.Self;
                treeController.UpdateWind (0f, 0f, Vector3.right);
            }

            // Transition to a breeze wind (main=1, equals seconds per wind unit to transition fron 0 to 1)
            UpdateWind (1f, 1f, Vector3.left);
            //coroutine = StartCoroutine (WindTo (1f, 1f, Vector3.left, 5f));

            if (mainCamera != null) {
                UpdateCamera (0f);
            }
		}
		/// <summary>
		/// Update this instance.
		/// </summary>
		void Update ()
        {
            /*
			if (Input.GetMouseButtonDown (0)) {
                UpdateWind ();
            }
            */
		}
		#endregion

		#region Animations
        public const float minWindMain = 0.15f;
        public const float maxWindMain = 3f;
        public const float minWindTurbulence = 0.2f;
        public const float maxWindTurbulence = 2.2f;
        public const float secondsPerWindUnit = 8f;
        public void UpdateWind ()
        {
            float randomWindValueA = Random.Range (0f, 1f); 
            float randomWindValueB = Random.Range (0f, 1f); 
            float targetWindMain = Mathf.Lerp (minWindMain, maxWindMain, randomWindValueA);
            float targetWindTurbulence = Mathf.Lerp (minWindTurbulence, maxWindTurbulence, randomWindValueB);
            Vector3 targetWindDirection = new Vector3 (Random.Range (-1f, 1f), 0f, Random.Range (-1f, 1f));
            UpdateWind (targetWindMain, targetWindTurbulence, targetWindDirection);
        }
        public void UpdateWind (float windMain, float windTurbulence, Vector3 windDirection)
        {
            if (coroutine != null) {
                StopCoroutine (coroutine);
            }
            // Calculate the number of seconds to transition based on the wind main units.
            float transitionSeconds = Mathf.Abs (treeController.globalWindMain - windMain) * secondsPerWindUnit;
            coroutine = StartCoroutine (WindTo (windMain, windTurbulence, windDirection, transitionSeconds));
            onWindUpdateRequest?.Invoke (windMain, windTurbulence, windDirection);
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
        /// <summary>
		/// Update Wind values as a transition.
		/// </summary>
		/// <param name="windMain">Game object.</param>
		/// <param name="windTurbulence">Target scale.</param>
        /// <param name="windDirection">Target scale.</param>      
		/// <param name="seconds">Seconds.</param>
		/// <param name="destroyAtEnd">If set to <c>true</c> destroy at end.</param>
		IEnumerator WindTo (float windMain, float windTurbulence, Vector3 windDirection, float seconds)
        {
			float progress = 0;
			float startWindMain = treeController.globalWindMain;
            float startWindTurbulence = treeController.globalWindTurbulence;
            Vector3 startWindDirection = treeController.globalWindDirection;
			float finalWindMain = windMain;
            float finalWindTurbulence = windTurbulence;
            Vector3 finalWindDirection = windDirection;
            Vector3 _windDir;
            Quaternion fromToQ = Quaternion.FromToRotation (startWindDirection, finalWindDirection);
            UpdateWindArrow (targetWindArrow, finalWindDirection);
			while (progress <= 1) {
                _windDir = Quaternion.Slerp (Quaternion.identity, fromToQ, progress) * startWindDirection;
                treeController.UpdateWind (
                    Mathf.Lerp (startWindMain, finalWindMain, progress), 
                    //Broccoli.Utils.Easing.EaseOutBack (startWindMain, finalWindMain, progress), 
                    //Broccoli.Utils.Easing.LerpSmoothWave (startWindMain, finalWindMain, 0.3f, 20f, progress), 
                    Mathf.Lerp (startWindTurbulence, finalWindTurbulence, progress), 
                    _windDir);
                UpdateWindArrow (windArrow, _windDir);
				progress += Time.deltaTime * (1f / seconds);
				yield return null;
			}
			treeController.UpdateWind (finalWindMain, finalWindTurbulence, finalWindDirection);
		}
        void UpdateWindArrow (GameObject arrow, Vector3 windDirection)
        {
            if (arrow != null) {
                arrow.transform.forward = windDirection;
            }
        }
		#endregion
	}
}