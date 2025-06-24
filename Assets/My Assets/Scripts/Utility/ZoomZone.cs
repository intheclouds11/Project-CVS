using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class ZoomZone : MonoBehaviour
{
    public float detectRange = 10f;
    public float outOfSightRange = 15f;
    public float zoomOutSize = 7f;
    public float zoomSpeed = 0.5f;

    private CinemachineCamera virtualCam;
    private float _defaultSize;
    private bool _playerInside;
    private float _newSize;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("NoCollision");
    }

    private void Start()
    {
        virtualCam = FindAnyObjectByType<CinemachineCamera>();
        _defaultSize = virtualCam.Lens.OrthographicSize;
    }

    // Todo: Need a ZoomZoneManager to track which ZoomZone player is in
    private void LateUpdate()
    {
        var distToPlayer = Vector3.Distance(transform.position, GameManager.Instance.Player1.transform.position);

        if (distToPlayer >= outOfSightRange)
        {
            return;
        }
        
        var targetSize = distToPlayer <= detectRange ? zoomOutSize : _defaultSize;
        float currentSize = virtualCam.Lens.OrthographicSize;
        _newSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * zoomSpeed);
        virtualCam.Lens.OrthographicSize = _newSize;
    }
}