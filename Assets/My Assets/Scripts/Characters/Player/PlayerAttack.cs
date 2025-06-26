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
    private ParticleSystem _chargeParticle;
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

    private float _lastChargeAmount;
    private float _attackHeldTime;
    private CanvasGroup _chargeMeterCanvasGroup;
    private bool _enteredCritThreshold;
    private float _attackCooldownTime;
    private int _chargingSFXIndex;
    private InputManager _inputManager;

    public event Action<bool> Attacked;
    public bool AttackInputHeld { get; private set; }


    private void Awake()
    {
        _inputManager = InputManager.Instance;

        _chargeMeter.maxValue = _critChargeTime + _critGraceTime;
        _critRange.maxValue = _chargeMeter.maxValue;
        _critRange.value = _critGraceTime;
        _chargeMeterCanvasGroup = _chargeMeterCanvas.GetComponent<CanvasGroup>();
        _chargeMeterCanvasGroup.alpha = 0.25f;
    }

    private void Update()
    {
        if (PauseScreen.Instance.gameObject.activeSelf) return;

        CheckInput();
        HandleChargeAttack();
    }

    private void CheckInput()
    {
        if (_attackCooldownTime > 0)
        {
            _attackCooldownTime -= Time.deltaTime;
        }
        else
        {
            if (_inputManager.AttackHeld)
            {
                if (!AttackInputHeld)
                {
                    // If crit stalling and attack held, return SawBlade to player
                    if (_sawBlade.gameObject.activeSelf && _sawBlade.IsCritAttack)
                    {
                        _sawBlade.ReturnToPlayer();
                        return;
                    }

                    AttackInputHeld = true;
                    _chargingSFXIndex = AudioManager.Instance.PlaySound(transform, _chargingSFX, true, false, 0.7f);
                }
                else if (!_enteredCritThreshold && WithinCritThreshold())
                {
                    _enteredCritThreshold = true;
                    _chargeParticle.Play();
                    OnEnteredCritThreshold();
                }
            }

            if (AttackInputHeld && _inputManager.AttackWasReleased)
            {
                AttackInputHeld = false;
                _enteredCritThreshold = false;
                _chargeParticle.Stop();
                Attack();
            }
        }
    }

    private void OnEnteredCritThreshold()
    {
        // Debug.Log($"Entered crit threshold");
        // todo: visual indicator (flash like PO)
    }

    private void HandleChargeAttack()
    {
        if (AttackInputHeld)
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
        AudioManager.Instance.StopSound(_chargingSFXIndex);
        _attackCooldownTime = _attackBufferTime;
        bool critAttack = false;
        if (WithinCritThreshold())
        {
            // _inputManager.Vibrate(0.8f, 0.8f, 0.25f);
            _critParticle.Play();
            AudioManager.Instance.PlaySound(transform, _critSFX, true, false, 2f, 1.2f);
            critAttack = true;
        }
        else
        {
            AudioManager.Instance.PlaySound(transform, _basicSFX, true, false, 1.5f, 0.9f);
        }

        _lastChargeAmount = (_chargeMeter.value / _chargeMeter.maxValue);
        _sawBlade.OnAttack(_lastChargeAmount, critAttack);

        _sawBlade.transform.parent = null;
        _sawBlade.transform.position = _sawBladeSpawnPoint.position;
        _sawBlade.transform.rotation = _sawBladeSpawnPoint.rotation;
        _sawBlade.gameObject.SetActive(true);

        Attacked?.Invoke(critAttack);
        _attackHeldTime = 0f;
    }

    private bool WithinCritThreshold()
    {
        return _attackHeldTime >= _critChargeTime && _attackHeldTime <= _critChargeTime + _critGraceTime;
    }

    public void ToggleChargeHUD()
    {
        _chargeMeterCanvas.enabled = !_chargeMeterCanvas.enabled;
    }
    
    public void OnDied()
    {
        enabled = false;
        AttackInputHeld = false;
        _chargeMeterCanvasGroup.alpha = 0f;
        _chargeMeter.value = 0f;
        _sawBlade.ResetToDefaultState();
    }

    public void OnRespawn()
    {
        enabled = true;
    }
}