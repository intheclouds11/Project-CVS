using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField]
    private float _attackBufferTime = 0.3f;
    [field: SerializeField] public float PlayerBasicKnockbackAmount { get; private set; } = 0.2f;
    [field: SerializeField] public float PlayerCritKnockbackAmount { get; private set; } = 0.8f;
    [SerializeField]
    private float _critChargeTime = 0.4f;
    [SerializeField]
    private float _critGraceTime = 0.1f;
    [SerializeField]
    private Slider _chargeMeter;
    [SerializeField]
    private Canvas _chargeMeterCanvas;
    [SerializeField]
    private GameObject _chargeIndicator;
    [SerializeField]
    private Color _chargeIndicatorNewColor;
    [SerializeField]
    private float _chargeIndicatorColorModifier = 1;
    private Color _originalIndicatorColor;
    [SerializeField]
    private ParticleSystem _critParticle;
    [SerializeField]
    private Slider _critRange;
    [SerializeField]
    private Transform _sawBladeSpawnPoint;
    [SerializeField]
    private Transform _sawBladePlayerParent;
    [SerializeField]
    private SawBlade _sawBlade;
    [SerializeField]
    private AudioClip _chargingSFX;
    [SerializeField]
    private AudioClip _basicSFX;
    [SerializeField]
    private AudioClip _critSFX;

    private float _attackHeldTime;
    private CanvasGroup _chargeMeterCanvasGroup;
    private bool _enteredCritThreshold;
    private bool _exceededCritThreshold;
    private float _attackCooldownTime;
    private float _attackBufferTimer;
    private AudioSource _chargingAudio;
    private InputManager _inputManager;
    private PlayerAnimator _playerAnimator;
    private PlayerController _player;
    private SkinnedMeshRenderer indicatorMR;
    private bool _sawBladeReturned = true;

    public event Action<bool> Attacked;
    public bool AttackIsHeld { get; private set; }
    public event Action EnteredCritThreshold; // todo: could let enemies anticipate the player more


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

        if (AttackIsHeld && !_enteredCritThreshold)
        {
            var newColor = Color.Lerp(indicatorMR.material.color, _chargeIndicatorNewColor,
                _chargeIndicatorColorModifier * Time.deltaTime);
            indicatorMR.material.color = newColor;
        }

        CheckInput();
        HandleChargeAttack();
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
        Attacked?.Invoke(critAttack);
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