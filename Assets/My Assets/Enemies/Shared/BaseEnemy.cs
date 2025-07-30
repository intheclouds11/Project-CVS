using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using NaughtyAttributes;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Utils;
using Random = UnityEngine.Random;

public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Base Movement")]
    [SerializeField]
    protected bool _wander;
    [SerializeField]
    protected float _wanderRadius = 2f;
    [SerializeField]
    protected float _wanderRecoveryMaxDuration = 2f;
    private Transform _targetWanderPointHolder; // Empty GameObject used by AIDestinationSetter

    [SerializeField]
    protected float _aggroRange = 5f;
    [SerializeField]
    protected float _alertDuration = 0.5f;
    [SerializeField]
    protected float _aggroSpeed = 3.5f;
    [SerializeField]
    protected float _attackCooldownDuration = 0.25f;
    [SerializeField]
    protected float _patrolSpeed = 1f;
    [SerializeField]
    protected LayerMask _blockedLayers;

    [Header("Base Offense")]
    [SerializeField]
    protected int _baseDamage = 1;
    [SerializeField]
    protected Knockback _damagePlayerKnockback;
    [SerializeField]
    protected Knockback _damageEnemyKnockback;
    protected Tween _knockbackTween;

    [Header("Base FX")]
    [SerializeField]
    protected AudioClip _patrolSFX;
    [SerializeField]
    protected AudioClip _alertedSFX;
    [SerializeField]
    protected AudioClip _aggroSFX;
    [SerializeField]
    protected AudioClip _abilityStartSFX;
    [SerializeField]
    protected AudioClip _hitByKnockbackSFX;
    [SerializeField]
    protected GameObject _sleepBubble;
    [SerializeField]
    protected GameObject _alertIcon;
    // [SerializeField]
    // private bool _rigidbodyForceOnDeath;
    // [SerializeField]
    // private Rigidbody _topRingRb;
    // [SerializeField]
    // private Rigidbody _midRingRb;
    // [SerializeField]
    // private Rigidbody _lowRingRb;
    // [SerializeField]
    // private Rigidbody _bodyRb;

    public Health Health { get; protected set; }
    public bool IsAggroed { get; protected set; }

    protected Vector3 _startingPos;
    public bool IsGettingKnockedBack { get; private set; }
    protected float _distToPlayer;
    protected bool _usingAbility;
    protected float _aggroPitch;
    protected Coroutine _knockbackCoroutine;
    protected Coroutine _attackMovementCooldownCoroutine;
    protected Coroutine _wanderCoroutine;
    protected Coroutine _startAggroCoroutine;
    private readonly Collider[] _overlapColliders = new Collider[5];
    protected AudioSource _patrolAudio;
    protected AudioSource _alertedAudio;
    protected AudioSource _aggroAudio;
    protected AudioSource _abilityStartAudio;
    protected PlayerController _player;
    protected FollowerEntity _aiFollower;
    protected AIDestinationSetter _destinationSetter;
    protected Patrol _patrol;
    protected Animator _animator;
    protected CapsuleCollider _collider;


    protected virtual void Awake()
    {
        _startingPos = transform.position;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Health = GetComponent<Health>();
        _collider = GetComponent<CapsuleCollider>();

        _animator = GetComponent<Animator>();
        _animator.Play("Idle", 0, Random.Range(0f, 1f));

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

        GameObject t = new GameObject($"{gameObject.name} WanderTarget");
        t.transform.parent = transform.parent;
        t.transform.position = transform.position;
        _targetWanderPointHolder = t.transform;
        _destinationSetter.target = _targetWanderPointHolder;
        _sleepBubble.SetActive(!_wander);

        _aggroPitch = Random.Range(0.9f, 1.1f);
        _alertIcon.SetActive(false);
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        ClearLoopingAudio();
    }

    protected void OnEnable()
    {
        EnemyManager.Instance.RegisterEnemy(this);
        GameManager.EnemyAIToggled += OnEnemyAIToggled;
        _aiFollower.canMove = GameManager.Instance.EnemyAIEnabled;
        Health.Died += OnDied;
        Health.DamageTaken += OnDamageTaken;
        if (!_player)
        {
            PlayerSpawnManager.PlayerSpawned += OnPlayerSpawned;
        }
    }

    protected void OnDisable()
    {
        EnemyManager.Instance.DeregisterEnemy(this);
        GameManager.EnemyAIToggled -= OnEnemyAIToggled;
        Health.Died -= OnDied;
        Health.DamageTaken -= OnDamageTaken;
        if (_player)
        {
            PlayerSpawnManager.PlayerSpawned -= OnPlayerSpawned;
            _player.Health.Died -= OnPlayerDied;
            _player = null;
        }
    }

    protected virtual void Update()
    {
        _distToPlayer = GameManager.Instance.GetDistanceFromPlayer(transform);

        if (!IsAggroed && _wander)
        {
            if (GameManager.Instance.EnemyAIEnabled && _player && _player.Health.IsAlive() && _distToPlayer <= _aggroRange &&
                _startAggroCoroutine == null)
            {
                _startAggroCoroutine = StartCoroutine(StartAggroCoroutine());
            }

            if (_wanderCoroutine == null && _aiFollower.reachedDestination)
            {
                _wanderCoroutine = StartCoroutine(SetWanderDestination());
            }
        }
    }

    private IEnumerator SetWanderDestination()
    {
        Vector3 randomDirection = transform.position;
        bool exitLoop = false;
        while (!exitLoop)
        {
            randomDirection = Random.insideUnitSphere * _wanderRadius;
            randomDirection = new Vector3(randomDirection.x, 0f, randomDirection.z);
            randomDirection += transform.position;

            if (Vector3.Distance(randomDirection, _startingPos) > _wanderRadius)
            {
                yield return null;
            }
            else
            {
                exitLoop = true;
            }
        }

        GraphNode node = AstarPath.active.GetNearest(randomDirection, NNConstraint.Walkable).node;
        if (node != null && node.Walkable)
        {
            _targetWanderPointHolder.position = randomDirection;
        }

        yield return new WaitForSeconds(Random.Range(_wanderRecoveryMaxDuration * 0.5f, _wanderRecoveryMaxDuration));

        _wanderCoroutine = null;
    }

    protected bool IsOverlappingBlockedLayer(Vector3 p1, Vector3 p2, out GameObject overlapObj)
    {
        var overlapCount = Physics.OverlapCapsuleNonAlloc(p1, p2, _aiFollower.radius * 1.2f, _overlapColliders, _blockedLayers);
        if (overlapCount > 0)
        {
            // Debug.Log("Enemy overlapping other collider(s)");
            var overlapList = _overlapColliders.ToList();
            overlapList.Remove(_collider);
            if (overlapList.Any(c => c && (!c.TryGetComponent(out BaseEnemy otherEnemy) || otherEnemy._distToPlayer < _distToPlayer)))
            {
                overlapObj = _overlapColliders[0].gameObject;
                return true;
            }
        }

        overlapObj = null;
        return false;
    }

    private IEnumerator KnockbackCoroutine(Knockback knockback)
    {
        _aiFollower.canMove = false;
        IsGettingKnockedBack = true;
        if (Health.IsAlive())
        {
            _animator.SetTrigger("HitReact");
        }

        transform.GetCylinderPoints(_collider.center, _aiFollower.height, _aiFollower.radius, out var p01, out var p02);
        if (!IsOverlappingBlockedLayer(p01, p02, out var hit))
        {
            _knockbackTween?.Kill();
            Vector3 targetPos = transform.position + knockback.Direction.RemovePitch() * knockback.KnockbackAmount;
            _knockbackTween = transform.DOMove(targetPos, knockback.KnockbackDuration).SetEase(knockback.KnockbackEasing);

            while (_knockbackTween != null && _knockbackTween.IsActive())
            {
                transform.GetCylinderPoints(_collider.center, _aiFollower.height, _aiFollower.radius, out var p1, out var p2);
                if (IsOverlappingBlockedLayer(p1, p2, out var hitObj))
                {
                    Debug.Log("OVERLAP", hitObj);
                    _knockbackTween.Kill();
                }

                yield return null;
            }
        }

        if (!Health.IsAlive())
        {
            _collider.enabled = false;
        }

        yield return new WaitForSeconds(knockback.StunDuration);

        _aiFollower.canMove = Health.IsAlive() && GameManager.Instance.EnemyAIEnabled && !_usingAbility;
        IsGettingKnockedBack = false;
        _knockbackCoroutine = null;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (IsGettingKnockedBack && other.TryGetComponent(out BaseEnemy enemyHit))
        {
            if (enemyHit && !enemyHit.IsGettingKnockedBack)
            {
                _knockbackTween?.Kill();
                AudioManager.Instance.PlaySound(enemyHit.transform, _hitByKnockbackSFX);
                _damageEnemyKnockback.Direction = (enemyHit.transform.position - transform.position).normalized;
                enemyHit.Health.TakeDamage((int) (enemyHit.Health.GetMaxHealth * 0.25f), _damageEnemyKnockback);
                OnDamagedOther();
            }
        }
    }

    protected void OnDamagedPlayer()
    {
        if (!Health.IsAlive() || !GameManager.Instance.EnemyAIEnabled) return;
        if (!IsAggroed) // player ran into sleeping enemy
        {
            _startAggroCoroutine = StartCoroutine(StartAggroCoroutine());
            return;
        }

        OnDamagedOther();
    }

    protected void OnDamagedOther()
    {
        if (_attackMovementCooldownCoroutine != null) StopCoroutine(_attackMovementCooldownCoroutine);
        _attackMovementCooldownCoroutine = StartCoroutine(AttackMovementCooldownCoroutine());
    }

    protected IEnumerator AttackMovementCooldownCoroutine()
    {
        _aiFollower.canMove = false;

        yield return new WaitForSeconds(_attackCooldownDuration);

        _aiFollower.canMove = GameManager.Instance.EnemyAIEnabled;
    }

    protected virtual void OnDamageTaken(Knockback knockback)
    {
        if (Health.IsAlive() && !IsAggroed && GameManager.Instance.EnemyAIEnabled)
        {
            if (_startAggroCoroutine != null)
            {
                StopCoroutine(_startAggroCoroutine);
                _startAggroCoroutine = null;
                Aggro();
            }
            else // hit while sleeping
            {
                _startAggroCoroutine = StartCoroutine(StartAggroCoroutine());
                // _alertedAudio = AudioManager.Instance.PlaySound(transform, _alertedSFX, true, false, 0.55f);
            }
        }

        if (knockback == null || !knockback.ApplyKnockback || knockback.Direction == Vector3.zero ||
            knockback.KnockbackAmount <= 0) return;

        if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
        _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(knockback));
    }

    protected IEnumerator StartAggroCoroutine()
    {
        _animator.SetTrigger("Alerted");
        _sleepBubble.SetActive(false);
        _alertedAudio = AudioManager.Instance.PlaySound(transform, _alertedSFX, true, false, 0.55f);
        _alertIcon.SetActive(true);
        yield return new WaitForSeconds(_alertDuration);

        if (_player.Health.IsAlive())
        {
            Aggro();
        }

        _startAggroCoroutine = null;
    }

    protected void Aggro()
    {
        _destinationSetter.target = _player.transform;
        _destinationSetter.enabled = true;
        _aiFollower.maxSpeed = _aggroSpeed;
        _aiFollower.canMove = true;

        if (_wanderCoroutine != null) StopCoroutine(_wanderCoroutine);
        _patrol.enabled = false;
        if (_patrolAudio) _patrolAudio.Stop();
        _patrolAudio = null;
        _aggroAudio = AudioManager.Instance.PlaySoundLoop(transform, _aggroSFX, true, 1f, _aggroPitch);
        _sleepBubble.SetActive(false);
        _alertIcon.SetActive(false);
        IsAggroed = true;
    }

    protected virtual void OnDied(Knockback knockback)
    {
        _aiFollower.enabled = false;

        if (_knockbackCoroutine == null) _collider.enabled = false;
        if (_startAggroCoroutine != null) StopCoroutine(_startAggroCoroutine);

        // if (_rigidbodyForceOnDeath)
        // {
        //     _animator.enabled = false;
        //     _bodyRb.isKinematic = false;
        //     _bodyRb.GetComponent<Collider>().enabled = true;
        //     _topRingRb.isKinematic = false;
        //     _topRingRb.GetComponent<Collider>().enabled = true;
        //     _midRingRb.isKinematic = false;
        //     _midRingRb.GetComponent<Collider>().enabled = true;
        //     _lowRingRb.isKinematic = false;
        //     _lowRingRb.GetComponent<Collider>().enabled = true;
        //     _topRingRb.AddForce(knockback.Direction * knockback.KnockbackAmount, ForceMode.Impulse);
        //     _midRingRb.AddForce(knockback.Direction * knockback.KnockbackAmount, ForceMode.Impulse);
        //     _lowRingRb.AddForce(knockback.Direction * knockback.KnockbackAmount, ForceMode.Impulse);
        // }
        // else
        {
            _animator.ResetTrigger("Alerted");
            _animator.ResetTrigger("HitReact");
            _animator.SetTrigger("Death");
        }

        _alertIcon.SetActive(false);
        _sleepBubble.SetActive(false);
        ClearLoopingAudio();
        if (_attackMovementCooldownCoroutine != null) StopCoroutine(_attackMovementCooldownCoroutine);
        if (_startAggroCoroutine != null) StopCoroutine(_startAggroCoroutine);
        if (_abilityStartAudio)
        {
            _abilityStartAudio.Stop();
            _abilityStartAudio = null;
        }

        enabled = false;
        _aiFollower.canMove = false;
    }

    private void OnEnemyAIToggled(bool toggle)
    {
        _aiFollower.canMove = toggle;
    }

    protected virtual void OnPlayerSpawned(PlayerController player)
    {
        _player = player;
        _player.Health.Died += OnPlayerDied;
    }

    private void OnPlayerDied(Knockback knockback)
    {
        _aiFollower.maxSpeed = _patrolSpeed;
        _targetWanderPointHolder.position = _startingPos;
        _destinationSetter.target = _targetWanderPointHolder;
    }

    private void ClearLoopingAudio()
    {
        if (_aggroAudio)
        {
            _aggroAudio.Stop();
            _aggroAudio = null;
        }

        if (_patrolAudio)
        {
            _patrolAudio.Stop();
            _patrolAudio = null;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        var gizmosColor = Gizmos.color;
        if (_wander)
        {
            Gizmos.color = Color.blue;
            GizmosExtensions.DrawWireCircle(transform.position, _wanderRadius);
        }

        Gizmos.color = Color.white;
        GizmosExtensions.DrawWireCircle(transform.position, _aggroRange);

        Gizmos.color = gizmosColor;
    }
}