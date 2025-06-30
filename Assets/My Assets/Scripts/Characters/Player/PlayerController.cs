using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float _moveSpeed = 2.5f;
    [SerializeField]
    private LayerMask GroundedLayerMask;
    [SerializeField]
    private float GroundedDistance = 0.02f;
    [SerializeField]
    private float _maxFallSpeed = 2f;

    [Header("Movement Modifiers")]
    [SerializeField]
    private float _attackMoveSpeedMultiplier = 0.5f;
    [SerializeField]
    private float _attackMoveSpeedCooldown = 0.1f;
    [SerializeField]
    private bool _applyAttackKnockback = true;
    [SerializeField]
    private float _knockbackDuration = 0.4f;
    [SerializeField]
    private AnimationCurve _knockBackCurve;

    [Header("Dash")]
    [SerializeField]
    private float _dashBufferTime = 0.2f;
    [SerializeField]
    private float _dashSmoothing = 1f;
    [SerializeField]
    private float _dashMaxDistance = 7f;
    [SerializeField]
    private float _dashDuration = 0.35f;

    [Header("Transforms")]
    [SerializeField]
    private Transform _rotationTransform;
    [SerializeField]
    private Transform _lookAt;
    [SerializeField]
    private Transform _playerModel;

    [Header("FX")]
    [SerializeField]
    private float _footstepDistance = 1.45f;
    [SerializeField]
    private AudioClip _footstepSFX;
    [SerializeField]
    private ParticleSystem _dashParticleSystem;
    [SerializeField]
    private AudioClip _dashSFX;

    public PlayerAttack PlayerAttack { get; private set; }
    public Health Health { get; protected set; }
    public Rigidbody Rb { get; private set; }
    public CharacterController CharacterController { get; private set; }
    public bool IsGrounded { get; private set; }
    public float Gravity { get; private set; } = 9.81f;

    private InputManager _inputManager;
    private CinemachineCamera _virtualCamera;
    private PlayerAnimator _playerAnimator;
    private PlayerChargesManager _playerCharges;
    private Vector3 _movementVector;
    private Vector3 xzVelocity;
    private float yVelocity;
    private Vector3 _rotateDirection;
    private float _lastAttackTime;
    private bool _dashWasPressed;
    private bool _isDashing;
    private float _dashBufferTimer;
    private bool _applyingKnockback;
    private Coroutine _knockbackCoroutine;
    private float _knockbackTimer;

    // Footstep Tracking
    private bool _startedMoving;
    private Vector3 _lastPosition;
    private float _distanceSinceLastFootstep;

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
        CharacterController = GetComponent<CharacterController>();
        _playerAnimator = GetComponentInChildren<PlayerAnimator>();
        _playerCharges = GetComponent<PlayerChargesManager>();
        PlayerAttack = GetComponent<PlayerAttack>();
        PlayerAttack.Attacked += OnPlayerAttack;
    }

    private void Start()
    {
        _inputManager = InputManager.Instance;
        _virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        _virtualCamera.Follow = _lookAt; // Set follower
        _virtualCamera.PreviousStateIsValid = false;
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
        if (PauseScreen.IsPaused) return;
        CheckInputs();
        HandleHorizontalMovement();
        HandleVerticalMovement();
        HandleRotation();
        HandleDash();
        CheckGrounded();
    }

    private void CheckInputs()
    {
        _movementVector = new Vector3(_inputManager.Translation.x, 0f, _inputManager.Translation.y);

        if (_inputManager.UsingGamepad)
        {
            _rotateDirection = new Vector3(_inputManager.Direction.x, 0f, _inputManager.Direction.y);
        }
        else
        {
            Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(_lookAt.position);
            Vector3 mouseScreenPos = Input.mousePosition;
            Vector3 playerToCursorDirection = (mouseScreenPos - playerScreenPos).normalized;
            _rotateDirection = playerToCursorDirection;
        }

        if (_inputManager.DashWasPressed && _playerCharges.IsChargeAvailable())
        {
            _dashBufferTimer = _dashBufferTime;
        }
    }

    private void HandleHorizontalMovement()
    {
        if (_applyingKnockback || _isDashing)
        {
            xzVelocity = Vector3.zero;
            return;
        }

        if (_lastAttackTime + _attackMoveSpeedCooldown >= Time.time)
        {
            // Todo: lerp return to base speed
            return;
        }

        if (PauseScreen.IsPaused)
        {
            xzVelocity = Vector3.zero;
        }
        else
        {
            if (_inputManager.IsMovementActive())
            {
                if (!PlayerAttack.AttackIsHeld)
                {
                    _rotationTransform.rotation = Quaternion.LookRotation(_movementVector);
                }

                if (!_startedMoving)
                {
                    _startedMoving = true;
                    PlayFootstep();
                }
            }
            else
            {
                _startedMoving = false;
            }

            if (_distanceSinceLastFootstep >= _footstepDistance)
            {
                PlayFootstep();
            }
            else
            {
                _distanceSinceLastFootstep += Vector3.Distance(_lastPosition, transform.position);
                _lastPosition = transform.position;
            }

            float moveSpeed = !PlayerAttack.AttackIsHeld ? _moveSpeed : _moveSpeed * _attackMoveSpeedMultiplier;
            xzVelocity = _movementVector * (moveSpeed * Time.deltaTime);
            _playerAnimator.SetSpeed(_movementVector.magnitude);
        }
    }

    private void HandleVerticalMovement()
    {
        Vector3 velocity = xzVelocity;

        yVelocity += -Gravity * Time.deltaTime;
        yVelocity = Mathf.Clamp(yVelocity, -_maxFallSpeed * Time.deltaTime, yVelocity);
        velocity.y = yVelocity;

        CharacterController.Move(velocity);
    }

    private void HandleRotation()
    {
        if (!PlayerAttack.AttackIsHeld || PauseScreen.IsPaused) return;

        if (_inputManager.UsingGamepad)
        {
            if (_inputManager.IsDirectionActive())
            {
                var lookAtRotation = Quaternion.LookRotation(_rotateDirection);
                _rotationTransform.localRotation = lookAtRotation;
            }
        }
        else
        {
            float angle = Mathf.Atan2(_rotateDirection.y, _rotateDirection.x) * Mathf.Rad2Deg;
            _rotationTransform.rotation = Quaternion.Euler(0f, -angle + 90f, 0f);
        }
    }

    private void HandleDash()
    {
        if (_dashBufferTimer > 0f)
        {
            if (!_isDashing && !_applyingKnockback)
            {
                StartCoroutine(DashCoroutine());
                _dashBufferTimer = 0f;
            }
        }
        else
        {
            _dashBufferTimer -= Time.deltaTime;
        }
    }

    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        _playerCharges.UseCharge();
        
        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = null;
            _applyingKnockback = false;
        }

        GetComponent<CapsuleCollider>().excludeLayers = LayerMask.GetMask("Enemy");
        CharacterController.excludeLayers = LayerMask.GetMask("Enemy");
        _playerAnimator.SetIsDashing(true);
        _dashParticleSystem.Play();
        float pitch = Random.Range(0.9f, 1.1f);
        AudioManager.Instance.PlaySound(transform, _dashSFX, true, false, 0.85f, pitch);

        float startTime = Time.time;
        var dir = _movementVector == Vector3.zero ? _rotationTransform.forward : _movementVector;
        Vector3 dashVector = dir * _dashMaxDistance;

        while (startTime + _dashDuration >= Time.time && Health.IsAlive())
        {
            var t = _knockbackTimer / _knockbackDuration;
            var curveValue = _knockBackCurve.Evaluate(t);
            var move = dashVector * (curveValue * _dashSmoothing * Time.deltaTime);
            CharacterController.Move(move);
            yield return null;
        }

        // Reset state
        GetComponent<CapsuleCollider>().excludeLayers -= LayerMask.GetMask("Enemy");
        CharacterController.excludeLayers -= LayerMask.GetMask("Enemy");
        _playerAnimator.SetIsDashing(false);
        _isDashing = false;
    }

    private void CheckGrounded()
    {
        var origin = CharacterController.center - Vector3.up * (.5f * CharacterController.height - CharacterController.radius);
        IsGrounded = Physics.SphereCast(
            transform.TransformPoint(origin) + Vector3.up * CharacterController.contactOffset,
            CharacterController.radius,
            Vector3.down,
            out var hit,
            GroundedDistance + CharacterController.contactOffset,
            GroundedLayerMask, QueryTriggerInteraction.Ignore);

        _playerAnimator.SetIsGrounded(IsGrounded);
    }

    private void ToggleMeshRenderers(bool toggle)
    {
        var mrs = _playerModel.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var meshRenderer in mrs)
        {
            meshRenderer.enabled = toggle;
        }
    }

    private void OnPlayerAttack(bool critAttack)
    {
        if (!_applyAttackKnockback) return;

        if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
        _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(critAttack));
        _lastAttackTime = Time.time;
    }

    private IEnumerator KnockbackCoroutine(bool critAttack)
    {
        _applyingKnockback = true;

        var amount = critAttack ? PlayerAttack.PlayerCritKnockbackAmount : PlayerAttack.PlayerBasicKnockbackAmount;
        Vector3 knockbackDir = -_rotationTransform.forward;

        while (_knockbackTimer < _knockbackDuration && Health.IsAlive())
        {
            var t = _knockbackTimer / _knockbackDuration;
            var curveValue = _knockBackCurve.Evaluate(t);
            var move = knockbackDir * (curveValue * amount * Time.deltaTime);
            CharacterController.Move(move);
            _knockbackTimer += Time.deltaTime;
            yield return null;
        }

        _knockbackTimer = 0f;
        _applyingKnockback = false;
        _knockbackCoroutine = null;
    }

    private void OnDied(GameObject deadObj)
    {
        enabled = false;
        _dashBufferTimer = 0f;
        PlayerAttack.enabled = false;
        CharacterController.enabled = false;
        PlayerAttack.OnDied();
        ToggleMeshRenderers(false);
    }

    public void Respawn(Vector3 position, Quaternion rotation, bool canControl)
    {
        enabled = canControl;
        PlayerAttack.enabled = canControl;
        transform.position = position;
        transform.rotation = rotation;
        ToggleMeshRenderers(true);
        Health.OnRespawn();
        PlayerAttack.OnRespawn();
        CharacterController.enabled = true;
    }

    public void ResetCamera()
    {
        StartCoroutine(ResetCameraCoroutine());
    }

    public IEnumerator ResetCameraCoroutine()
    {
        var cinemachineFollow = _virtualCamera.GetComponent<CinemachineFollow>();
        var origTrackerSettings = cinemachineFollow.TrackerSettings;
        cinemachineFollow.TrackerSettings.PositionDamping = Vector3.zero;
        yield return new WaitForSeconds(0.1f);
        cinemachineFollow.TrackerSettings = origTrackerSettings;
    }

    public void PlayFootstep()
    {
        var pitch = Mathf.Clamp(Random.Range(0.9f, 1.2f) * _movementVector.magnitude, 0.8f, 1.2f);
        var volume = _movementVector.magnitude * 0.9f;
        AudioManager.Instance.PlaySound(transform, _footstepSFX, true, false, volume, pitch);
        _distanceSinceLastFootstep = 0f;
    }
}