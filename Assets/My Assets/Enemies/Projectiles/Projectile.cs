using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    protected int _baseDamage = 2;
    [SerializeField]
    protected Knockback _knockback;
    
    public Rigidbody Rb { get; private set; }
    protected string _poolKey;
    protected float _distToPlayer;
    protected MultiProjectilePool _pool;
    protected PlayerController _player;
    protected Animator _animator;
    


    protected void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("EnemyProjectile");
        _animator = GetComponent<Animator>();
        Rb = gameObject.AddComponent<Rigidbody>();
        Rb.useGravity = false;
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
        gameObject.layer = LayerMask.NameToLayer("EnemyProjectile");
        Rb.linearVelocity = Vector3.zero;
        _pool.Return(_poolKey, gameObject);
        OnReturnToPool();
    }

    protected virtual void OnReturnToPool()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Weapon"))
        {
            Debug.Log($"Deflect projectile");
            gameObject.layer = LayerMask.NameToLayer("Weapon");
            Rb.linearVelocity = Vector3.zero;
            Rb.linearVelocity = other.transform.forward * 10f;
        }
        else
        {
            if (other.gameObject.CompareTag("Player"))
            {
                var playerHit = other.GetComponent<PlayerController>();
                if (playerHit)
                {
                    var knockBackDir = (playerHit.transform.position - transform.position).normalized;
                    playerHit.Health.TakeDamage(_baseDamage, knockBackDir, _knockback);
                }
            }
            
            var enemyHit = other.GetComponentInParent<BaseEnemy>();
            if (enemyHit)
            {
                Debug.Log($"Deflected projectile hit {enemyHit}");

                enemyHit.Health.TakeDamage(1000, Vector3.zero, null);
            }
            else
            {
                var bossHit = other.GetComponentInParent<FirstBossEncounter>();
                if (bossHit)
                {
                    Debug.Log($"Deflected projectile hit {bossHit}");

                    bossHit.Health.TakeDamage(200, Vector3.zero, null);
                }
            }

            ReturnToPool();
        }
    }
    
    protected virtual void OnPlayerSpawned(PlayerController player)
    {
        _player = player;
    }
}