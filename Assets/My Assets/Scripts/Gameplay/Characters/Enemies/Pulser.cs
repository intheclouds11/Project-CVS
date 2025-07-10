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

    private float _lastPulseCompleteTime;
    private bool _isPulsing;


    protected override void OnDied(GameObject obj)
    {
        base.OnDied(obj);
    }

    private void Update()
    {
        _distToPlayer = Vector3.Distance(transform.position, _player.transform.position);

        if (_isPulsing || !_player.Health.IsAlive()) return;

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
        else if (_distToPlayer <= _pulseTriggerDistance && Time.time >= _lastPulseCompleteTime + _pulseCooldownDuration)
        {
            StartCoroutine(PulseCoroutine());
        }
    }

    private IEnumerator PulseCoroutine()
    {
        _isPulsing = true;
        _aiFollower.canMove = false;
        _pulseVFX.SetActive(false);
        _pulseVFX.SetActive(true);
        _animator.SetTrigger("Alerted");

        _agroAudio.Stop();
        _agroAudio = null;
        _abilityStartAudio = AudioManager.Instance.PlaySound(transform, _abilityStartSFX, true, false, 0.7f);

        yield return new WaitForSeconds(_pulseDelayDuration);
        
        var startTime = Time.time;

        while (startTime + _pulseDuration >= Time.time)
        {
            if (_isInterruptable && _isGettingKnockedBack)
            {
                _pulseVFX.SetActive(false);
                _abilityStartAudio.Stop();
                _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
                _isPulsing = false;
                _lastPulseCompleteTime = Time.time;
                yield break;
            }
            
            if (!_player.IsDashing && _distToPlayer <= _pulseDamageRadius)
            {
                var knockBackDir = (_player.transform.position - transform.position).normalized;
                _player.Health.TakeDamage(_pulseDamage, knockBackDir, _knockback);
                StartCoroutine(DamagedPlayerCoroutine());
            }

            yield return null;
        }

        _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);

        _aiFollower.canMove = true;
        _isPulsing = false;
        _lastPulseCompleteTime = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            var playerHit = other.GetComponent<PlayerController>();
            if (playerHit)
            {
                var knockBackDir = (playerHit.transform.position - transform.position).normalized;
                playerHit.Health.TakeDamage(_baseDamage, knockBackDir, _knockback);
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
        GizmosExtensions.DrawWireCircle(transform.position, _pulseTriggerDistance);
        Gizmos.color = Color.red;
        GizmosExtensions.DrawWireCircle(transform.position, _pulseDamageRadius);
        Gizmos.color = gizmosColor;
    }
}