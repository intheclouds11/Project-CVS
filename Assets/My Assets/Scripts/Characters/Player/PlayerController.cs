using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float _moveSpeed = 2.5f;
    [SerializeField]
    private float _rotateSmoothing = 3f;
    [SerializeField]
    private float _gravity = 3f;
    [SerializeField]
    private Transform _rotationTransform;
    [SerializeField]
    private Transform _lookAt;
    public Transform GetLookAtTransform() => _lookAt;
    [SerializeField]
    private Transform _projectileSpawnPoint;
    [SerializeField]
    private GameObject _projectilePrefab;

    public Health Health { get; protected set; }

    private bool _allowForward;
    private bool _allowBackward;
    private Vector3 _mouseHitPosition;
    private bool _mouseHitting;
    private Rigidbody _rb;
    private InputManager _inputManager;
    private CinemachineCamera virtualCamera;
    private Vector3 _moveInput;
    private Vector3 _rotateInput;
    private bool _isGrounded;

    [Header("Debug")]
    [SerializeField]
    private bool showMouseDebugSphere = true;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Health = GetComponent<Health>();
        Health.Died += OnDied;
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _inputManager = InputManager.instance;
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        virtualCamera.Follow = _lookAt; // Set follower
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        if (virtualCamera)
        {
            virtualCamera.Follow = _lookAt; // Set follower
        }
    }

    private void Update()
    {
        if (PauseMenu.Instance.gameObject.activeSelf) return;

        CheckInputs();

        if (_inputManager.fireWasPressed)
        {
            Fire();
        }
    }

    private void FixedUpdate()
    {
        if (PauseMenu.Instance.gameObject.activeSelf) return;

        CheckMovementCollision();
        Move();
        Rotate();
    }

    private void CheckInputs()
    {
        CheckMouseHitResult();
        _moveInput = new Vector3(_inputManager.translation.x, 0f, _inputManager.translation.y);
        if (_inputManager.controlScheme == InputManager.ControlScheme.Gamepad)
        {
            _rotateInput = new Vector3(_inputManager.direction.x, 0f, _inputManager.direction.y);
        }
        else if (_inputManager.controlScheme == InputManager.ControlScheme.MouseKeyboard)
        {
            _rotateInput = _mouseHitPosition - _rb.position;
        }
    }

    private void CheckMouseHitResult()
    {
        var pointerPosition = Mouse.current.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(pointerPosition);

        if (Physics.Raycast(ray, out var hit, Mathf.Infinity, ~LayerMask.GetMask("Friendly")))
        {
            _mouseHitting = true;
            _mouseHitPosition = hit.point;
        }
        else
        {
            _mouseHitting = false;
        }
    }

    private void CheckMovementCollision()
    {
        if (Physics.BoxCast(_lookAt.position, transform.localScale * 0.1f, transform.forward, out var hitFwd, Quaternion.identity,
                0.1f))
        {
            _allowForward = false;
        }
        else
        {
            _allowForward = true;
        }

        if (Physics.BoxCast(_lookAt.position, transform.localScale * 0.1f, -transform.forward, out var hitBwd, Quaternion.identity,
                0.2f))
        {
            _allowBackward = false;
        }
        else
        {
            _allowBackward = true;
        }
    }

    private void Move()
    {
        if (_moveInput != Vector3.zero && (_allowForward || _allowBackward))
        {
            Vector3 moveDirection = _rb.rotation * _moveInput;
            Vector3 targetPos = _rb.position + moveDirection * (_moveSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(targetPos);
        }
    }

    private void Rotate()
    {
        if (_inputManager.controlScheme == InputManager.ControlScheme.Gamepad && _rotateInput.magnitude > 0.7f)
        {
            var lookAtRotation = Quaternion.LookRotation(_rotateInput);
            _rotationTransform.localRotation = Quaternion.RotateTowards(_rotationTransform.localRotation, lookAtRotation,
                _rotateSmoothing * Time.fixedDeltaTime);
        }
        else if (_inputManager.controlScheme == InputManager.ControlScheme.MouseKeyboard)
        {
            Debug.DrawRay(_rb.position, _rotateInput, Color.red);

            var directionYaw = new Vector3(_rotateInput.x, 0f, _rotateInput.z);
            var lookAtRotation = Quaternion.LookRotation(directionYaw);
            _rotationTransform.rotation =
                Quaternion.RotateTowards(_rotationTransform.rotation, lookAtRotation, _rotateSmoothing * Time.fixedDeltaTime);
        }
    }

    private void Fire()
    {
        _rb.AddForce(-_projectileSpawnPoint.forward * 1.5f, ForceMode.Impulse);

        var projectile = Instantiate(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
        projectile.GetComponent<SawBlade>().isPlayerProjectile = true;
    }

    private void OnDied(GameObject deadObj)
    {
        enabled = false;
    }

    public void Respawn(Vector3 position, Quaternion rotation, bool controllable)
    {
        _rb.position = position;
        _rb.rotation = rotation;
        enabled = controllable;
        Health.OnRespawn();
    }


    private void OnDrawGizmos()
    {
        if (showMouseDebugSphere && _mouseHitting)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_mouseHitPosition, 0.2f);
        }
    }
}