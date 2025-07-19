using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using Random = UnityEngine.Random;

public class Pulser : BaseEnemy
{
    [Header("Pulse")]
    [SerializeField]
    protected int _pulseDamage = 2;
    [SerializeField]
    private bool _isInterruptable;
    [SerializeField]
    private float _pulseTriggerDistance = 2.5f;
    [SerializeField]
    private float _pulseDamageRadius = 2.5f;
    [SerializeField]
    private float _pulseDuration = 0.3f;
    [SerializeField]
    private float _pulseDelayDuration = 0.5f;
    [SerializeField]
    private float _pulseCooldownDuration = 1.5f;
    [SerializeField]
    private GameObject _pulseVFX;
    [SerializeField]
    private Animator _lightAnimator;
    [SerializeField]
    private AudioClip _pulseHitSFX;

    private float _lastPulseCompleteTime;


    protected override void Update()
    {
        base.Update();
        _distToPlayer = GameManager.Instance.GetDistanceFromPlayer(transform);
        
        if (!_aiFollower.canMove || !_player.Health.IsAlive()) return;

        if (!IsAggroed && _distToPlayer <= _agroRange)
        {
            IsAggroed = true;
            _lastPulseCompleteTime = Time.time;
            _destinationSetter.target = _player.transform;
            _destinationSetter.enabled = true;
            _aiFollower.maxSpeed = _agroSpeed;
            _aiFollower.canMove = true;

            _patrol.enabled = false;
            if (_patrolAudio) _patrolAudio.Stop();
            _patrolAudio = null;
            _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
        }
        else if (_distToPlayer <= _pulseTriggerDistance && Time.time >= _lastPulseCompleteTime + _pulseCooldownDuration)
        {
            StartCoroutine(PulseCoroutine());
        }
    }

    private IEnumerator PulseCoroutine()
    {
        _aiFollower.canMove = false;
        _pulseVFX.SetActive(false);
        _pulseVFX.SetActive(true);
        _lightAnimator.SetTrigger("Pulse");
        _animator.SetTrigger("Alerted");

        if (_agroAudio) _agroAudio.Stop();
        _abilityStartAudio = AudioManager.Instance.PlaySound(transform, _abilityStartSFX, true, false, 0.7f);

        yield return new WaitForSeconds(_pulseDelayDuration);
        
        var startTime = Time.time;

        while (startTime + _pulseDuration >= Time.time)
        {
            if (_isInterruptable && IsGettingKnockedBack)
            {
                _lastPulseCompleteTime = Time.time;
                _pulseVFX.SetActive(false);
                _abilityStartAudio.Stop();
                _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
                yield break;
            }
            
            if (!_player.Health.IsInvincible() && !_player.IsDashing && _distToPlayer <= _pulseDamageRadius)
            {
                var knockBackDir = (_player.transform.position - transform.position).normalized;
                _player.Health.TakeDamage(_pulseDamage, knockBackDir, _damagePlayerKnockback);
                AudioManager.Instance.PlaySound(_player.transform, _pulseHitSFX, true, false, 0.9f);
                OnDamagedPlayer();
            }

            yield return null;
        }

        _lastPulseCompleteTime = Time.time;
        _aiFollower.canMove = true;
        _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
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
                playerHit.Health.TakeDamage(_baseDamage, knockBackDir, _damagePlayerKnockback);
                OnDamagedPlayer();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        var gizmosColor = Gizmos.color;
        Gizmos.color = Color.white;
        GizmosExtensions.DrawWireCircle(transform.position, _agroRange);
        Gizmos.color = Color.yellow;
        GizmosExtensions.DrawWireCircle(transform.position, _pulseTriggerDistance);
        Gizmos.color = Color.red;
        GizmosExtensions.DrawWireCircle(transform.position, _pulseDamageRadius);
        Gizmos.color = gizmosColor;
    }
}