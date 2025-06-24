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
    private float _knockbackDuration = 0.35f;
    [SerializeField]
    private AnimationCurve _knockBackCurve;
    [SerializeField]
    private float _dashSmoothing = 20f;
    [SerializeField]
    private float _dashMaxDistance = 2.5f;
    [SerializeField]
    private float _dashTime = 0.5f;

    [Header("Transforms")]
    [SerializeField]
    private Transform _rotationTransform;
    [SerializeField]
    private Transform _lookAt;
    [SerializeField]
    private Transform _playerModel;

    [Header("FX")]
    [SerializeField]
    private ParticleSystem _dashParticleSystem;
    [SerializeField]
    private AudioClip _dashSFX;

    public PlayerAttack PlayerAttack { get; private set; }
    public Health Health { get; protected set; }
    public Rigidbody Rb { get; private set; }
    public CharacterController Controller { get; private set; }

    private InputManager _inputManager;
    private CinemachineCamera _virtualCamera;
    private Vector3 _movementDirection;
    private Vector3 _rotateDirection;
    private bool _dashWasPressed;
    private bool _isDashing;
    private bool _isGrounded;
    private float _lastAttackTime;
    private bool _applyingKnockback;
    private Coroutine _knockbackCoroutine;
    private float _knockbackTimer;
    private float _dashInputBufferTime = 0.2f;
    private float _dashBufferTimer;

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
        Controller = GetComponent<CharacterController>();
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
        if (PauseScreen.Instance.gameObject.activeSelf) return;

        CheckInputs();
    }

    private void FixedUpdate()
    {
        if (PauseScreen.Instance.gameObject.activeSelf) return;
        
        HandleMovement();
        HandleRotation();
        HandleDash();
    }

    private void CheckInputs()
    {
        _movementDirection = new Vector3(_inputManager.Translation.x, 0f, _inputManager.Translation.y);

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

        if (_inputManager.DashWasPressed)
        {
            _dashBufferTimer = _dashInputBufferTime;
        }
    }

    private void HandleMovement()
    {
        if (_applyingKnockback || _isDashing) return;

        float moveSpeed = !PlayerAttack.AttackInputHeld ? _moveSpeed : _moveSpeed * _attackMoveSpeedMultiplier;

        if (_lastAttackTime + _attackMoveSpeedCooldown >= Time.time)
        {
            // lerp return to base speed
            return;
        }

        if (_inputManager.IsMovementActive())
        {
            Vector3 move = _movementDirection * (moveSpeed * Time.fixedDeltaTime);
            Controller.Move(move);
            if (!PlayerAttack.AttackInputHeld) _rotationTransform.rotation = Quaternion.LookRotation(_movementDirection);
        }
    }

    private void HandleRotation()
    {
        if (!PlayerAttack.AttackInputHeld) return;

        if (_inputManager.UsingGamepad && _inputManager.IsDirectionActive())
        {
            var lookAtRotation = Quaternion.LookRotation(_rotateDirection);
            _rotationTransform.localRotation = lookAtRotation;
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
            _dashBufferTimer -= Time.fixedDeltaTime;
        }
    }

    private IEnumerator DashCoroutine()
    {
        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = null;
            _applyingKnockback = false;
        }

        _isDashing = true;
        ToggleMeshRenderers(false);
        GetComponent<CapsuleCollider>().excludeLayers = LayerMask.GetMask("Enemy");
        Controller.excludeLayers = LayerMask.GetMask("Enemy");
        _dashParticleSystem.Play();
        AudioManager.Instance.PlaySound(transform, _dashSFX);

        var dir = _movementDirection == Vector3.zero ? _rotationTransform.forward : _movementDirection;
        Vector3 dashVector = dir * _dashMaxDistance;
        float startTime = Time.time;

        while (startTime + _dashTime >= Time.time && Health.IsAlive())
        {
            var t = _knockbackTimer / _knockbackDuration;
            var curveValue = _knockBackCurve.Evaluate(t);
            var move = dashVector * (curveValue * _dashSmoothing * Time.deltaTime);
            Controller.Move(move);
            yield return new WaitForFixedUpdate();
        }

        ToggleMeshRenderers(true);
        GetComponent<CapsuleCollider>().excludeLayers -= LayerMask.GetMask("Enemy");
        Controller.excludeLayers -= LayerMask.GetMask("Enemy");
        _isDashing = false;
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
            Controller.Move(move);
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
        Controller.enabled = false;
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
        Controller.enabled = true;
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

    private void OnDrawGizmos()
    {
        if (showMouseAimDirection)
        {
            // Gizmos.color = Color.red;
            // Gizmos.DrawWireSphere(_mouseHitPosition, 0.2f);
        }
    }
}