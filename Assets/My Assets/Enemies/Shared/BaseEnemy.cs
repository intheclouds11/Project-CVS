using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using NaughtyAttributes;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Base Movement")]
    [SerializeField]
    protected bool _wander;
    [field: SerializeField, ShowIf(nameof(_wander))]
    protected float _wanderRadius = 2f;
    [field: SerializeField, ShowIf(nameof(_wander))]
    protected float _wanderRecoveryMaxDuration = 2f;
    private Transform _targetWanderPointHolder; // Empty GameObject used by AIDestinationSetter

    [SerializeField]
    protected float _agroRange = 5f;
    [FormerlySerializedAs("_moveSpeed")]
    [SerializeField]
    protected float _agroSpeed = 3.5f;
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
    protected AudioClip _agroSFX;
    [SerializeField]
    protected AudioClip _abilityStartSFX;
    [SerializeField]
    protected AudioClip _hitByKnockbackSFX;

    public Health Health { get; protected set; }
    public bool IsAggroed { get; protected set; }

    protected Vector3 _startingPos;
    public bool IsGettingKnockedBack { get; private set; }
    protected float _distToPlayer;
    protected bool _usingAbility;
    protected float _agroPitch;
    protected Coroutine _knockbackCoroutine;
    protected Coroutine _damagedPlayerCoroutine;
    protected Coroutine _wanderCoroutine;
    private readonly Collider[] _overlapColliders = new Collider[5];
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
        _startingPos = transform.position;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Health = GetComponent<Health>();
        Health.Died += OnDied;
        Health.DamageTaken += OnDamageTaken;
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

        _agroPitch = Random.Range(0.9f, 1.1f);
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        ClearLoopingAudio();
    }

    protected void OnEnable()
    {
        GameManager.EnemyAIToggled += OnEnemyAIToggled;
        _aiFollower.canMove = GameManager.Instance.EnemyAIEnabled;
        Health.Died += OnDied;
        if (!_player)
        {
            PlayerSpawnManager.PlayerSpawned += OnPlayerSpawned;
        }
    }

    protected void OnDisable()
    {
        GameManager.EnemyAIToggled -= OnEnemyAIToggled;
        Health.Died -= OnDied;
        if (_player)
        {
            PlayerSpawnManager.PlayerSpawned -= OnPlayerSpawned;
        }
    }

    protected void Start()
    {
        EnemyManager.Instance.RegisterEnemy(this);
    }

    protected virtual void Update()
    {
        if (_wander && !IsAggroed)
        {
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

    private void OnEnemyAIToggled(bool toggle)
    {
        _aiFollower.canMove = toggle;
    }

    protected virtual void OnPlayerSpawned(PlayerController player)
    {
        _player = player;
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

    private IEnumerator KnockbackCoroutine(Vector3 dir, Knockback knockback)
    {
        _aiFollower.canMove = false;
        IsGettingKnockedBack = true;
        var prevAnimatorSpeed = _animator.speed;
        _animator.speed = 0.5f;

        _knockbackTween?.Kill();
        Vector3 targetPos = transform.position + dir * knockback.KnockbackAmount;
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

        yield return new WaitForSeconds(knockback.StunDuration);

        _aiFollower.canMove = GameManager.Instance.EnemyAIEnabled && !_usingAbility;
        IsGettingKnockedBack = false;
        _knockbackCoroutine = null;
        _animator.speed = prevAnimatorSpeed;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (IsGettingKnockedBack && other.TryGetComponent(out BaseEnemy enemyHit))
        {
            if (enemyHit && !enemyHit.IsGettingKnockedBack)
            {
                _knockbackTween?.Kill();
                AudioManager.Instance.PlaySound(enemyHit.transform, _hitByKnockbackSFX);
                var knockBackDir = (enemyHit.transform.position - transform.position).normalized;
                enemyHit.Health.TakeDamage(_baseDamage, knockBackDir, _damageEnemyKnockback);
                OnDamagedPlayer();
            }
        }
    }

    protected void OnDamagedPlayer()
    {
        if (!Health.IsAlive()) return;
        if (_damagedPlayerCoroutine != null) StopCoroutine(_damagedPlayerCoroutine);
        _damagedPlayerCoroutine = StartCoroutine(DamagedPlayerCoroutine());
    }

    protected IEnumerator DamagedPlayerCoroutine()
    {
        _aiFollower.canMove = false;

        yield return new WaitForSeconds(_attackCooldownDuration);

        _aiFollower.canMove = GameManager.Instance.EnemyAIEnabled;
    }

    protected virtual void OnDamageTaken(Vector3 knockbackDir, Knockback knockback)
    {
        IsAggroed = true;

        if (knockback == null || !knockback.ApplyKnockback || knockbackDir == Vector3.zero || knockback.KnockbackAmount <= 0) return;

        if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
        transform.GetCylinderPoints(_collider.center, _aiFollower.height, _aiFollower.radius, out var p1, out var p2);
        if (!IsOverlappingBlockedLayer(p1, p2, out var hitObj))
        {
            _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(knockbackDir, knockback));
        }
        else
        {
            // Debug.Log("Prevent knockback. OVERLAP", hitObj);
        }
    }

    protected virtual void OnDied(GameObject obj)
    {
        ClearLoopingAudio();
        if (_abilityStartAudio)
        {
            _abilityStartAudio.Stop();
            _abilityStartAudio = null;
        }

        gameObject.SetActive(false);
    }

    private void ClearLoopingAudio()
    {
        if (_agroAudio)
        {
            _agroAudio.Stop();
            _agroAudio = null;
        }

        if (_patrolAudio)
        {
            _patrolAudio.Stop();
            _patrolAudio = null;
        }
    }
}