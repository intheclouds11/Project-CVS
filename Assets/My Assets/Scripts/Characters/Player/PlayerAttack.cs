using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerAttack : MonoBehaviour
{
    [field: SerializeField]
    public float PlayerBasicKnockbackDistance { get; private set; } = 0.2f;
    [field: SerializeField]
    public float PlayerCritKnockbackDistance { get; private set; } = 0.8f;
    [SerializeField]
    private float _critChargeTime = 0.4f;
    [SerializeField]
    private float _critGraceTime = 0.1f;
    [SerializeField]
    private AudioClip _chargingSFX;
    [SerializeField]
    private Slider _chargeMeter;
    [SerializeField]
    private Slider _critRange;
    [SerializeField]
    private CanvasGroup _chargeMeterCanvasGroup;
    [SerializeField]
    private Transform _projectileSpawnPoint;
    [SerializeField]
    private GameObject _projectilePrefab;
    [SerializeField]
    private AudioClip _basicSFX;
    [SerializeField]
    private AudioClip _critSFX;
    
    public event Action<bool> Attacked;
    public bool AttackInputHeld { get; private set; }
    private PlayerController _player;
    private InputManager _inputManager;
    private float _attackHeldTime;


    private void Awake()
    {
        _inputManager = InputManager.Instance;
        _player = FindAnyObjectByType<PlayerController>();
        _chargeMeter.maxValue = _critChargeTime + _critGraceTime;
        _critRange.maxValue = _chargeMeter.maxValue;
        _critRange.value = _critGraceTime;
        _chargeMeterCanvasGroup.alpha = 0.25f;
    }

    private void Update()
    {
        if (PauseMenu.Instance.gameObject.activeSelf) return;

        CheckInput();
        HandleChargeAttack();
    }
    
    private void CheckInput()
    {
        if (_inputManager.AttackWasPressed)
        {
            AttackInputHeld = true;
        }
        else if (AttackInputHeld && _inputManager.AttackWasReleased)
        {
            AttackInputHeld = false;
            Attack();
        }
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
            _chargeMeter.value = 0f;
            _chargeMeterCanvasGroup.alpha = 0.25f;
        }
    }

    private void Attack()
    {
        bool critAttack = false;
        if (_attackHeldTime >= _critChargeTime && _attackHeldTime <= _critChargeTime + _critGraceTime)
        {
            // AudioManager.Instance.PlaySound(transform, _critSFX);
            critAttack = true;
        }
        else
        {
            // AudioManager.Instance.PlaySound(transform, _basicSFX);
        }

        _attackHeldTime = 0f;
        Instantiate(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
        Attacked?.Invoke(critAttack);
    }
}