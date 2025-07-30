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
    private MeshRenderer _telegraphIndicatorMesh;
    [SerializeField]
    private float _telegraphAlphaTarget = 1f;
    [SerializeField]
    private Animator _lightAnimator;
    [SerializeField]
    private AudioClip _pulseAttackSFX;
    [SerializeField]
    private AudioClip _pulseHitSFX;

    private float _lastPulseCompleteTime;
    private Vector3 _scaleTarget;
    private AudioSource _pulseAttackAudio;
    private Coroutine _pulseCoroutine;


    protected override void Awake()
    {
        base.Awake();

        _scaleTarget = Vector3.one * _pulseDamageRadius * 2;
        _telegraphIndicatorMesh.transform.localScale = Vector3.zero;
    }


    protected override void Update()
    {
        base.Update();

        if (!IsAggroed || !GameManager.Instance.EnemyAIEnabled || _usingAbility || !_aiFollower.canMove ||
            !_player.Health.IsAlive()) return;

        if (_distToPlayer <= _pulseTriggerDistance && Time.time >= _lastPulseCompleteTime + _pulseCooldownDuration)
        {
            _pulseCoroutine = StartCoroutine(PulseCoroutine());
        }
    }

    private IEnumerator PulseCoroutine()
    {
        _aiFollower.canMove = false;
        Health.Invincible = true;
        _usingAbility = true;
        _pulseVFX.SetActive(false);
        _pulseVFX.SetActive(true);
        _lightAnimator.SetTrigger("Pulse");
        _animator.SetTrigger("PulseWindup");

        if (_aggroAudio) _aggroAudio.Stop();
        _abilityStartAudio = AudioManager.Instance.PlaySound(transform, _abilityStartSFX, true, false, 0.7f);

        if (_telegraphCoroutine != null) StopCoroutine(_telegraphCoroutine);
        _telegraphCoroutine = StartCoroutine(TelegraphIndicatorCoroutine());

        yield return new WaitForSeconds(_pulseDelayDuration);

        _animator.SetBool("IsPulsing", true);
        _abilityStartAudio.Stop();
        _pulseAttackAudio = AudioManager.Instance.PlaySound(transform, _pulseAttackSFX, true, false, 0.7f);

        var startTime = Time.time;

        while (startTime + _pulseDuration >= Time.time)
        {
            if (_isInterruptable && IsGettingKnockedBack)
            {
                _animator.SetBool("IsPulsing", false);
                _usingAbility = false;
                Health.Invincible = false;
                _lastPulseCompleteTime = Time.time;
                _pulseVFX.SetActive(false);
                _aggroAudio = AudioManager.Instance.PlaySoundLoop(transform, _aggroSFX, true, 1f, _aggroPitch);
                yield break;
            }

            if (_player.Health.IsAlive() && !_player.Health.IsInvincible() && !_player.IsDashing &&
                _distToPlayer <= _pulseDamageRadius)
            {
                _damagePlayerKnockback.Direction = (_player.transform.position - transform.position).normalized;
                _player.Health.TakeDamage(_pulseDamage, _damagePlayerKnockback);
                AudioManager.Instance.PlaySound(_player.transform, _pulseHitSFX, true, false, 0.9f);
                OnDamagedPlayer();
            }

            yield return null;
        }

        _animator.SetBool("IsPulsing", false);
        _usingAbility = false;
        _lastPulseCompleteTime = Time.time;
        _aiFollower.canMove = true;
        Health.Invincible = false;
        _aggroAudio = AudioManager.Instance.PlaySoundLoop(transform, _aggroSFX, true, 1f, _aggroPitch);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (enabled && other.gameObject.CompareTag("Player"))
        {
            var playerHit = other.GetComponent<PlayerController>();
            if (playerHit)
            {
                _damagePlayerKnockback.Direction = (playerHit.transform.position - transform.position).normalized;
                playerHit.Health.TakeDamage(_baseDamage, _damagePlayerKnockback);
                OnDamagedPlayer();
            }
        }
    }

    private Coroutine _telegraphCoroutine;

    private IEnumerator TelegraphIndicatorCoroutine()
    {
        _telegraphIndicatorMesh.gameObject.SetActive(true);

        var startScale = _telegraphIndicatorMesh.transform.localScale;
        var startTime = Time.time;
        while (Time.time < startTime + _pulseDelayDuration)
        {
            _telegraphIndicatorMesh.transform.localScale = Vector3.MoveTowards(_telegraphIndicatorMesh.transform.localScale,
                _scaleTarget, _scaleTarget.magnitude * Time.deltaTime / _pulseDelayDuration);
            yield return null;
        }


        // yield return new WaitForSeconds(_pulseDuration);

        startTime = Time.time;
        while (Time.time < startTime + _pulseDuration)
        {
            _telegraphIndicatorMesh.transform.localScale = Vector3.MoveTowards(_telegraphIndicatorMesh.transform.localScale,
                startScale, _scaleTarget.magnitude * Time.deltaTime / _pulseDuration);

            yield return null;
        }

        _telegraphIndicatorMesh.transform.localScale = startScale;
        _telegraphIndicatorMesh.gameObject.SetActive(false);
    }

    protected override void OnDied(Knockback knockback)
    {
        base.OnDied(knockback);
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseVFX.SetActive(false);
        }

        if (_telegraphCoroutine != null) StopCoroutine(_telegraphCoroutine);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        var gizmosColor = Gizmos.color;
        Gizmos.color = Color.yellow;
        GizmosExtensions.DrawWireCircle(transform.position, _pulseTriggerDistance);
        Gizmos.color = Color.red;
        GizmosExtensions.DrawWireCircle(transform.position, _pulseDamageRadius);
        Gizmos.color = gizmosColor;
    }
}