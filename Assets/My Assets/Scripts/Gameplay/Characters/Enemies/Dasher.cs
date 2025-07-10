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
    private float _dashCooldownDuration = 1.5f;
    [SerializeField]
    private LayerMask _blockedLayers;
    [SerializeField]
    private AudioClip _dashingSFX;

    private float _lastDashCompleteTime;
    private bool _isDashing;

    private AudioSource _dashingAudio;
    private readonly Collider[] _overlapColliders = new Collider[1];


    protected override void OnDied(GameObject obj)
    {
        base.OnDied(obj);
        if (_dashingAudio) _dashingAudio.Stop();
    }

    private void Update()
    {
        _distToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        
        if (_isDashing || !_player.Health.IsAlive()) return;

        if (!_destinationSetter.target && _distToPlayer <= _agroRange)
        {
            _destinationSetter.target = _player.transform;
            _destinationSetter.enabled = true;
            _aiFollower.maxSpeed = _agroSpeed;
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

    private bool CanDashReachPlayer(out GameObject hitObj, bool dashing = false)
    {
        Vector3 centerWorldPos = transform.TransformPoint(_collider.center);
        float cylinderLength = Mathf.Max(0, _aiFollower.height * 0.5f - _aiFollower.radius);
        var p1 = centerWorldPos + Vector3.up * cylinderLength;
        var p2 = centerWorldPos - Vector3.up * cylinderLength;

        if (!dashing)
        {
            // First check if already overlapping obstacle
            var overlapCount = Physics.OverlapCapsuleNonAlloc(p1, p2, _aiFollower.radius * 1.2f, _overlapColliders, _blockedLayers);
            if (overlapCount > 0)
            {
                // Debug.Log("Enemy overlapping other collider(s)");
                hitObj = _overlapColliders[0].gameObject;
                return false;
            }
        }

        // Then check if enemy will hit an obstacle on the way
        var dir = (_player.transform.position - transform.position).normalized;
        var maxDist = dashing ? _aiFollower.radius : _distToPlayer;
        bool blocked = Physics.CapsuleCast(p1, p2, _aiFollower.radius, dir, out var hit, maxDist, _blockedLayers);
        hitObj = blocked ? hit.transform.gameObject : null;
        return !blocked;
    }

    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        _aiFollower.canMove = false;
        float pitch = Random.Range(1.1f, 1.3f);
        _agroAudio.Stop();
        _agroAudio = null;
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

            while (startTime + _dashDuration >= Time.time)
            {
                if (_isInterruptable && _isGettingKnockedBack)
                {
                    _dashingAudio.Stop();
                    _abilityStartAudio.Stop();
                    _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
                    _isDashing = false;
                    _lastDashCompleteTime = Time.time;
                    yield break;
                }
                
                if (!blocked)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPos, _dashSpeed * Time.deltaTime);
                    blocked = !CanDashReachPlayer(out var hitObj, true);
                    if (blocked) Debug.Log($"Enemy ran into: {hitObj.name}", hitObj);
                }

                yield return null;
            }
        }

        yield return new WaitForSeconds(_dashDelayDuration * 0.75f);
        _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
        _aiFollower.canMove = true;
        _isDashing = false;
        _lastDashCompleteTime = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            var playerHit = other.GetComponent<PlayerController>();
            if (playerHit)
            {
                var knockBackDir = (playerHit.transform.position - transform.position).normalized;
                var damage = _isDashing ? _dashDamage : _baseDamage;
                playerHit.Health.TakeDamage(damage, knockBackDir, _knockback);
                StartCoroutine(DamagedPlayerCoroutine());
            }
        }
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