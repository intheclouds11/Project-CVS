using System;
using System.Collections;
using System.Linq;
using Pathfinding;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Base Movement")]
    [SerializeField]
    protected float _agroRange = 5f;
    [FormerlySerializedAs("_moveSpeed")]
    [SerializeField]
    protected float _agroSpeed = 3.5f;
    [SerializeField]
    protected float _attackCooldownDuration = 0.25f;
    [SerializeField]
    protected float _patrolSpeed = 1f;
    
    [Header("Base Offense")]
    [SerializeField]
    protected int _baseDamage = 1;
    [SerializeField]
    protected Knockback _knockback;

    [Header("Base FX")]
    [SerializeField]
    protected AudioClip _patrolSFX;
    [SerializeField]
    protected AudioClip _agroSFX;
    [SerializeField]
    protected AudioClip _abilityStartSFX;

    public Health Health { get; protected set; }

    protected bool _isGettingKnockedBack;
    protected Coroutine _knockbackCoroutine;
    protected float _distToPlayer;
    protected float _agroPitch;
    protected AudioSource _patrolAudio;
    protected AudioSource _agroAudio;
    protected AudioSource _abilityStartAudio;
    protected PlayerController _player;
    protected FollowerEntity _aiFollower;
    protected AIDestinationSetter _destinationSetter;
    protected Patrol _patrol;
    protected Animator _animator;
    protected CapsuleCollider _collider;


    protected virtual void Awake()
    {
        _player = GameManager.Instance?.Player1;
        Health = GetComponent<Health>();
        Health.Died += OnDied;
        Health.DamageTaken += OnDamageTaken;
        _animator = GetComponent<Animator>();
        _collider = GetComponent<CapsuleCollider>();
        _aiFollower = GetComponent<FollowerEntity>();
        _aiFollower.maxSpeed = _patrolSpeed;
        _destinationSetter = GetComponent<AIDestinationSetter>();
        _patrol = GetComponent<Patrol>();
        if (_patrol)
        {
            if (_patrol.targets.Any())
            {
                _destinationSetter.enabled = false;
                _patrolAudio = AudioManager.Instance.PlaySoundLoop(transform, _patrolSFX, true);
            }
            else
            {
                _patrol.enabled = false;
            }
        }

        _agroPitch = Random.Range(0.9f, 1.1f);
    }

    private void OnDamageTaken(Vector3 knockbackDir, Knockback knockback)
    {
        if (!knockback.ApplyKnockback || knockback.KnockbackAmount <= 0) return;
        
        if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
        _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(knockbackDir, knockback));
    }
    
    private IEnumerator KnockbackCoroutine(Vector3 dir, Knockback knockback)
    {
        _aiFollower.canMove = false;
        _isGettingKnockedBack = true;
        var startTime = Time.time;

        while (Time.time < startTime + knockback.KnockbackDuration && Health.IsAlive())
        {
            var targetPos = transform.position + dir * knockback.KnockbackAmount;
            transform.position = Vector3.Lerp(transform.position, targetPos, 5 * Time.deltaTime);
            yield return null;
        }
        
        yield return new WaitForSeconds(knockback.StunDuration);
        
        _aiFollower.canMove = true;
        _isGettingKnockedBack = false;
        _knockbackCoroutine = null;
    }
    
    protected IEnumerator DamagedPlayerCoroutine()
    {
        _aiFollower.canMove = false;

        yield return new WaitForSeconds(_attackCooldownDuration);
        
        _aiFollower.canMove = true;
    }

    protected void OnEnable()
    {
        Health.Died += OnDied;
        PlayerSpawnPoint.PlayerSpawned += OnPlayerSpawned;
    }

    protected void OnDisable()
    {
        Health.Died -= OnDied;
        PlayerSpawnPoint.PlayerSpawned -= OnPlayerSpawned;
    }

    protected void Start()
    {
        EnemyManager.Instance.RegisterEnemy(this);
    }

    protected virtual void OnPlayerSpawned(PlayerController player)
    {
        _player = player;
    }

    protected virtual void OnDied(GameObject obj)
    {
        if (_agroAudio) _agroAudio.Stop();
        if (_patrolAudio) _patrolAudio.Stop();
        if (_abilityStartAudio) _abilityStartAudio.Stop();
        gameObject.SetActive(false);
    }
}