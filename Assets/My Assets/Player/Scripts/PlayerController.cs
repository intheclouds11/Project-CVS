using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float _moveSpeed = 2.5f;
    [SerializeField]
    private float _maxFallSpeed = 2f;

    [Header("Movement Modifiers")]
    [SerializeField]
    private float _attackMoveSpeedMultiplier = 0.5f;
    [SerializeField]
    private float _attackMoveSpeedCooldown = 0.1f;
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
    [SerializeField]
    private AnimationCurve _dashCurve;

    [Header("Transforms")]
    [field: SerializeField]
    public Transform RotationTransform { get; private set; }
    [field: SerializeField]
    public Transform LookAt { get; private set; }
    [SerializeField]
    private Transform _playerModel;

    [Header("FX")]
    [SerializeField]
    private float _deathDelay = 0.3f;
    [SerializeField]
    private float _footstepDistance = 1.45f;
    [SerializeField]
    private AudioClip _footstepSFX;
    [SerializeField]
    private ParticleSystem _dashParticleSystem;
    [SerializeField]
    private AudioClip _dashSFX;

    public CharacterController CharacterController { get; private set; }
    public PlayerHealth Health { get; protected set; }
    public PlayerAttack PlayerAttack { get; private set; }
    public PlayerChargesManager PlayerCharges { get; private set; }
    public float Gravity { get; private set; } = 9.81f;
    public bool IsDashing { get; private set; }

    private Vector3 _movementVector;
    private Vector3 xzVelocity;
    private float yVelocity;

    private float _lastAttackTime;
    private bool _dashWasPressed;
    private float _dashBufferTimer;
    private float _dashTimeElapsed;
    private bool _applyingKnockback;
    private float _knockbackTimeElapsed;

    // Footstep Tracking
    private bool _startedMoving;
    private Vector3 _lastPosition;
    private float _distanceSinceLastFootstep;

    private Coroutine _knockbackCoroutine;
    private InputManager _inputManager;
    private CinemachineCamera _virtualCamera;
    private PlayerAnimator _playerAnimator;
    private Collider _triggerCollider;
    private List<SkinnedMeshRenderer> _skinnedMeshRenderers;
    public List<Material> _transparentMaterials;
    public List<Material> _originalMaterials;


    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Health = GetComponent<PlayerHealth>();
        Health.Died += OnDied;
        Health.DamageTaken += OnDamageTaken;
        CharacterController = GetComponent<CharacterController>();
        _playerAnimator = GetComponentInChildren<PlayerAnimator>();
        PlayerCharges = GetComponent<PlayerChargesManager>();
        PlayerAttack = GetComponent<PlayerAttack>();
        PlayerAttack.Attacked += OnPlayerAttack;
        _triggerCollider = GetComponent<Collider>();

        _skinnedMeshRenderers = _playerModel.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
    }

    private void Start()
    {
        if (!gameObject.scene.name.Equals("DontDestroyOnLoad"))
        {
            Debug.LogError($"Player is a scene object in scene: {gameObject.scene.name}");
        }

        _inputManager = InputManager.Instance;
        _virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        _virtualCamera.Follow = LookAt; // Set follower
        _virtualCamera.PreviousStateIsValid = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        _virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        if (_virtualCamera)
        {
            _virtualCamera.Follow = LookAt;
            CinemachineImpulseManager.Instance.Clear();
        }
    }

    private void Update()
    {
        if (!_inputManager.InputsAllowed || !CharacterController.enabled)
        {
            xzVelocity = Vector3.zero;
            _playerAnimator.SetSpeed(0f);
            return;
        }

        CheckInputs();
        HandleHorizontalMovement();
        HandleVerticalMovement();
        HandleRotation();
        HandleDash();
    }

    private void CheckInputs()
    {
        _movementVector = new Vector3(_inputManager.Translation.x, 0f, _inputManager.Translation.y);

        if (_inputManager.DashWasPressed && PlayerCharges.IsChargeAvailable())
        {
            _dashBufferTimer = _dashBufferTime;
        }
    }

    public Vector3 CalculateRotateDirection()
    {
        if (_inputManager.UsingGamepad)
        {
            return new Vector3(_inputManager.Direction.x, 0f, _inputManager.Direction.y).normalized;
        }

        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(LookAt.position);
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 playerToCursorDirection = (mouseScreenPos - playerScreenPos).normalized;
        return playerToCursorDirection;
    }

    private void HandleHorizontalMovement()
    {
        if (PlayerAttack.IsAttacking || _applyingKnockback || IsDashing)
        {
            xzVelocity = Vector3.zero;
            return;
        }

        if (_lastAttackTime + _attackMoveSpeedCooldown >= Time.time)
        {
            // Todo: lerp return to base speed
            return;
        }

        if (_inputManager.IsMovementActive())
        {
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

        float moveSpeed = !PlayerAttack.IsCharging ? _moveSpeed : _moveSpeed * _attackMoveSpeedMultiplier;
        xzVelocity = _movementVector * (moveSpeed * Time.deltaTime);
        _playerAnimator.SetSpeed(_movementVector.magnitude);
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
        if (PlayerAttack.State == PlayerAttack.AttackState.Attacking)
        {
            return;
        }

        if (PlayerAttack.State == PlayerAttack.AttackState.Idle)
        {
            if (_inputManager.IsMovementActive() && !_inputManager.AttackHeld)
            {
                RotationTransform.rotation = Quaternion.LookRotation(_movementVector);
            }

            return;
        }
    }

    public void SetAttackRotateDirection(Vector3 direction)
    {
        if (_inputManager.UsingGamepad)
        {
            var lookAtRotation = Quaternion.LookRotation(direction);
            RotationTransform.localRotation = lookAtRotation;
        }
        else
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            RotationTransform.rotation = Quaternion.Euler(0f, -angle + 90f, 0f);
        }
    }

    private void HandleDash()
    {
        if (_dashBufferTimer > 0f)
        {
            if (!IsDashing && !_applyingKnockback)
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
        IsDashing = true;
        PlayerCharges.UseCharge();

        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = null;
            _applyingKnockback = false;
        }

        GetComponent<CapsuleCollider>().excludeLayers = LayerMask.GetMask("Enemy");
        CharacterController.excludeLayers = LayerMask.GetMask("Enemy");
        if (!PlayerAttack.IsCharging) _playerAnimator.SetIsDashing(true);

        if (!Health.IsInvincible())
        {
            FadeMeshRenderers(false, 0.2f, 0.05f);
        }

        _dashParticleSystem.Play();
        float pitch = Random.Range(0.9f, 1.1f);
        AudioManager.Instance.PlaySound(transform, _dashSFX, true, false, 0.85f, pitch);

        var dir = _movementVector == Vector3.zero ? RotationTransform.forward : _movementVector.normalized;
        Vector3 dashVector = dir * _dashMaxDistance;

        while (_dashTimeElapsed < _dashDuration && Health.IsAlive())
        {
            var t = _dashTimeElapsed / _dashDuration;
            var curveValue = _dashCurve.Evaluate(t);
            var move = dashVector * (curveValue * _dashSmoothing * Time.deltaTime);
            CharacterController.Move(move);
            _dashTimeElapsed += Time.deltaTime;
            yield return null;
        }

        // Reset state
        if (!Health.IsInvincible())
        {
            FadeMeshRenderers(true, 0.2f);
        }

        GetComponent<CapsuleCollider>().excludeLayers -= LayerMask.GetMask("Enemy");
        CharacterController.excludeLayers -= LayerMask.GetMask("Enemy");
        _playerAnimator.SetIsDashing(false);
        _dashTimeElapsed = 0f;
        IsDashing = false;
    }

    private void OnPlayerAttack(Knockback attackKnockback)
    {
        _lastAttackTime = Time.time;
        if (!attackKnockback.ApplyKnockback) return;

        if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
        Vector3 knockbackDir = -RotationTransform.forward;
        _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(knockbackDir, attackKnockback));
    }

    private IEnumerator KnockbackCoroutine(Vector3 dir, Knockback knockback)
    {
        // Debug.Log($"Start Knockback. Amount: {knockbackAmount}, Dir: {dir}, Duration: {knockbackDuration}");
        _applyingKnockback = true;

        while (_knockbackTimeElapsed < knockback.KnockbackDuration && Health.IsAlive())
        {
            var t = _knockbackTimeElapsed / knockback.KnockbackDuration;
            var curveValue = _knockBackCurve.Evaluate(t);
            var move = dir * (curveValue * knockback.KnockbackAmount * Time.deltaTime);
            CharacterController.Move(move);
            _knockbackTimeElapsed += Time.deltaTime;
            yield return null;
        }

        _knockbackTimeElapsed = 0f;
        _applyingKnockback = false;
        _knockbackCoroutine = null;
    }

    private void OnDamageTaken(Vector3 knockbackDir, Knockback damagedKnockback)
    {
        _playerAnimator.SetSpeed(0f);
        //todo: damage knockback animation

        if (damagedKnockback == null || knockbackDir == Vector3.zero || damagedKnockback.KnockbackAmount <= 0) return;

        if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
        _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(knockbackDir, damagedKnockback));
    }

    private void OnDied(GameObject deadObj)
    {
        _dashBufferTimer = 0f;
        CharacterController.enabled = false;
        TogglePlayerTriggerCollider(false);
        PlayerAttack.OnDied();
        _playerAnimator.SetSpeed(0f);
        _playerAnimator.SetDiedTrigger();
        StartCoroutine(DiedCoroutine());
    }

    private IEnumerator DiedCoroutine()
    {
        yield return new WaitForSeconds(_deathDelay);
        FadeMeshRenderers(false, 0.25f);
    }

    public void Respawn(PlayerSpawnPoint spawnPoint)
    {
        transform.position = spawnPoint.transform.position;
        RotationTransform.rotation = spawnPoint.transform.rotation;
        FadeMeshRenderers(true, 0.5f);
        Health.OnRespawn();
        PlayerAttack.OnRespawn();
        PlayerCharges.OnRespawn();
        AudioManager.Instance.OnPlayerRespawned();
        CharacterController.enabled = true;
        TogglePlayerTriggerCollider(true);
    }

    public void TogglePlayerTriggerCollider(bool toggle)
    {
        _triggerCollider.enabled = toggle;
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

    private Coroutine _fadeMRsCoroutine;

    public void FadeMeshRenderers(bool show, float fadeDuration, float alphaTarget = 0f)
    {
        if (_fadeMRsCoroutine != null) StopCoroutine(_fadeMRsCoroutine);
        _fadeMRsCoroutine = StartCoroutine(FadeMeshRenderersCoroutine(show, fadeDuration, alphaTarget));
    }

    private IEnumerator FadeMeshRenderersCoroutine(bool show, float fadeDuration, float alphaTarget = 0f)
    {
        if (!show)
        {
            for (int i = 0; i < _skinnedMeshRenderers.Count; i++)
            {
                _skinnedMeshRenderers[i].material = _transparentMaterials[i];
            }
        }

        var newAlpha = show ? 0f : 1f;
        var startTime = Time.time;

        while (Time.time < fadeDuration + startTime)
        {
            if (show)
            {
                newAlpha += Time.deltaTime / fadeDuration;
            }
            else if (newAlpha > alphaTarget)
            {
                newAlpha -= Time.deltaTime / fadeDuration;
            }

            foreach (var skinnedMeshRenderer in _skinnedMeshRenderers)
            {
                var color = skinnedMeshRenderer.material.color;
                skinnedMeshRenderer.material.color = new Color(color.r, color.g, color.b, newAlpha);
            }

            yield return null;
        }

        foreach (var skinnedMeshRenderer in _skinnedMeshRenderers)
        {
            var color = skinnedMeshRenderer.material.color;
            skinnedMeshRenderer.material.color = new Color(color.r, color.g, color.b, show ? 1f : alphaTarget);
        }

        if (show)
        {
            for (int i = 0; i < _skinnedMeshRenderers.Count; i++)
            {
                _skinnedMeshRenderers[i].material = _originalMaterials[i];
            }
        }

        _fadeMRsCoroutine = null;
    }

    public void FlashMeshRenderers(float flashDuration, float flashRate)
    {
        if (_fadeMRsCoroutine != null) StopCoroutine(_fadeMRsCoroutine);
        _fadeMRsCoroutine = StartCoroutine(FlashMeshRenderersCoroutine(flashDuration, flashRate));
    }

    private IEnumerator FlashMeshRenderersCoroutine(float flashDuration, float flashRate)
    {
        for (int i = 0; i < _skinnedMeshRenderers.Count; i++)
        {
            _skinnedMeshRenderers[i].material = _transparentMaterials[i];
        }

        float lowTarget = 0.25f;
        float highTarget = 0.95f;
        var newAlpha = highTarget;
        var startTime = Time.time;
        bool reachedLowTarget = false;

        while (Time.time < flashDuration + startTime)
        {
            if (!reachedLowTarget && newAlpha > lowTarget)
            {
                newAlpha -= flashRate * Time.deltaTime;
            }
            else
            {
                reachedLowTarget = true;
                newAlpha += flashRate * Time.deltaTime;

                if (newAlpha >= highTarget)
                {
                    reachedLowTarget = false;
                    // Debug.Log("Return to low target");
                }
            }

            foreach (var skinnedMeshRenderer in _skinnedMeshRenderers)
            {
                var color = skinnedMeshRenderer.material.color;
                skinnedMeshRenderer.material.color = new Color(color.r, color.g, color.b, newAlpha);
            }

            yield return null;
        }

        for (int i = 0; i < _skinnedMeshRenderers.Count; i++)
        {
            _skinnedMeshRenderers[i].material = _originalMaterials[i];
        }

        _fadeMRsCoroutine = null;
    }
}