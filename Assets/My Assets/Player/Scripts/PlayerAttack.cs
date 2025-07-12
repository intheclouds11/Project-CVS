using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float _attackBufferTime = 0.3f;
    [SerializeField]
    private float _attackRate = 0.3f;
    [SerializeField]
    private Knockback _attackKnockback;
    [SerializeField]
    private float _playerBasicKnockbackAmount = 2.5f;
    [SerializeField]
    private float _playerCritKnockbackAmount = 5f;
    [SerializeField]
    private float _critChargeTime = 0.4f;
    [SerializeField]
    private float _critGraceTime = 0.1f;

    [Header("Transforms")]
    [SerializeField]
    private SawBlade _sawBlade;
    [SerializeField]
    private Transform _sawBladeSpawnPoint;
    [SerializeField]
    private Transform _sawBladePlayerParent;

    [Header("Charging Visuals")]
    [SerializeField]
    private Slider _chargeMeter;
    [SerializeField]
    private Slider _critRange;
    [SerializeField]
    private Canvas _chargeMeterCanvas;
    [SerializeField]
    private Transform _chargeIndicator;
    [SerializeField]
    private float _chargeIndicatorDelay = 0.2f;
    [SerializeField]
    private Color _chargedColor;
    [SerializeField]
    private float _chargeIndicatorColorModifier = 1;
    [SerializeField]
    private ParticleSystem _critParticle;

    [Header("SFX")]
    [SerializeField]
    private AudioClip _chargingSFX;
    [SerializeField]
    private AudioClip _basicSFX;
    [SerializeField]
    private AudioClip _critSFX;

    public bool IsCharging => State == AttackState.Charging;
    public bool IsAttacking => State == AttackState.Attacking;
    public event Action EnteredCritThreshold; // todo: could let enemies anticipate the player more
    public event Action<Knockback> Attacked;

    public enum AttackState
    {
        Idle,
        Charging,
        Attacking
    }

    public AttackState State { get; private set; }

    private float _attackHeldTime;
    private bool _enteredCritThreshold;
    private bool _exceededCritThreshold;
    private float _attackCooldownTime;
    private float _attackBufferTimer;
    private bool _bufferedNextAttackDirection;
    private Vector3 _bufferedRotateDir;
    private Color _originalIndicatorColor;
    private Coroutine _chargeIndicatorCoroutine;

    private CanvasGroup _chargeMeterCanvasGroup;
    private AudioSource _chargingAudio;
    private InputManager _inputManager;
    private PlayerAnimator _playerAnimator;
    private PlayerController _player;
    private SkinnedMeshRenderer indicatorMR;


    private void Awake()
    {
        _inputManager = InputManager.Instance;
        _playerAnimator = GetComponentInChildren<PlayerAnimator>();
        _player = GetComponent<PlayerController>();

        State = AttackState.Idle;
        _chargeMeter.maxValue = _critChargeTime + _critGraceTime;
        _critRange.maxValue = _chargeMeter.maxValue;
        _critRange.value = _critGraceTime;
        _chargeMeterCanvasGroup = _chargeMeterCanvas.GetComponent<CanvasGroup>();
        _chargeMeterCanvasGroup.alpha = 0.25f;
        indicatorMR = _chargeIndicator.GetComponent<SkinnedMeshRenderer>();
        _originalIndicatorColor = indicatorMR.material.color;
    }

    private void Update()
    {
        if (PauseScreen.IsPaused) return;

        HandleCharging();
        HandleAttacking();

        if (!IsCharging && _chargeMeterCanvasGroup.alpha != 0f)
        {
            var newAlpha = _chargeMeterCanvasGroup.alpha - 0.5f * Time.deltaTime;
            _chargeMeterCanvasGroup.alpha = Mathf.Clamp(newAlpha, 0f, 1);
            _chargeMeter.value = 0f;
        }
    }

    private void HandleCharging()
    {
        if (_inputManager.AttackHeld && !IsAttacking && !_sawBlade.isActiveAndEnabled)
        {
            if (!IsCharging)
            {
                State = AttackState.Charging;
                if (_chargeIndicatorCoroutine != null) StopCoroutine(_chargeIndicatorCoroutine);
                _chargeIndicatorCoroutine = StartCoroutine(ChargeIndicatorCoroutine());

                _chargingAudio = AudioManager.Instance.PlaySound(transform, _chargingSFX, true, false, 0.7f);

                if (_player.IsDashing) _playerAnimator.SetIsDashing(false);
                _playerAnimator.SetReadyAttackTrigger();
                // Debug.Log($"StartCharge");
            }
            else
            {
                _player.SetAttackRotateDirection(_player.GetRotateDirection());

                if (_exceededCritThreshold)
                {
                    _attackHeldTime -= Time.deltaTime;
                    if (_chargeMeter.value > _critChargeTime)
                    {
                        _chargeMeter.value -= Time.deltaTime;
                    }
                }
                else
                {
                    _attackHeldTime += Time.deltaTime;
                    _chargeMeter.value += Time.deltaTime;
                }

                _chargeMeterCanvasGroup.alpha = 1f;

                if (!_enteredCritThreshold && WithinCritThreshold())
                {
                    OnEnteredCritThreshold();
                }
                else if (!_exceededCritThreshold && ExceededCritThreshold())
                {
                    _exceededCritThreshold = true;
                    indicatorMR.material.color = _originalIndicatorColor;
                }
            }
        }
    }
    
    private void HandleAttacking()
    {
        if (_inputManager.AttackWasReleased)
        {
            _attackBufferTimer = _attackBufferTime;
        }

        if (_attackBufferTimer > 0)
        {
            if (!IsAttacking && !_player.IsDashing && !_sawBlade.isActiveAndEnabled && !_sawBlade.IsReturning)
            {
                StartCoroutine(Attack());
            }
        }
        else if (_attackBufferTimer <= 0)
        {
            _attackBufferTimer -= Time.deltaTime;
        }

        if (IsAttacking && _inputManager.AttackWasPressed)
        {
            _bufferedRotateDir = _player.GetRotateDirection();
            _bufferedNextAttackDirection = true;
            // Debug.Log($"_bufferedNextAttackDirection: {_bufferedRotateDir}");
        }
    }

    private IEnumerator ChargeIndicatorCoroutine()
    {
        yield return new WaitForSeconds(_chargeIndicatorDelay);

        while (IsCharging && !_enteredCritThreshold)
        {
            var newColor = Color.Lerp(indicatorMR.material.color, _chargedColor, _chargeIndicatorColorModifier * Time.deltaTime);
            indicatorMR.material.color = newColor;
            yield return null;
        }
    }

    private void OnEnteredCritThreshold()
    {
        _enteredCritThreshold = true;
        EnteredCritThreshold?.Invoke();
    }

    private IEnumerator Attack()
    {
        // Debug.Log($"StartATTACK");
        if (!IsCharging && _bufferedNextAttackDirection)
        {
            _player.SetAttackRotateDirection(_bufferedRotateDir);
            _bufferedNextAttackDirection = false;
            // Debug.Log($"SetAttackRotateDirection: {_bufferedRotateDir}");
        }

        _attackBufferTimer = 0f;
        State = AttackState.Attacking;

        indicatorMR.material.color = _originalIndicatorColor;
        if (_chargingAudio) _chargingAudio.Stop();
        if (_player.IsDashing) _playerAnimator.SetIsDashing(false);
        _playerAnimator.SetAttackTrigger();

        _wasCritAttack = false;
        if (WithinCritThreshold())
        {
            _inputManager.Vibrate(0.4f, 1f, 0.2f);
            _critParticle.Play();
            AudioManager.Instance.PlaySound(transform, _critSFX, true, false, 1f, 1.3f);
            _wasCritAttack = true;
        }
        else
        {
            var pitch = Random.Range(0.8f, 0.9f);
            AudioManager.Instance.PlaySound(transform, _basicSFX, true, false, 0.5f, pitch);
        }

        _lastChargeAmount = _chargeMeter.value / _chargeMeter.maxValue;
        _attackKnockback.KnockbackAmount = _wasCritAttack ? _playerCritKnockbackAmount : _playerBasicKnockbackAmount;
        Attacked?.Invoke(_attackKnockback);

        yield return new WaitForSeconds(_attackRate);

        _attackHeldTime = 0f;
        _enteredCritThreshold = false;
        _exceededCritThreshold = false;
        State = AttackState.Idle;
    }

    private bool _wasCritAttack;
    private float _lastChargeAmount;

    public void ThrowSawBlade()
    {
        _sawBlade.OnAttack(_sawBladeSpawnPoint, _lastChargeAmount, _wasCritAttack);
    }

    private bool WithinCritThreshold()
    {
        return _attackHeldTime >= _critChargeTime && _attackHeldTime <= _critChargeTime + _critGraceTime;
    }

    private bool ExceededCritThreshold()
    {
        return _attackHeldTime >= _critChargeTime + _critGraceTime;
    }

    public void ToggleChargeHUD()
    {
        _chargeMeterCanvas.enabled = !_chargeMeterCanvas.enabled;
    }

    public void OnDied()
    {
        enabled = false;
        State = AttackState.Idle;
        _attackBufferTimer = 0f;
        _chargeMeterCanvasGroup.alpha = 0f;
        _chargeMeter.value = 0f;
    }

    public void OnRespawn()
    {
        enabled = true;
    }
}