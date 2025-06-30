using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

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
    private int _alertAudioSourceIndex;
    private int _dashingAudioSourceIndex;
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
        AudioManager.Instance.StopSound(_alertAudioSourceIndex);
        AudioManager.Instance.StopSound(_dashingAudioSourceIndex);
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
        _alertAudioSourceIndex = AudioManager.Instance.PlaySound(transform, _alertSFX, true, false, 0.7f, 1.3f);
        _animator.SetTrigger("Alerted");

        yield return new WaitForSeconds(_dashDelayDuration);

        _dashingAudioSourceIndex = AudioManager.Instance.PlaySound(transform, _dashingSFX, true, false, 0.7f, 1.3f);

        float startTime = Time.time;
        var dir = (_player.transform.position - transform.position).normalized;
        var targetPos = _player.transform.position + dir;

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