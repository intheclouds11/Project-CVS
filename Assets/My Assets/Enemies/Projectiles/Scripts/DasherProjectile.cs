using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

public class DasherProjectile : Projectile
{
    [Header("Dasher Settings")]
    [SerializeField]
    private bool useAbilityAfterDistanceTraveled;
    [field: SerializeField, ShowIf(nameof(useAbilityAfterDistanceTraveled))]
    private float _distanceToTravel = 2.5f;
    [SerializeField]
    private float _dashTriggerDistance = 4f;
    [SerializeField]
    private float _dashSpeed = 6f;
    [SerializeField]
    private float _dashStartDelay = 0.5f;

    [Header("Dasher FX")]
    [SerializeField]
    private AudioClip _dashAlertSFX;
    [SerializeField]
    private AudioClip _dashSFX;

    private bool _isDashing;
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
        base.Update();
        if (_isDashing || !_abilityEnabled) return;

        if (useAbilityAfterDistanceTraveled)
        {
            _distTraveled += Vector3.Distance(transform.position, _lastPos);
            if (_distTraveled >= _distanceToTravel)
            {
                StartCoroutine(DashCoroutine());
                return;
            }

            _lastPos = transform.position;
        }

        _distToPlayer = GameManager.Instance.GetDistanceFromPlayer(transform);
        if (_distToPlayer <= _dashTriggerDistance)
        {
            StartCoroutine(DashCoroutine());
        }
    }

    // short delay, halt, play alerted SFX and animation, short delay, dash towards player and play dashSFX
    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        tag = "Deflected";

        Rb.linearVelocity = Vector3.zero;
        _animator.SetTrigger("Alerted");
        _dashAlertAudio = AudioManager.Instance.PlaySound(transform, _dashAlertSFX, true, false, 1f, 1.2f);

        yield return new WaitForSeconds(_dashStartDelay);

        _abilityAudio = AudioManager.Instance.PlaySound(transform, _dashSFX, true, false, 1f, 1.1f);

        var dir = (_player.transform.position - transform.position).normalized;
        var dirNoPitch = new Vector3(dir.x, 0f, dir.z).normalized;
        Rb.linearVelocity = dirNoPitch * _dashSpeed;
    }

    protected override void DamagePlayer(PlayerController playerHit, bool usingAbility)
    {
        base.DamagePlayer(playerHit, _isDashing);
    }

    protected override void OnReturnToPool()
    {
        base.OnReturnToPool();
        _isDashing = false;
        if (_dashAlertAudio) _dashAlertAudio = null;
    }

    private void OnDrawGizmosSelected()
    {
        var gizmosColor = Gizmos.color;
        Gizmos.color = Color.red;
        GizmosExtensions.DrawWireCircle(transform.position, _dashTriggerDistance);
        Gizmos.color = gizmosColor;
    }
}