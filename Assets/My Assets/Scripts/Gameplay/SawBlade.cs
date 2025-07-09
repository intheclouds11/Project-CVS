using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[Serializable]
public class Knockback
{
    [SerializeField]
    public bool ApplyKnockback = true;
    [SerializeField]
    public float KnockbackAmount = 1f;
    [SerializeField]
    public float KnockbackDuration = 0.25f;
    [SerializeField]
    public float StunDuration = 0.25f;
}
public class SawBlade : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField]
    private Knockback _knockback;
    [SerializeField]
    private int _baseDamage = 70;
    [SerializeField]
    private int _critDamage = 120;
    
    [Header("Movement")]
    [SerializeField]
    private float _shortRangeImpulse = 1f;
    [SerializeField]
    private float _longRangeImpulse = 5f;
    [SerializeField]
    private float _critImpulse = 8f;
    [SerializeField]
    private float _returnSpeed = 20f;
    [field: SerializeField, Tooltip("Percent charge required for long range attack")]
    private float _longRangeChargeThreshold = 0.25f;
    [SerializeField]
    private float _shortRangeReturnTime = 0.2f;
    [SerializeField]
    private float _longRangeReturnTime = 0.4f;
    [SerializeField]
    private float _critReturnTime = 0.2f;

    [Header("FX")]
    [SerializeField]
    private AudioClip _swipeSFX;
    [SerializeField]
    private AudioClip _bladeSpinLoopSFX;
    [SerializeField]
    private AudioClip _returnedSFX;
    [SerializeField]
    private float _returnedSFXVolume;
    [SerializeField]
    private AudioClip _impactSFX;
    [SerializeField]
    private GameObject _impactVfx;

    public bool IsReturning { get; private set; }
    public bool IsLongRangeAttack { get; private set; }
    public bool IsCritAttack { get; private set; }
    /// bool: wasCritAttack
    public static event Action<bool> HitEnemy;

    private int _finalDamage;
    private float _finalImpulseForce;
    private float _finalStartReturnTime;
    private AudioSource _loopAudio;
    private float _spawnTime;
    private Rigidbody _rb;
    private PlayerController _player;
    private CinemachineImpulseSource _impulseSource;
    private bool _hasInitialized;


    private void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _player = GameManager.Instance.Player1;
        _player.Health.Died += OnDied;
        _rb = GetComponent<Rigidbody>();
        _finalStartReturnTime = _longRangeReturnTime;
        _hasInitialized = true;
    }

    private void OnDied(GameObject obj)
    {
        ResetToDefaultState();
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        Vector3 forceToAdd = transform.forward * _finalImpulseForce;
        _rb.AddForce(forceToAdd, ForceMode.Impulse);

        if (IsLongRangeAttack)
        {
            PlayLoopAudio();
        }
        else
        {
            var pitch = Random.Range(0.9f, 1.1f);
            AudioManager.Instance.PlaySound(transform, _swipeSFX, true, false, 0.8f, pitch);
        }
    }

    private void Update()
    {
        if (IsLongRangeAttack)
        {
            transform.rotation *= Quaternion.AngleAxis(transform.eulerAngles.y + Time.deltaTime * 360, Vector3.up);
        }

        if (!IsReturning)
        {
            if (Time.time >= _spawnTime + _finalStartReturnTime)
            {
                ReturnToPlayer();
            }
        }
        else
        {
            Vector3 newPos = Vector3.MoveTowards(_rb.position, _player.LookAt.position, _returnSpeed * Time.deltaTime);
            _rb.MovePosition(newPos);
        }
    }

    public void ReturnToPlayer()
    {
        IsReturning = true;
        _rb.linearVelocity = Vector3.zero;
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void PlayLoopAudio()
    {
        if (_loopAudio) _loopAudio.Stop();
        _loopAudio = AudioManager.Instance.PlaySound(transform, _bladeSpinLoopSFX, true, true, 0.7f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"Hit: {other.transform.name}", other.transform);

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            OnReturnedToPlayer();
        }
        else
        {
            var enemyHit = other.GetComponentInParent<BaseEnemy>();
            if (enemyHit)
            {
                if (IsCritAttack)
                {
                    _impulseSource.GenerateImpulse();
                    _player.Health.RecoverHP(1);
                }

                var knockbackDir = _player.RotationTransform.forward;
                knockbackDir.y = 0f;
                enemyHit.Health.TakeDamage(_finalDamage, knockbackDir, _knockback);
                HitEnemy?.Invoke(IsCritAttack);
            }

            ReturnToPlayer();
            PlayEffects();
        }
    }

    private void OnReturnedToPlayer()
    {
        if (IsCritAttack)
        {
            AudioManager.Instance.PlaySound(transform, _returnedSFX, true, false, _returnedSFXVolume);
        }

        ResetToDefaultState();
    }

    public void ResetToDefaultState()
    {
        if (!_hasInitialized) return;

        if (_loopAudio) _loopAudio.Stop();
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Weapon");
        _rb.linearVelocity = Vector3.zero;
        IsReturning = false;
        transform.parent = _player.transform;
        gameObject.SetActive(false);
    }

    private void PlayEffects()
    {
        Instantiate(_impactVfx, transform.position, Quaternion.LookRotation(-transform.forward));

        AudioManager.Instance.PlaySound(transform, _impactSFX, true, false, 0.4f);
    }

    public void OnAttack(Transform spawnPoint, float chargeAmount, bool crit)
    {
        IsCritAttack = crit;
        IsLongRangeAttack = chargeAmount >= _longRangeChargeThreshold;
        _finalImpulseForce = IsCritAttack ? _critImpulse : IsLongRangeAttack ? _longRangeImpulse : _shortRangeImpulse;
        _finalStartReturnTime = IsCritAttack ? _critReturnTime : IsLongRangeAttack ? _longRangeReturnTime : _shortRangeReturnTime;

        _finalDamage = crit ? _critDamage : (int) Mathf.Clamp(_baseDamage * chargeAmount * 2f, _baseDamage * 0.5f, _baseDamage);

        // Debug.Log($"_finalDamage: {_finalDamage}");

        transform.parent = null;
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        gameObject.SetActive(true);
    }
}