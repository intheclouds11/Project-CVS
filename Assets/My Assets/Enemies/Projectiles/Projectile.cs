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
    protected Knockback _knockback;
    [SerializeField]
    protected AudioClip _deflectedSFX;
    [SerializeField]
    protected AudioClip _critDeflectedSFX;
    [SerializeField]
    protected AudioClip _impactSFX;
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
        gameObject.tag = "EnemyProjectile";
        _animator = GetComponent<Animator>();
        Rb = GetComponent<Rigidbody>();
    }

    protected void OnEnable()
    {
        if (!_player)
        {
            PlayerSpawnPoint.PlayerSpawned += OnPlayerSpawned;
        }
    }

    protected void OnDisable()
    {
        if (_player)
        {
            PlayerSpawnPoint.PlayerSpawned -= OnPlayerSpawned;
        }
    }

    public void Init(MultiProjectilePool pool, string poolKey)
    {
        _pool = pool;
        _poolKey = poolKey;
    }

    public void ReturnToPool()
    {
        gameObject.tag = "EnemyProjectile";
        Rb.linearVelocity = Vector3.zero;
        _isCritDeflected = false;
        _pool.Return(_poolKey, gameObject);
        OnReturnToPool();
    }

    protected virtual void OnReturnToPool()
    {
        AudioManager.Instance.PlaySound(transform, _impactSFX, true, false, 1f, 1f);
        Instantiate(_impactVFX, transform.position, Quaternion.LookRotation(-transform.forward));
    }

    private void OnTriggerEnter(Collider other)
    {
        bool returnToPool = false;
        if (gameObject.CompareTag("EnemyProjectile") && other.gameObject.CompareTag("PlayerWeapon"))
        {
            gameObject.tag = "Deflected";
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
                ReturnToPool();
            }
            else if (gameObject.CompareTag("Deflected"))
            {
                var enemyHit = other.GetComponentInParent<BaseEnemy>();
                var projHit = other.GetComponent<Projectile>();
                var bossHit = other.GetComponentInParent<FirstBossEncounter>();
                var deflectedDamage = _isCritDeflected ? _critDeflectedDamage : _baseDeflectedDamage;

                if (enemyHit)
                {
                    enemyHit.Health.TakeDamage(deflectedDamage, Vector3.zero, null);
                    ReturnToPool();
                }
                else if (projHit)
                {
                    Debug.Log("Deflected Projectile hit projectile");
                    projHit.transform.rotation = Quaternion.LookRotation(transform.forward);
                    projHit.Rb.linearVelocity = transform.forward * _baseDeflectSpeed;
                    Rb.linearVelocity = projHit.transform.forward * _baseDeflectSpeed * 0.75f;
                    return;
                }
                else if (bossHit)
                {
                    bossHit.Health.TakeDamage(deflectedDamage, Vector3.zero, null);
                    ReturnToPool();
                }
            }
            else if (other.gameObject.layer != LayerMask.NameToLayer("Enemy"))
            {
                ReturnToPool();
            }
        }
    }

    protected virtual void OnPlayerSpawned(PlayerController player)
    {
        _player = player;
    }
}