using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraTriggerZoneManager : MonoBehaviour
{
    public static CameraTriggerZoneManager Instance;

    private readonly List<CameraTriggerZone> _zonesInRange = new();
    private bool _isPerspective;
    private float _lensTarget;
    private float _startingFOV;
    private float _startingLensSize;
    private float _startingPan;
    private float _startingPitch;
    private float _startingRoll; // todo: for weird effect
    private CameraTriggerZone _currentZone;
    private CameraTriggerZone _lastExitedZone;
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
        _zonesInRange.Clear();
        _virtualCam.Lens.FieldOfView = _startingFOV;
        _virtualCam.Lens.OrthographicSize = _startingLensSize;
    }

    private void LateUpdate()
    {
        CameraTriggerZone lastActiveZone = _currentZone ? _currentZone : _lastExitedZone;
        if (!lastActiveZone) return;

        if (_isPerspective)
        {
            _lensTarget = _currentZone ? _startingFOV * _currentZone.ZoomModifier : _startingFOV;
            float newFOV = Mathf.Lerp(_virtualCam.Lens.FieldOfView, _lensTarget, Time.deltaTime * lastActiveZone.ZoomSpeed);
            _virtualCam.Lens.FieldOfView = newFOV;
        }
        else
        {
            _lensTarget = _currentZone ? _startingLensSize * _currentZone.ZoomModifier : _startingLensSize;
            float newSize = Mathf.Lerp(_virtualCam.Lens.OrthographicSize, _lensTarget, Time.deltaTime * lastActiveZone.ZoomSpeed);
            _virtualCam.Lens.OrthographicSize = newSize;
        }

        // todo: why build my own when Cinemachine can blend between cameras
        var panTarget = _currentZone ? _startingPan * _currentZone.PanModifier : _startingPan;
    }

    public void EnteredZone(CameraTriggerZone zone)
    {
        _zonesInRange.Add(zone);
        _currentZone = zone;
    }

    public void ExitedZone(CameraTriggerZone zone)
    {
        _lastExitedZone = zone;
        _zonesInRange.Remove(zone);
        if (!_zonesInRange.Any())
        {
            _currentZone = null;
        }
    }
}