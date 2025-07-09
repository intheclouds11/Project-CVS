using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

public class FirstBossEncounter : MonoBehaviour
{
    [Header("Base Mechanics")]
    [SerializeField]
    private float _postAttackDelay = 2f;
    [SerializeField]
    private float _AOEAgroRadius = 3f;
    [SerializeField]
    private float _AOEStartDelay = 3f;
    [SerializeField]
    private float _AOEChargeDuration = 2f;
    [SerializeField]
    private int _AOEDamage = 1;
    [SerializeField]
    private Knockback _AOEKnockback;
    [SerializeField]
    private float _AOEDuration = 3f;
    [SerializeField]
    private float _AOEDamageRadius = 5f;

    [Header("FX")]
    [SerializeField]
    private AudioClip _AOEZoneEnteredSFX;
    [SerializeField]
    private GameObject _AOEChargeVFX;
    [SerializeField]
    private AudioClip _AOEChargeSFX;
    [SerializeField]
    private float _AOEChargeVolume = 0.9f;
    [SerializeField]
    private GameObject _AOEAttackVFX;
    [SerializeField]
    private AudioClip _AOEAttackSFX;
    [SerializeField]
    private float _AOEAttackVolume = 0.9f;
    [SerializeField]
    private float _AOEImpulseVelocity = 0.3f;
    [SerializeField]
    private float _AOEImpulseRate = 1f;

    private float _distToPlayer;
    private bool _isPerformingAOE;
    private bool _isShootingProjectiles;
    private float _lastImpulseTime;
    private PlayerController _player;
    private Animator _animator;
    private CinemachineImpulseSource _impulseSource;
    private AudioSource _AOEChargeAudio;
    private AudioSource _AOEAttackAudio;


    public void EnteredBossZone()
    {
        enabled = true;
        _animator = GetComponent<Animator>();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _player = GameManager.Instance.Player1;
    }

    private void Update()
    {
        _distToPlayer = Vector3.Distance(transform.position, _player.transform.position);

        if (!_isPerformingAOE && !_isShootingProjectiles && _distToPlayer <= _AOEAgroRadius)
        {
            StartCoroutine(AOECoroutine());
        }
    }

    private IEnumerator AOECoroutine()
    {
        _isPerformingAOE = true;
        // AudioSource.PlayClipAtPoint(_AOEZoneEnteredSFX, transform.position);

        yield return new WaitForSeconds(_AOEStartDelay);

        _animator.SetTrigger("AOECharge");
        if (_AOEChargeVFX) _AOEChargeVFX.SetActive(true);
        _AOEChargeAudio = AudioManager.Instance.PlaySoundLoop(transform, _AOEChargeSFX, false, _AOEChargeVolume);

        yield return new WaitForSeconds(_AOEChargeDuration);

        if (_AOEChargeVFX) _AOEChargeVFX.SetActive(false);
        if (_AOEAttackVFX) _AOEAttackVFX.SetActive(true);
        _animator.SetBool("IsPerformingAOE", true);
        _AOEChargeAudio.Stop();
        _AOEAttackAudio = AudioManager.Instance.PlaySoundLoop(transform, _AOEAttackSFX, false, _AOEAttackVolume);

        var startTime = Time.time;
        while (startTime + _AOEDuration >= Time.time)
        {
            if (!_player.IsDashing && _distToPlayer <= _AOEDamageRadius)
            {
                var knockBackDir = (_player.transform.position - transform.position).normalized;
                _player.Health.TakeDamage(1, knockBackDir, _AOEKnockback);
            }

            if (Time.time >= _lastImpulseTime + _AOEImpulseRate)
            {
                _impulseSource.GenerateImpulse(new Vector3(0f, _AOEImpulseVelocity, 0f));
                _lastImpulseTime = Time.time;
            }

            yield return null;
        }

        _animator.SetBool("IsPerformingAOE", false);
        _AOEAttackAudio.Stop();
        if (_AOEAttackVFX) _AOEAttackVFX.SetActive(false);

        yield return new WaitForSeconds(_postAttackDelay);

        _isPerformingAOE = false;
    }

    private void OnDrawGizmosSelected()
    {
        var gizmosColor = Gizmos.color;
        Gizmos.color = Color.white;
        GizmosExtensions.DrawWireCircle(transform.position, _AOEAgroRadius);
        Gizmos.color = Color.red;
        GizmosExtensions.DrawWireCircle(transform.position, _AOEDamageRadius);
        Gizmos.color = gizmosColor;
    }
}