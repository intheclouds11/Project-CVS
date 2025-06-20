using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAttack : MonoBehaviour
{
    [field: SerializeField]
    public float PlayerBasicKnockbackDistance { get; private set; } = 0.2f;
    [field: SerializeField]
    public float PlayerCritKnockbackDistance { get; private set; } = 0.8f;
    [SerializeField]
    private float _critAttackChargeTime = 0.6f;
    [SerializeField]
    private float _critAttackGraceTime = 0.1f;
    [SerializeField]
    private AudioClip _chargingAttackSFX;
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
    
    public event Action<bool> Attacked;
    public bool AttackInputHeld { get; private set; }
    private PlayerController _player;
    private InputManager _inputManager;
    private float _attackHeldTime;


    private void Awake()
    {
        _inputManager = InputManager.Instance;
        _player = FindAnyObjectByType<PlayerController>();
        _chargeMeter.maxValue = _critAttackChargeTime + _critAttackGraceTime;
        _critRange.maxValue = _chargeMeter.maxValue;
        _critRange.value = _critAttackGraceTime;
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
        else if (_inputManager.AttackWasReleased)
        {
            AttackInputHeld = false;
            Attack();
        }
    }

    private void HandleChargeAttack()
    {
        if (AttackInputHeld)
        {
            _chargeMeter.value += Time.deltaTime;
            _attackHeldTime += Time.deltaTime;
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
        if (_attackHeldTime >= _critAttackChargeTime && _attackHeldTime <= _critAttackChargeTime + _critAttackGraceTime)
        {
            Debug.Log($"Crit attack!");
            critAttack = true;
        }
        else
        {
            Debug.Log($"Basic attack..");
        }

        _attackHeldTime = 0f;
        Instantiate(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
        Attacked?.Invoke(critAttack);
    }
}