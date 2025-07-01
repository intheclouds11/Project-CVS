using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemyTorus : BaseEnemy
{
    [Header("Base Movement")]
    [SerializeField]
    private float _agroRange = 5f;
    [SerializeField]
    private float _moveSpeed = 1f;

    [Header("Dashing")]
    [SerializeField]
    private float _dashRange = 3f;
    [SerializeField]
    private float _dashSpeed = 6f;
    [SerializeField]
    private float _dashDuration = 0.5f;
    [SerializeField]
    private float _dashDelayDuration = 1f;
    [SerializeField]
    private float _dashCooldownDuration = 1.5f;

    [Header("FX")]
    [SerializeField]
    private AudioClip _alertSFX;
    [SerializeField]
    private AudioClip _dashingSFX;

    private Animator _animator;
    private bool _inAgroRange;
    private AudioSource _alertAudio;
    private AudioSource _dashingAudio;
    private float _lastDashCompleteTime;
    private bool _isDashing;


    protected override void Awake()
    {
        base.Awake();
        Health.Died += OnDied;
        _animator = GetComponent<Animator>();
    }

    protected override void OnDied(GameObject obj)
    {
        base.OnDied(obj);
        if (_alertAudio) _alertAudio.Stop();
        if (_dashingAudio) _dashingAudio.Stop();
    }

    private void Update()
    {
        if (_isDashing || !_player.Health.IsAlive()) return;

        var distToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        if (distToPlayer <= _agroRange)
        {
            if (distToPlayer <= _dashRange && Time.time >= _lastDashCompleteTime + _dashCooldownDuration)
            {
                StartCoroutine(DashCoroutine());
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, _moveSpeed * Time.deltaTime);
        }
    }

    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        float pitch = Random.Range(1.1f, 1.3f);
        _alertAudio = AudioManager.Instance.PlaySound(transform, _alertSFX, true, false, 0.7f, pitch);
        _animator.SetTrigger("Alerted");

        yield return new WaitForSeconds(_dashDelayDuration);

        _dashingAudio = AudioManager.Instance.PlaySound(transform, _dashingSFX, true, false, 0.7f, pitch);

        float startTime = Time.time;
        var dir = (_player.transform.position - transform.position).normalized;
        var targetPos = _player.transform.position + dir * 4f;

        while (startTime + _dashDuration >= Time.time)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, _moveSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(_dashDelayDuration * 0.5f);

        _isDashing = false;
        _lastDashCompleteTime = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        var playerHit = other.GetComponent<PlayerController>();
        if (playerHit)
        {
            playerHit.Health.TakeDamage(1);
        }
    }
}