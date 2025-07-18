using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    protected int _baseDamage = 1;
    [SerializeField]
    protected int _baseDeflectedDamage = 100;
    [SerializeField]
    protected int _critDeflectedDamage = 200;
    [SerializeField]
    protected float _baseDeflectSpeed = 10f;
    [SerializeField]
    protected float _hitByDeflectedSpeed = 15f;
    [SerializeField]
    protected Knockback _knockback;
    [SerializeField]
    protected AudioClip _deflectedSFX;
    [SerializeField]
    protected AudioClip _hitByDeflectedSFX;
    [SerializeField]
    protected AudioClip _critDeflectedSFX;
    [SerializeField]
    protected AudioClip _impactSFX;
    [SerializeField]
    protected Transform _impactVFXSpawnPoint;
    [SerializeField]
    protected GameObject _impactVFX;

    public Rigidbody Rb { get; private set; }
    protected string _poolKey;
    protected float _distToPlayer;
    protected bool _isCritDeflected;
    protected MultiProjectilePool _pool;
    protected PlayerController _player;
    protected Animator _animator;


    protected void Awake()
    {
        tag = "EnemyProjectile";
        _animator = GetComponent<Animator>();
        Rb = GetComponent<Rigidbody>();
    }

    protected void OnEnable()
    {
        if (!_player)
        {
            PlayerSpawnManager.PlayerSpawned += OnPlayerSpawned;
        }
    }

    protected void OnDisable()
    {
        if (_player)
        {
            PlayerSpawnManager.PlayerSpawned -= OnPlayerSpawned;
        }
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, _player.transform.position) > 30)
        {
            ReturnToPool(null);
        }
    }

    public void Init(MultiProjectilePool pool, string poolKey)
    {
        _pool = pool;
        _poolKey = poolKey;
    }

    public void ReturnToPool(GameObject hitObj = null, bool deflected = false)
    {
        AudioManager.Instance.PlaySound(transform, _impactSFX, true, false, 1f, 1f);
        var particleDirection = (FindAnyObjectByType<FirstBossEncounter>().transform.position - transform.position).normalized;
        particleDirection = deflected ? -particleDirection : particleDirection;
        Instantiate(_impactVFX, _impactVFXSpawnPoint.position, Quaternion.LookRotation(particleDirection));
        
        tag = "EnemyProjectile";
        Rb.linearVelocity = Vector3.zero;
        _isCritDeflected = false;
        gameObject.SetActive(false);
        transform.localPosition = Vector3.zero;
        _pool.Return(_poolKey, gameObject);
        OnReturnToPool();
    }

    protected virtual void OnReturnToPool()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (CompareTag("EnemyProjectile") && other.CompareTag("PlayerWeapon"))
        {
            tag = "Deflected";
            var sawblade = other.GetComponentInParent<SawBlade>();
            _isCritDeflected = sawblade.IsCritAttack;

            if (_isCritDeflected)
            {
                AudioManager.Instance.PlaySound(transform, _critDeflectedSFX, true, false, 1f, 1f);
            }
            else
            {
                var pitch = Random.Range(0.9f, 1.1f);
                AudioManager.Instance.PlaySound(transform, _deflectedSFX, true, false, 0.5f, pitch);
            }

            transform.rotation = Quaternion.LookRotation(_player.RotationTransform.forward);
            var deflectSpeed = _baseDeflectSpeed * sawblade.DamageEnemyKnockback.KnockbackAmount * (_isCritDeflected ? 2f : 1f);
            Rb.linearVelocity = _player.RotationTransform.forward * deflectSpeed;
        }
        else
        {
            var playerHit = other.GetComponent<PlayerController>();
            if (playerHit)
            {
                var knockBackDir = (playerHit.transform.position - transform.position).normalized;
                playerHit.Health.TakeDamage(_baseDamage, knockBackDir, _knockback);
                ReturnToPool(other.gameObject);
            }
            else if (CompareTag("Deflected"))
            {
                var enemyHit = other.GetComponentInParent<BaseEnemy>();
                var projHit = other.GetComponent<Projectile>();
                var bossHit = other.GetComponentInParent<FirstBossEncounter>();
                var deflectedDamage = _isCritDeflected ? _critDeflectedDamage : _baseDeflectedDamage;

                if (enemyHit)
                {
                    enemyHit.Health.TakeDamage(deflectedDamage, Vector3.zero, null);
                    ReturnToPool(other.gameObject, true);
                }
                else if (projHit)
                {
                    // Debug.Log("Deflected Projectile hit projectile");
                    projHit.tag = "Deflected";
                    projHit.transform.rotation = Quaternion.LookRotation(transform.forward);
                    projHit.Rb.linearVelocity = transform.forward * _hitByDeflectedSpeed;
                    Rb.linearVelocity = projHit.transform.forward * _baseDeflectSpeed * 0.75f;
                    var pitch = Random.Range(1.2f, 1.3f);
                    AudioManager.Instance.PlaySound(transform, _hitByDeflectedSFX, true, false, 0.7f, pitch);
                }
                else if (bossHit)
                {
                    bossHit.Health.TakeDamage(deflectedDamage, Vector3.zero, null);
                    ReturnToPool(other.gameObject, true);
                }
            }
            else if (other.gameObject.layer != LayerMask.NameToLayer("Enemy"))
            {
                ReturnToPool(other.gameObject);
            }
        }
    }

    protected virtual void OnPlayerSpawned(PlayerController player)
    {
        _player = player;
    }
}