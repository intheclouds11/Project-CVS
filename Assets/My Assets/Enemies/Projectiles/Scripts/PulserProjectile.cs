using System.Collections;
using UnityEngine;

public class PulserProjectile : Projectile
{
    [SerializeField]
    private float _pulseTriggerDistance = 2.5f;
    [SerializeField]
    private float _pulseDamageRadius = 2.5f;
    [SerializeField]
    private float _pulseDuration = 0.3f;
    [SerializeField]
    private GameObject _pulseVFX;

    private bool _isPulsing;


    // private void Update()
    // {
    //     _distToPlayer = Vector3.Distance(transform.position, _player.transform.position);
    //
    //     if (_isPulsing) return;
    //
    //     if (_distToPlayer <= _pulseTriggerDistance)
    //     {
    //         StartCoroutine(PulseCoroutine());
    //     }
    // }
    
    protected override void OnReturnToPool()
    {
        base.OnReturnToPool();
    }
    //
    // private IEnumerator PulseCoroutine()
    // {
    //     _isPulsing = true;
    //     _pulseVFX.SetActive(false);
    //     _pulseVFX.SetActive(true);
    //     _animator.SetTrigger("Alerted");
    //
    //     _agroAudio.Stop();
    //     _agroAudio = null;
    //     _abilityStartAudio = AudioManager.Instance.PlaySound(transform, _abilityStartSFX, true, false, 0.7f);
    //
    //     yield return new WaitForSeconds(_pulseDelayDuration);
    //     
    //     var startTime = Time.time;
    //
    //     while (startTime + _pulseDuration >= Time.time)
    //     {
    //         if (_isInterruptable && _isGettingKnockedBack)
    //         {
    //             _pulseVFX.SetActive(false);
    //             _abilityStartAudio.Stop();
    //             _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
    //             _isPulsing = false;
    //             _lastPulseCompleteTime = Time.time;
    //             yield break;
    //         }
    //         
    //         if (!_player.IsDashing && _distToPlayer <= _pulseDamageRadius)
    //         {
    //             var knockBackDir = (_player.transform.position - transform.position).normalized;
    //             _player.Health.TakeDamage(_pulseDamage, knockBackDir, _knockback);
    //             StartCoroutine(DamagedPlayerCoroutine());
    //         }
    //
    //         yield return null;
    //     }
    //
    //     _agroAudio = AudioManager.Instance.PlaySoundLoop(transform, _agroSFX, true, 1f, _agroPitch);
    //
    //     _aiFollower.canMove = true;
    //     _isPulsing = false;
    //     _lastPulseCompleteTime = Time.time;
    // }
}