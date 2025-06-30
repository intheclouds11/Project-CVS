using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SawBlade : MonoBehaviour
{
    [SerializeField]
    private float _baseDamage = 20f;
    [SerializeField]
    private float _critDamage = 120f;
    [SerializeField]
    private float _shortRangeImpulse = 1f;
    [SerializeField]
    private float _longRangeImpulse = 5f;
    [field: SerializeField, Tooltip("Percent charge required for long range attack")]
    private float _longRangeChargeThreshold = 0.25f;
    [SerializeField]
    private float _longRangeReturnTime = 0.35f;
    [SerializeField]
    private float _shortRangeReturnTime = 0.15f;
    [SerializeField]
    private float _startReturnPlayerDistance = 5f;

    [Header("FX")]
    [SerializeField]
    private AudioClip _swipeSFX;
    [SerializeField]
    private AudioClip _bladeSpinLoopSFX;
    [SerializeField]
    private AudioClip _impactSFX;
    [SerializeField]
    private GameObject _impactVfx;

    public bool IsCritAttack { get; private set; }
    public static event Action EnemyHit;
    public event Action ReturnedToPlayer;

    private bool _longRangeAttack;
    private float _finalDamage;
    private float _finalImpulseForce;
    private float _finalStartReturnTime;
    private int _loopAudioSourceIndex = -1;
    private float _spawnTime;
    private bool _isReturning;
    private Rigidbody _rb;
    private PlayerController _player;
    private CinemachineImpulseSource _impulseSource;
    private bool _hasInitialized;


    private void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _player = GameManager.Instance.Player1;
        _rb = GetComponent<Rigidbody>();
        _finalStartReturnTime = _longRangeReturnTime;
        _hasInitialized = true;
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        Vector3 forceToAdd = transform.forward * _finalImpulseForce;
        _rb.AddForce(forceToAdd, ForceMode.Impulse);

        if (_longRangeAttack)
        {
            PlayLoopAudio();
        }
        else
        {
            var pitch = Random.Range(0.9f, 1.1f);
            AudioManager.Instance.PlaySound(transform, _swipeSFX, true, false, 1f, pitch);
        }
    }

    private void Update()
    {
        if (_longRangeAttack)
        {
            transform.rotation *= Quaternion.AngleAxis(transform.eulerAngles.y + Time.deltaTime * 360, Vector3.up);
        }

        if (_isReturning) return;

        if (Time.time >= _spawnTime + _finalStartReturnTime)
        {
            if (IsCritAttack)
            {
                _rb.linearVelocity = Vector3.zero;
            }
            else
            {
                ReturnToPlayer();
            }
        }

        if (Vector3.Distance(transform.position, _player.transform.position) >= _startReturnPlayerDistance)
        {
            ReturnToPlayer();
        }
    }

    public void ReturnToPlayer()
    {
        _isReturning = true;
        var dir = (_player.transform.position - transform.position).normalized;
        Vector3 forceToAdd = new Vector3(dir.x, 0f, dir.z) * (_longRangeImpulse * 4f);
        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(forceToAdd, ForceMode.Impulse);
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void PlayLoopAudio()
    {
        AudioManager.Instance.StopSound(_loopAudioSourceIndex);
        _loopAudioSourceIndex = AudioManager.Instance.PlaySound(transform, _bladeSpinLoopSFX, true, true, 0.6f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"Hit: {other.transform.name}", other.transform);

        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            var enemyHit = other.GetComponentInParent<BaseEnemy>();
            if (enemyHit)
            {
                if (IsCritAttack) _impulseSource.GenerateImpulse();
                enemyHit.Health.TakeDamage(_finalDamage);
                EnemyHit?.Invoke();
            }

            PlayEffects();
        }

        OnReturnedToPlayer();
    }

    private void OnReturnedToPlayer()
    {
        ReturnedToPlayer?.Invoke();
        ResetToDefaultState();
    }
    
    public void ResetToDefaultState()
    {
        if (!_hasInitialized) return;
        
        AudioManager.Instance.StopSound(_loopAudioSourceIndex);
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Weapon");
        _rb.linearVelocity = Vector3.zero;
        _isReturning = false;
        transform.parent = _player.transform;
        gameObject.SetActive(false);
    }

    private void PlayEffects()
    {
        Instantiate(_impactVfx, transform.position, Quaternion.LookRotation(-transform.forward));

        AudioManager.Instance.PlaySound(transform, _impactSFX, true, false, 0.4f);
    }

    public void OnAttack(float chargeAmount, bool crit)
    {
        _longRangeAttack = chargeAmount >= _longRangeChargeThreshold;
        _finalImpulseForce = _longRangeAttack ? _longRangeImpulse * chargeAmount : _shortRangeImpulse;
        _finalStartReturnTime = _longRangeAttack ? _longRangeReturnTime : _shortRangeReturnTime;

        _finalDamage = crit ? _critDamage : Mathf.Clamp(_baseDamage * chargeAmount * 2f, _baseDamage * 0.5f, _baseDamage);
        IsCritAttack = crit;

        Debug.Log($"_finalDamage: {_finalDamage}");
    }
}