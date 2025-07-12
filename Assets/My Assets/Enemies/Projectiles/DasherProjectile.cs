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
    [SerializeField]
    private GameObject _dashAlertVFX;

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
        _isDashing = true;
        Rb.linearVelocity = Vector3.zero;
        _animator.SetTrigger("Alerted");
        _dashAlertAudio = AudioManager.Instance.PlaySound(transform, _dashAlertSFX, true, false, 1f, 1.2f);
        _dashAlertVFX.SetActive(true);
        
        yield return new WaitForSeconds(_dashDelayDuration);

        _dashingAudio = AudioManager.Instance.PlaySound(transform, _dashSFX, true, false, 1f, 1.1f);

        while (true)
        {
            var dir = (_player.transform.position - transform.position).normalized;
            var targetPos = _player.LookAt.position + dir * 4f;
            transform.position = Vector3.Lerp(transform.position, targetPos, _dashSpeed * Time.deltaTime);
            yield return null;
        }
    }

    protected override void OnReturnToPool()
    {
        base.OnReturnToPool();
        // if (_dashAlertAudio) _dashAlertAudio.Stop();
        // if (_dashingAudio) _dashingAudio.Stop();
    }
}