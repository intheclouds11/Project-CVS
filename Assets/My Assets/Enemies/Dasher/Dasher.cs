using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using Random = UnityEngine.Random;

public class Dasher : BaseEnemy
{
    [Header("Dash")]
    [SerializeField]
    protected int _dashDamage = 2;
    [SerializeField]
    private bool _isInterruptable = true;
    [SerializeField]
    private float _dashTriggerDistance = 2.5f;
    [SerializeField]
    private float _dashStartDelay = 0.5f;
    [SerializeField]
    private float _dashSpeed = 6f;
    [SerializeField]
    private float _dashDuration = 0.5f;
    [SerializeField]
    private float _dashRecoveryDuration = 0.35f;
    [SerializeField]
    private float _dashCooldownDuration = 1.5f;
    [SerializeField]
    private AudioClip _dashingSFX;

    private float _lastDashTime;
    private bool _applyDashDamage;
    private AudioSource _dashingAudio;
    private Coroutine _dashCoroutine;


    protected override void Update()
    {
        base.Update();

        if (!IsAggroed || !GameManager.Instance.EnemyAIEnabled || _usingAbility || !_aiFollower.canMove ||
            !_player.Health.IsAlive()) return;

        if (_distToPlayer <= _dashTriggerDistance && Time.time >= _lastDashTime + _dashCooldownDuration)
        {
            if (CanDashReachPlayer(out var hitObj))
            {
                _dashCoroutine = StartCoroutine(DashCoroutine());
            }
        }
    }

    protected bool CanDashReachPlayer(out GameObject hitObj, bool dashing = false)
    {
        transform.GetCylinderPoints(_collider.center, _aiFollower.height, _aiFollower.radius, out var p1, out var p2);

        if (!dashing)
        {
            // First check if already overlapping obstacle
            if (IsOverlappingBlockedLayer(p1, p2, out var overlapObj))
            {
                hitObj = overlapObj;
                return false;
            }
        }

        // Then check if enemy will hit an obstacle on the way
        var dir = (_player.transform.position - transform.position).normalized;
        var maxDist = dashing ? _aiFollower.radius * 1.25f : _distToPlayer;
        bool blocked = Physics.CapsuleCast(p1, p2, _aiFollower.radius, dir, out var hit, maxDist, _blockedLayers);
        hitObj = blocked ? hit.transform.gameObject : null;
        return !blocked;
    }

    private IEnumerator DashCoroutine()
    {
        _usingAbility = true;
        _aiFollower.canMove = false;
        float pitch = Random.Range(1.1f, 1.3f);
        if (_aggroAudio) _aggroAudio.Stop();
        _abilityStartAudio = AudioManager.Instance.PlaySound(transform, _abilityStartSFX, true, false, 0.55f, pitch);
        _animator.SetTrigger("DashWindup");

        var startTime = Time.time;
        while (startTime + _dashStartDelay >= Time.time)
        {
            if (_isInterruptable && IsGettingKnockedBack)
            {
                _usingAbility = false;
                _lastDashTime = Time.time;
                _aggroAudio = AudioManager.Instance.PlaySoundLoop(transform, _aggroSFX, true, 1f, _aggroPitch);
                yield break;
            }

            yield return null;
        }

        _applyDashDamage = true;
        Health.Invincible = true;
        _abilityStartAudio.Stop();
        bool interrupted = false;

        bool blocked = !CanDashReachPlayer(out var preHitObj, true);
        if (!blocked)
        {
            _animator.SetBool("IsDashing", true);
            _dashingAudio = AudioManager.Instance.PlaySound(transform, _dashingSFX, true, false, 0.6f, pitch);

            startTime = Time.time;
            var dir = (_player.transform.position - transform.position).normalized;
            var targetPos = _player.transform.position + dir * 4f;
            float elapsedTime = 0f;

            while (!blocked && !interrupted && startTime + _dashDuration >= Time.time)
            {
                if (_isInterruptable && IsGettingKnockedBack)
                {
                    interrupted = true;
                    continue;
                }

                if (elapsedTime >= _dashDuration * 0.8f)
                {
                    _animator.SetBool("IsDashing", false);
                    _applyDashDamage = false;
                    Health.Invincible = false;
                }

                transform.position = Vector3.Lerp(transform.position, targetPos, _dashSpeed * Time.deltaTime);
                blocked = !CanDashReachPlayer(out var hitObj, true);
                if (blocked)
                {
                    Debug.Log($"Enemy ran into: {hitObj.name}", hitObj);
                    _animator.SetBool("IsDashing", false);
                    _dashingAudio.Stop();
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            _animator.SetTrigger("CancelWindup");
            _dashingAudio.Stop();
        }

        if (interrupted)
        {
            _animator.SetBool("IsDashing", false);
        }
        else
        {
            yield return new WaitForSeconds(_dashRecoveryDuration);
            _aiFollower.canMove = true;
        }

        _aggroAudio = AudioManager.Instance.PlaySoundLoop(transform, _aggroSFX, true, 1f, _aggroPitch);
        Health.Invincible = false;
        _applyDashDamage = false;
        _lastDashTime = Time.time;
        _usingAbility = false;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (enabled && other.gameObject.CompareTag("Player"))
        {
            var playerHit = other.GetComponent<PlayerController>();
            if (playerHit)
            {
                var damage = _applyDashDamage ? _dashDamage : _baseDamage;
                _damagePlayerKnockback.Direction = (playerHit.transform.position - transform.position).normalized;
                playerHit.Health.TakeDamage(damage, _damagePlayerKnockback);
                OnDamagedPlayer();
            }
        }
    }

    protected override void OnDied(Knockback knockback)
    {
        base.OnDied(knockback);
        if (_dashCoroutine != null) StopCoroutine(_dashCoroutine);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        var gizmosColor = Gizmos.color;
        Gizmos.color = Color.yellow;
        GizmosExtensions.DrawWireCircle(transform.position, _dashTriggerDistance);
        Gizmos.color = gizmosColor;
    }
}