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
            _agroAudio = AudioManager.Instance.PlaySound(transform, _agroSFX, true, true, 1f, _agroPitch);
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
        
        // float pitch = Random.Range(1.1f, 1.3f);
        _agroAudio.Stop();
        _agroAudio = null;
        _abilityStartAudio = AudioManager.Instance.PlaySound(transform, _abilityStartSFX, true, false, 0.7f);

        yield return new WaitForSeconds(_pulseDelayDuration);

        if (!_player.IsDashing && _distToPlayer <= _pulseDamageRadius)
        {
            _player.Health.TakeDamage(1);
        }

        yield return new WaitForSeconds(_pulseDuration);
        
        
        _agroAudio = AudioManager.Instance.PlaySound(transform, _agroSFX, true, true, 1f, _agroPitch);
        
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
                playerHit.Health.TakeDamage(1);
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