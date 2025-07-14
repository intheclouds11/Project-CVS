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
    private float _dashSpeed = 6f;
    [SerializeField]
    private float _dashDuration = 0.5f;
    [SerializeField]
    private float _dashDelayDuration = 1f;
    [SerializeField]
    private float _dashRecoveryDuration = 0.35f;
    [SerializeField]
    private float _dashCooldownDuration = 1.5f;
    [SerializeField]
    private AudioClip _dashingSFX;
    [SerializeField]
    private AudioClip _interruptedSFX;

    private float _lastDashCompleteTime;
    private bool _isDashing;

    private AudioSource _dashingAudio;


    protected override void Update()
    {
        base.Update();
        _distToPlayer = Vector3.Distance(transform.position, _player.transform.position);

        if (!_aiFollower.canMove || !_player.Health.IsAlive()) return;

        if (!IsAggroed && _distToPlayer <= _agroRange)
        {
            IsAggroed = true;
            _lastDashCompleteTime = Time.time;
            _destinationSetter.target = _player.transform;
            _destinationSetter.enabled = true;
            _aiFollower.maxSpeed = _agroSpeed;
            _aiFollower.canMove = true;

            _patrol.enabled = false;
            if (_patrolAudio) _patrolAudio.Stop();
            _patrolAudio = null;
            _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
        }
        else if (_distToPlayer <= _dashTriggerDistance && Time.time >= _lastDashCompleteTime + _dashCooldownDuration)
        {
            if (CanDashReachPlayer(out var hitObj))
            {
                StartCoroutine(DashCoroutine());
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
        _isDashing = true;
        if (_aiFollower) _aiFollower.canMove = false;
        float pitch = Random.Range(1.1f, 1.3f);
        if (_agroAudio) _agroAudio.Stop();
        _abilityStartAudio = AudioManager.Instance.PlaySound(transform, _abilityStartSFX, true, false, 0.55f, pitch);
        _animator.SetTrigger("Alerted");

        yield return new WaitForSeconds(_dashDelayDuration);

        bool blocked = !CanDashReachPlayer(out var preHitObj);
        if (!blocked)
        {
            _dashingAudio = AudioManager.Instance.PlaySound(transform, _dashingSFX, true, false, 0.6f, pitch);

            float startTime = Time.time;
            var dir = (_player.transform.position - transform.position).normalized;
            var targetPos = _player.transform.position + dir * 4f;

            while (!blocked && startTime + _dashDuration >= Time.time)
            {
                if (_isInterruptable && IsGettingKnockedBack)
                {
                    _isDashing = false;
                    _lastDashCompleteTime = Time.time;
                    _dashingAudio.Stop();
                    _abilityStartAudio.Stop();
                    pitch = Random.Range(0.9f, 1.1f);
                    AudioManager.Instance.PlaySound(transform, _interruptedSFX, true, false, 0.9f, pitch);
                    _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
                    yield break;
                }

                transform.position = Vector3.Lerp(transform.position, targetPos, _dashSpeed * Time.deltaTime);
                blocked = !CanDashReachPlayer(out var hitObj, true);
                if (blocked)
                {
                    Debug.Log($"Enemy ran into: {hitObj.name}", hitObj);
                }

                yield return null;
            }
        }

        yield return new WaitForSeconds(_dashRecoveryDuration);
        _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
        _aiFollower.canMove = true;
        _isDashing = false;
        _lastDashCompleteTime = Time.time;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (other.gameObject.CompareTag("Player"))
        {
            var playerHit = other.GetComponent<PlayerController>();
            if (playerHit)
            {
                var knockBackDir = (playerHit.transform.position - transform.position).normalized;
                var damage = _isDashing ? _dashDamage : _baseDamage;
                playerHit.Health.TakeDamage(damage, knockBackDir, _damagePlayerKnockback);
                OnDamagedPlayer();
            }
        }
    }

    protected override void OnDied(GameObject obj)
    {
        base.OnDied(obj);
        if (_dashingAudio) _dashingAudio.Stop();
    }

    private void OnDrawGizmosSelected()
    {
        var gizmosColor = Gizmos.color;
        Gizmos.color = Color.white;
        GizmosExtensions.DrawWireCircle(transform.position, _agroRange);
        Gizmos.color = Color.yellow;
        GizmosExtensions.DrawWireCircle(transform.position, _dashTriggerDistance);
        Gizmos.color = gizmosColor;
    }
}