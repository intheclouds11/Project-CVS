using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

public class DasherProjectile : Projectile
{
    [Header("Dasher Settings")]
    [SerializeField]
    private bool useAbilityAfterDistanceTraveled;
    [field: SerializeField, ShowIf(nameof(useAbilityAfterDistanceTraveled))]
    private float _distanceToTravel = 2.5f;
    [field: SerializeField, HideIf(nameof(useAbilityAfterDistanceTraveled))]
    private float _dashTriggerDistance = 4f;
    [SerializeField]
    private float _dashSpeed = 6f;
    [SerializeField]
    private float _alertStartDelay = 0.5f;
    [SerializeField]
    private float _dashStartDelay = 0.5f;

    [Header("Dasher FX")]
    [SerializeField]
    private AudioClip _dashAlertSFX;
    [SerializeField]
    private AudioClip _dashSFX;

    private bool _isDashing;
    private AudioSource _dashingAudio;
    private AudioSource _dashAlertAudio;
    private float _distTraveled;
    private Vector3 _lastPos;


    protected override void OnEnable()
    {
        base.OnEnable();
        _lastPos = transform.position;
    }

    protected override void Update()
    {
        if (_isDashing || !_abilityEnabled) return;

        if (useAbilityAfterDistanceTraveled)
        {
            _distTraveled += Vector3.Distance(transform.position, _lastPos);
            if (_distTraveled >= _distanceToTravel)
            {
                StartCoroutine(DashCoroutine());
            }

            _lastPos = transform.position;
        }
        else
        {
            _distToPlayer = Vector3.Distance(transform.position, _player.transform.position);
            if (_distToPlayer <= _dashTriggerDistance)
            {
                StartCoroutine(DashCoroutine());
            }
        }
    }

    // short delay, halt, play alerted SFX and animation, short delay, dash towards player and play dashSFX
    private IEnumerator DashCoroutine()
    {
        _isDashing = true;

        if (!useAbilityAfterDistanceTraveled)
        {
            yield return new WaitForSeconds(_alertStartDelay);
        }

        Rb.linearVelocity = Vector3.zero;
        _animator.SetTrigger("Alerted");
        _dashAlertAudio = AudioManager.Instance.PlaySound(transform, _dashAlertSFX, true, false, 1f, 1.2f);

        yield return new WaitForSeconds(_dashStartDelay);

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
        if (_dashAlertAudio) _dashAlertAudio = null;
        if (_dashingAudio) _dashingAudio = null;
    }
}