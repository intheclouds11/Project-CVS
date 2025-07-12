using System.Collections;
using UnityEngine;

public class DasherProjectile : Projectile
{
    [SerializeField]
    private float _dashTriggerDistance = 2.5f;
    [SerializeField]
    private float _dashSpeed = 6f;
    [SerializeField]
    private float _dashDelayDuration = 1f;
    
    [SerializeField]
    private AudioClip _dashAlertSFX;
    [SerializeField]
    private AudioClip _dashSFX;

    private bool _isDashing;
    private AudioSource _dashingAudio;
    private AudioSource _dashAlertAudio;
    

    private void Update()
    {
        if (_isDashing) return;

        _distToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        
        if (_distToPlayer <= _dashTriggerDistance)
        {
            StartCoroutine(DashCoroutine());
        }
    }

    private IEnumerator DashCoroutine()
    {
        _animator.SetTrigger("Alerted");
        
        _dashAlertAudio = AudioManager.Instance.PlaySound(transform, _dashAlertSFX, true, false, 1f);
        yield return new WaitForSeconds(_dashDelayDuration);

        _dashingAudio = AudioManager.Instance.PlaySound(transform, _dashSFX, true, false, 1f);

        while (true)
        {
            var dir = (_player.transform.position - transform.position).normalized;
            var targetPos = _player.transform.position + dir * 4f;
            transform.position = Vector3.Lerp(transform.position, targetPos, _dashSpeed * Time.deltaTime);
            yield return null;
        }
    }

    protected override void OnReturnToPool()
    {
        base.OnReturnToPool();
        if (_dashAlertAudio) _dashAlertAudio.Stop();
        if (_dashingAudio) _dashingAudio.Stop();
    }
}