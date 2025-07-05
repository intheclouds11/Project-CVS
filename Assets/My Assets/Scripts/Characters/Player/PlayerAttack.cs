using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float _attackBufferTime = 0.3f;
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

    public event Action<float> Attacked;
    public bool AttackIsHeld { get; private set; }
    public event Action EnteredCritThreshold; // todo: could let enemies anticipate the player more

    private bool _sawBladeReturned = true;
    private float _attackHeldTime;
    private bool _enteredCritThreshold;
    private bool _exceededCritThreshold;
    private float _attackCooldownTime;
    private float _attackBufferTimer;
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
        _sawBlade.ReturnedToPlayer += OnSawBladeReturnedToPlayer;

        _chargeMeter.maxValue = _critChargeTime + _critGraceTime;
        _critRange.maxValue = _chargeMeter.maxValue;
        _critRange.value = _critGraceTime;
        _chargeMeterCanvasGroup = _chargeMeterCanvas.GetComponent<CanvasGroup>();
        _chargeMeterCanvasGroup.alpha = 0.25f;
        indicatorMR = _chargeIndicator.GetComponent<SkinnedMeshRenderer>();
        _originalIndicatorColor = indicatorMR.material.color;
    }

    private void OnSawBladeReturnedToPlayer()
    {
        _sawBladeReturned = true;
    }

    private void Update()
    {
        if (PauseScreen.Instance.gameObject.activeSelf) return;

        CheckInput();
        HandleChargeAttack();
    }

    private IEnumerator ChargeIndicatorCoroutine()
    {
        yield return new WaitForSeconds(_chargeIndicatorDelay);

        while (AttackIsHeld && !_enteredCritThreshold)
        {
            var newColor = Color.Lerp(indicatorMR.material.color, _chargedColor, _chargeIndicatorColorModifier * Time.deltaTime);
            indicatorMR.material.color = newColor;
            yield return null;
        }
    }

    private void CheckInput()
    {
        if (_inputManager.AttackWasPressed)
        {
            _attackBufferTimer = _attackBufferTime;
        }

        if (_attackBufferTimer > 0)
        {
            if (_inputManager.AttackHeld)
            {
                if (!AttackIsHeld)
                {
                    // If crit stalling and attack held, return SawBlade to player
                    if (_sawBlade.gameObject.activeSelf && _sawBlade.IsCritAttack)
                    {
                        _sawBlade.ReturnToPlayer();
                        return;
                    }

                    if (_sawBladeReturned)
                    {
                        AttackIsHeld = true;
                        if (_chargeIndicatorCoroutine != null) StopCoroutine(_chargeIndicatorCoroutine);
                        _chargeIndicatorCoroutine = StartCoroutine(ChargeIndicatorCoroutine());
                        _chargingAudio = AudioManager.Instance.PlaySound(transform, _chargingSFX, true, false, 0.7f);
                        _playerAnimator.SetReadyAttackTrigger();
                    }
                }
                else if (!_enteredCritThreshold && WithinCritThreshold())
                {
                    OnEnteredCritThreshold();
                }
            }

            if (AttackIsHeld)
            {
                if (!_exceededCritThreshold && ExceededCritThreshold())
                {
                    _exceededCritThreshold = true;
                    indicatorMR.material.color = _originalIndicatorColor;
                }

                if (_inputManager.AttackWasReleased && !_player.IsDashing)
                {
                    AttackIsHeld = false;
                    Attack();
                }
            }
        }
        else
        {
            _attackBufferTimer -= Time.deltaTime;
        }
    }

    private void OnEnteredCritThreshold()
    {
        _enteredCritThreshold = true;
        EnteredCritThreshold?.Invoke();
    }

    private void HandleChargeAttack()
    {
        if (AttackIsHeld)
        {
            _attackHeldTime += Time.deltaTime;
            _chargeMeter.value += Time.deltaTime;
            _chargeMeterCanvasGroup.alpha = 1f;
        }
        else
        {
            var newAlpha = _chargeMeterCanvasGroup.alpha - 0.5f * Time.deltaTime;
            _chargeMeterCanvasGroup.alpha = Mathf.Clamp(newAlpha, 0f, 1);
            _chargeMeter.value = 0f;
        }
    }

    private void Attack()
    {
        _attackBufferTimer = 0;
        _sawBladeReturned = false;
        // _chargeIndicator.transform.localScale = _originalChargeIndicatorScale;
        indicatorMR.material.color = _originalIndicatorColor;
        _chargingAudio.Stop();
        _playerAnimator.SetAttackTrigger();

        bool critAttack = false;
        if (WithinCritThreshold())
        {
            _inputManager.Vibrate(0.4f, 1f, 0.2f);
            _critParticle.Play();
            AudioManager.Instance.PlaySound(transform, _critSFX, true, false, 2f, 1.2f);
            critAttack = true;
        }
        else
        {
            AudioManager.Instance.PlaySound(transform, _basicSFX, true, false, 1.5f, 0.9f);
        }

        float _lastChargeAmount = _chargeMeter.value / _chargeMeter.maxValue;
        _sawBlade.OnAttack(_lastChargeAmount, critAttack);

        _sawBlade.transform.parent = null;
        _sawBlade.transform.position = _sawBladeSpawnPoint.position;
        _sawBlade.transform.rotation = _sawBladeSpawnPoint.rotation;
        _sawBlade.gameObject.SetActive(true);

        _attackHeldTime = 0f;
        _enteredCritThreshold = false;
        _exceededCritThreshold = false;
        Attacked?.Invoke(critAttack ? _playerCritKnockbackAmount : _playerBasicKnockbackAmount);
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
        _sawBladeReturned = true;
        AttackIsHeld = false;
        _attackBufferTimer = 0f;
        _chargeMeterCanvasGroup.alpha = 0f;
        _chargeMeter.value = 0f;
    }

    public void OnRespawn()
    {
        enabled = true;
    }
}