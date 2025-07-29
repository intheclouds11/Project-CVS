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
    [SerializeField]
    private MeshRenderer _telegraphIndicatorMesh;

    private bool _isPulsing;
    private readonly Collider[] _overlapColliders = new Collider[1];
    private Vector3 _scaleTarget;


    protected override void Awake()
    {
        base.Awake();
        _scaleTarget = Vector3.one * _pulseDamageRadius * 2;
        _telegraphIndicatorMesh.transform.localScale = Vector3.zero;
    }

    protected override void Update()
    {
        base.Update();
        if (_isPulsing || !_abilityEnabled) return;

        StartCoroutine(PulseCoroutine());
    }

    private IEnumerator PulseCoroutine()
    {
        _isPulsing = true;
        StartCoroutine(TelegraphIndicatorCoroutine());
        yield return new WaitForSeconds(_pulseStartDelay);

        _pulseVFX.SetActive(false);
        _pulseVFX.SetActive(true);
        _lightAnimator.SetTrigger("Pulse");
        _animator.SetTrigger("Alerted");
        _abilityAudio = AudioManager.Instance.PlaySound(transform, _pulseSFX, true, false, 0.7f);

        while (true)
        {
            var overlapCount = Physics.OverlapSphereNonAlloc(transform.position, _pulseDamageRadius, _overlapColliders, _layersToHit);
            if (overlapCount > 0)
            {
                if (_overlapColliders[0].CompareTag("Player"))
                {
                    var playerHit = _overlapColliders[0].GetComponentInParent<PlayerController>();
                    if (!playerHit.Health.IsInvincible() && !playerHit.IsDashing)
                    {
                        var knockBackDir = (playerHit.transform.position - transform.position).normalized;
                        playerHit.Health.TakeDamage(2, knockBackDir, _knockback);
                        AudioManager.Instance.PlaySound(playerHit.transform, _pulseHitSFX, true, false, 0.9f);
                    }
                }
                else if (CompareTag("Deflected") && _overlapColliders[0].CompareTag("Boss"))
                {
                    var bossHit = _overlapColliders[0].GetComponentInParent<FirstBossEncounter>();
                    var hitInvincible = bossHit.Health.Invincible;
                    bossHit.Health.TakeDamage(_deflectedPulseDamage, Vector3.zero, null);
                    ReturnToPool(true, hitInvincible);
                }
            }

            yield return null;
        }
    }


    private IEnumerator TelegraphIndicatorCoroutine()
    {
        _telegraphIndicatorMesh.gameObject.SetActive(true);

        var startTime = Time.time;
        while (Time.time < startTime + _pulseStartDelay)
        {
            _telegraphIndicatorMesh.transform.localScale = Vector3.MoveTowards(_telegraphIndicatorMesh.transform.localScale,
                _scaleTarget, _scaleTarget.magnitude * Time.deltaTime / _pulseStartDelay);
            yield return null;
        }
    }

    protected override void OnReturnToPool()
    {
        base.OnReturnToPool();
        _isPulsing = false;
        _pulseVFX.SetActive(false);
        _telegraphIndicatorMesh.transform.localScale = Vector3.zero;
        _telegraphIndicatorMesh.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        var gizmosColor = Gizmos.color;
        Gizmos.color = Color.red;
        GizmosExtensions.DrawWireCircle(transform.position, _pulseDamageRadius);
        Gizmos.color = gizmosColor;
    }
}