using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class ZoomZone : MonoBehaviour
{
    public float detectRange = 10f;
    public float outOfSightRange = 15f;
    public float perspectiveFOV = 70f;
    public float orthographicLensSize = 7f;
    public float zoomSpeed = 0.5f;

    private CinemachineCamera virtualCam;
    private float _defaultFOV;
    private float _defaultSize;
    private bool _playerInside;
    private float _newSize;
    private bool _IsOrthographic;
    

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("NoCollision");
    }

    private void Start()
    {
        virtualCam = FindAnyObjectByType<CinemachineCamera>();
        _IsOrthographic = virtualCam.Lens.Orthographic;
        if (_IsOrthographic)
        {
            _defaultSize = virtualCam.Lens.OrthographicSize;
        }
        else
        {
            _defaultFOV = virtualCam.Lens.FieldOfView;
        }
    }

    // Todo: Need a ZoomZoneManager to track which ZoomZone player is in
    private void LateUpdate()
    {
        var distToPlayer = Vector3.Distance(transform.position, GameManager.Instance.Player1.transform.position);

        if (distToPlayer >= outOfSightRange)
        {
            return;
        }

        if (_IsOrthographic)
        {
            var targetSize = distToPlayer <= detectRange ? orthographicLensSize : _defaultSize;
            float currentSize = virtualCam.Lens.OrthographicSize;
            _newSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * zoomSpeed);
            virtualCam.Lens.OrthographicSize = _newSize;
        }
        else
        {
            var targetSize = distToPlayer <= detectRange ? perspectiveFOV : _defaultFOV;
            float currentFOV = virtualCam.Lens.FieldOfView;
            _newSize = Mathf.Lerp(currentFOV, targetSize, Time.deltaTime * zoomSpeed);
            virtualCam.Lens.FieldOfView = _newSize;
        }
    }
}