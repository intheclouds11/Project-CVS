using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

public class PulserProjectile : Projectile
{
    [Header("Pulser Settings")]
    [SerializeField]
    private LayerMask _layersToHit;
    [SerializeField]
    private int _deflectedPulseDamage = 200;
    [SerializeField]
    private float _pulseDamageRadius = 2.5f;
    [SerializeField]
    private float _pulseStartDelay = 0.3f;

    [Header("Pulser FX")]
    [SerializeField]
    private AudioClip _pulseSFX;
    [SerializeField]
    private AudioClip _pulseHitSFX;
    [SerializeField]
    private Animator _lightAnimator;
    [SerializeField]
    private GameObject _pulseVFX;

    private bool _isPulsing;
    private readonly Collider[] _overlapColliders = new Collider[1];


    protected override void Update()
    {
        base.Update();
        if (_isPulsing || !_abilityEnabled) return;

        StartCoroutine(PulseCoroutine());
    }

    private IEnumerator PulseCoroutine()
    {
        _isPulsing = true;
        yield return new WaitForSeconds(_pulseStartDelay);

        _pulseVFX.SetActive(false);
        _pulseVFX.SetActive(true);
        _lightAnimator.SetTrigger("Pulse");
        _animator.SetTrigger("Alerted");
        _abilityAudio = AudioManager.Instance.PlaySound(transform, _pulseSFX, true, false, 0.7f);

        while (true)
        {
            var overlapCount = Physics.OverlapSphereNonAlloc(transform.position, _pulseDamageRadius, _overlapColliders, _layersToHit);
            if (overlapCount > 0 && (_overlapColliders[0].CompareTag("Player") || _overlapColliders[0].CompareTag("Boss")))
            {
                var playerHit = _overlapColliders[0].GetComponentInParent<PlayerController>();
                var bossHit = _overlapColliders[0].GetComponentInParent<FirstBossEncounter>();

                if (playerHit && !playerHit.Health.IsInvincible() && !playerHit.IsDashing)
                {
                    var knockBackDir = (playerHit.transform.position - transform.position).normalized;
                    playerHit.Health.TakeDamage(2, knockBackDir, _knockback);
                    AudioManager.Instance.PlaySound(playerHit.transform, _pulseHitSFX, true, false, 0.9f);
                }
                else if (_isDeflected && bossHit)
                {
                    bossHit.Health.TakeDamage(_deflectedPulseDamage, Vector3.zero, null);
                }

                _abilityEnabled = false;
                yield break;
            }

            yield return null;
        }
    }


    protected override void OnReturnToPool()
    {
        base.OnReturnToPool();
        _isPulsing = false;
        _pulseVFX.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        var gizmosColor = Gizmos.color;
        Gizmos.color = Color.red;
        GizmosExtensions.DrawWireCircle(transform.position, _pulseDamageRadius);
        Gizmos.color = gizmosColor;
    }
}