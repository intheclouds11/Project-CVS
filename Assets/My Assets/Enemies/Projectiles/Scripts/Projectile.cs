using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour
{
    [Header("Base Projectile")]
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

    [Header("Base FX")]
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

    protected bool _abilityEnabled;
    protected string _poolKey;
    protected float _distToPlayer;
    protected bool _isCritDeflected;
    protected AudioSource _abilityAudio;
    protected MultiProjectilePool _pool;
    protected PlayerController _player;
    protected Animator _animator;


    protected virtual void Awake()
    {
        tag = "EnemyProjectile";
        _animator = GetComponent<Animator>();
        Rb = GetComponent<Rigidbody>();
    }

    protected virtual void OnEnable()
    {
        if (!_player)
        {
            PlayerSpawnManager.PlayerSpawned += OnPlayerSpawned;
        }
    }

    protected virtual void OnDisable()
    {
        if (_player)
        {
            PlayerSpawnManager.PlayerSpawned -= OnPlayerSpawned;
        }
    }

    protected virtual void Update()
    {
        if (Vector3.Distance(transform.position, _player.transform.position) > 30)
        {
            ReturnToPool(false, false);
        }
    }

    public void Init(MultiProjectilePool pool, string poolKey, bool enableAbility)
    {
        _pool = pool;
        _poolKey = poolKey;
        _abilityEnabled = enableAbility;
        if (_abilityEnabled)
        {
            // Debug.Log($"Ability enabled!", gameObject);
        }
    }

    protected void ReturnToPool(bool deflected, bool playImpactSFX)
    {
        if (playImpactSFX)
        {
            AudioManager.Instance.PlaySound(transform, _impactSFX, true, false, 1f, 1f);
        }

        var particleDirection = (FindAnyObjectByType<FirstBossEncounter>().transform.position - transform.position).normalized;
        particleDirection = deflected ? -particleDirection : particleDirection;
        var particleRotation = particleDirection == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(particleDirection);
        Instantiate(_impactVFX, _impactVFXSpawnPoint.position, particleRotation);

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
        if (_abilityAudio) _abilityAudio = null;
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

            var newForward = sawblade.DeflectDirection;
            transform.rotation = Quaternion.LookRotation(newForward);
            var deflectSpeed = _baseDeflectSpeed * sawblade.DamageEnemyKnockback.KnockbackAmount * (_isCritDeflected ? 2f : 1f);
            Rb.linearVelocity = newForward * deflectSpeed;
        }
        else
        {
            bool hitInvincible;
            var playerHit = other.GetComponent<PlayerController>();
            if (playerHit)
            {
                hitInvincible = playerHit.Health.IsInvincible();
                DamagePlayer(playerHit, false);
                ReturnToPool(false, hitInvincible);
            }
            else if (other.gameObject.layer != LayerMask.NameToLayer("Enemy"))
            {
                ReturnToPool(false, true);
            }
            else if (CompareTag("Deflected"))
            {
                var enemyHit = other.GetComponentInParent<BaseEnemy>();
                var projHit = other.GetComponent<Projectile>();
                var bossHit = other.GetComponentInParent<FirstBossEncounter>();
                var deflectedDamage = _isCritDeflected ? _critDeflectedDamage : _baseDeflectedDamage;

                if (enemyHit)
                {
                    hitInvincible = enemyHit.Health.Invincible;
                    enemyHit.Health.TakeDamage(deflectedDamage, Vector3.zero, null);
                    ReturnToPool(true, hitInvincible);
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
                    hitInvincible = bossHit.Health.Invincible;
                    bossHit.Health.TakeDamage(deflectedDamage, Vector3.zero, null);
                    ReturnToPool(true, hitInvincible);
                }
            }
        }
    }

    protected virtual void DamagePlayer(PlayerController playerHit, bool usingAbility)
    {
        var knockBackDir = (playerHit.transform.position - transform.position).normalized;
        playerHit.Health.TakeDamage(usingAbility ? 2 : _baseDamage, knockBackDir, _knockback);
    }

    protected virtual void OnPlayerSpawned(PlayerController player)
    {
        _player = player;
    }
}