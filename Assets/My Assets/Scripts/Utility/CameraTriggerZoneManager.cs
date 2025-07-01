using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraTriggerZoneManager : MonoBehaviour
{
    public static CameraTriggerZoneManager Instance;

    private CameraTriggerZone _currentZone;
    private CameraTriggerZone _lastExitedZone;
    private float _lerpTarget;
    private bool _isPerspective;
    private float _startingFOV;
    private float _startingLensSize;
    private CinemachineCamera _virtualCam;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Init();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Init()
    {
        _virtualCam = FindAnyObjectByType<CinemachineCamera>();
        _isPerspective = !_virtualCam.Lens.Orthographic;
        _startingFOV = _virtualCam.Lens.FieldOfView;
        _startingLensSize = _virtualCam.Lens.OrthographicSize;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        Init();
        _currentZone = null;
        _lastExitedZone = null;
        _virtualCam.Lens.FieldOfView = _startingFOV;
        _virtualCam.Lens.OrthographicSize = _startingLensSize;
    }

    private void LateUpdate()
    {
        CameraTriggerZone lastActiveZone = _currentZone ? _currentZone : _lastExitedZone;
        if (!lastActiveZone) return;

        if (_isPerspective)
        {
            _lerpTarget = _currentZone ? _startingFOV * _currentZone.ZoomModifier : _startingFOV;
            float newFOV = Mathf.Lerp(_virtualCam.Lens.FieldOfView, _lerpTarget, Time.deltaTime * lastActiveZone.ZoomSpeed);
            _virtualCam.Lens.FieldOfView = newFOV;
        }
        else
        {
            _lerpTarget = _currentZone ? _startingLensSize * _currentZone.ZoomModifier : _startingLensSize;
            float newSize = Mathf.Lerp(_virtualCam.Lens.OrthographicSize, _lerpTarget, Time.deltaTime * lastActiveZone.ZoomSpeed);
            _virtualCam.Lens.OrthographicSize = newSize;
        }
    }

    public void EnteredZone(CameraTriggerZone zone)
    {
        _currentZone = zone;
    }

    public void ExitedZone(CameraTriggerZone zone)
    {
        _lastExitedZone = zone;
        _currentZone = null;
    }
}