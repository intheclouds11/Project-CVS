using System.Collections;
using Unity.Cinemachine;
using Unity.Mathematics;
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
    private float _attackMoveSpeedMultiplier = 0.5f;
    [SerializeField]
    private float _attackMoveSpeedCooldown = 0.5f;
    [SerializeField]
    private float _knockbackTime = 0.2f;
    [SerializeField]
    private float _knockbackSmoothing = 5f;
    [SerializeField]
    private float _dashMaxDistance = 2.5f;
    [SerializeField]
    private float _dashTime = 0.5f;

    [Header("Transforms")]
    [SerializeField]
    private Transform _rotationTransform;
    [SerializeField]
    private Transform _lookAt;

    [Header("FX")]
    [SerializeField]
    private ParticleSystem _dashParticleSystem;
    [SerializeField]
    private AudioClip _dashSFX;

    public Health Health { get; protected set; }
    public Rigidbody Rb { get; private set; }

    private bool _allowForward;
    private bool _allowBackward;
    private InputManager _inputManager;
    private CinemachineCamera _virtualCamera;
    private PlayerAttack _playerAttack;
    private Vector2 _moveInput;
    private Vector3 _rotateDirection;
    private bool _dashWasPressed;
    private bool _isDashing;
    private bool _isGrounded;
    private bool _applyingKnockback;
    private float _lastAttackTime;

    [Header("Debug")]
    [SerializeField]
    private bool showMouseAimDirection = true;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Health = GetComponent<Health>();
        Health.Died += OnDied;
        Rb = GetComponent<Rigidbody>();
        _playerAttack = GetComponent<PlayerAttack>();
        _playerAttack.Attacked += OnPlayerAttack;
    }

    private void Start()
    {
        _inputManager = InputManager.Instance;
        _virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        _virtualCamera.Follow = _lookAt; // Set follower
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        _virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        if (_virtualCamera)
        {
            _virtualCamera.Follow = _lookAt;
        }
    }

    private void Update()
    {
        if (PauseMenu.Instance.gameObject.activeSelf) return;

        CheckInputs();
    }

    private void FixedUpdate()
    {
        if (PauseMenu.Instance.gameObject.activeSelf) return;

        CheckMovementCollision();

        if (!_isDashing)
        {
            HandleMovement();
        }

        HandleRotation();
        HandleDash();
    }

    private void CheckInputs()
    {
        _moveInput = _inputManager.Translation;

        if (_inputManager.Scheme == InputManager.ControlScheme.Gamepad)
        {
            _rotateDirection = new Vector3(_inputManager.Direction.x, 0f, _inputManager.Direction.y);
        }
        else if (_inputManager.Scheme == InputManager.ControlScheme.MouseKeyboard)
        {
            // Get playerToCursorDirection
            Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(transform.position);
            Vector3 mouseScreenPos = Input.mousePosition;
            Vector3 playerToCursorDirection = (mouseScreenPos - playerScreenPos).normalized;
            _rotateDirection = playerToCursorDirection;
        }

        _dashWasPressed = _inputManager.DashWasPressed;
    }

    private void CheckMovementCollision()
    {
        if (Physics.BoxCast(_lookAt.position, transform.localScale * 0.1f, _rotationTransform.forward, out var hitFwd,
                Quaternion.identity,
                0.1f))
        {
            _allowForward = false;
        }
        else
        {
            _allowForward = true;
        }

        if (Physics.BoxCast(_lookAt.position, transform.localScale * 0.1f, -_rotationTransform.forward, out var hitBwd,
                Quaternion.identity,
                0.2f))
        {
            _allowBackward = false;
        }
        else
        {
            _allowBackward = true;
        }
    }

    private void HandleMovement()
    {
        if (_applyingKnockback) return;

        float moveSpeed = !_playerAttack.AttackInputHeld ? _moveSpeed : _moveSpeed * _attackMoveSpeedMultiplier;

        if (_lastAttackTime + _attackMoveSpeedCooldown >= Time.time)
        {
            return;
        }

        if (_moveInput != Vector2.zero && (_allowForward || _allowBackward))
        {
            var moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
            Vector3 targetPos = Rb.position + moveDirection * (moveSpeed * Time.fixedDeltaTime);
            Rb.MovePosition(targetPos);
            if (!_playerAttack.AttackInputHeld) _rotationTransform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    private void HandleRotation()
    {
        if (!_playerAttack.AttackInputHeld || _rotateDirection.magnitude <= 0f || _rotateDirection.magnitude <= 0.7f) return;

        if (_inputManager.Scheme == InputManager.ControlScheme.Gamepad)
        {
            var lookAtRotation = Quaternion.LookRotation(_rotateDirection);
            _rotationTransform.localRotation = lookAtRotation;
        }
        else if (_inputManager.Scheme == InputManager.ControlScheme.MouseKeyboard)
        {
            float angle = Mathf.Atan2(_rotateDirection.y, _rotateDirection.x) * Mathf.Rad2Deg;
            _rotationTransform.rotation = Quaternion.Euler(0f, -angle + 90f, 0f);
        }
    }

    private void HandleDash()
    {
        if (_dashWasPressed)
        {
            // _isDashing = true;
            _dashParticleSystem.Play();
        }

        if (_isDashing)
        {
        }
    }

    private void OnPlayerAttack(bool critAttack)
    {
        StartCoroutine(KnockbackCoroutine(critAttack));
        _lastAttackTime = Time.time;
    }

    private IEnumerator KnockbackCoroutine(bool critAttack)
    {
        _applyingKnockback = true;

        var amount = critAttack ? _playerAttack.PlayerCritKnockbackDistance : _playerAttack.PlayerBasicKnockbackDistance;
        Vector3 knockbackDir = -_rotationTransform.forward * amount;
        var targetPos = Rb.position + knockbackDir;
        float startTime = Time.time;

        while (startTime + _knockbackTime >= Time.time)
        {
            var targetPosition = Vector3.Lerp(Rb.position, targetPos, _knockbackSmoothing * Time.deltaTime);
            Rb.MovePosition(targetPosition);
            yield return null;
        }

        _applyingKnockback = false;
    }

    private void OnDied(GameObject deadObj)
    {
        enabled = false;
    }

    public void Respawn(Vector3 position, Quaternion rotation, bool controllable)
    {
        Rb.position = position;
        Rb.rotation = rotation;
        enabled = controllable;
        Health.OnRespawn();
    }


    private void OnDrawGizmos()
    {
        if (showMouseAimDirection)
        {
            // Gizmos.color = Color.red;
            // Gizmos.DrawWireSphere(_mouseHitPosition, 0.2f);
        }
    }
}