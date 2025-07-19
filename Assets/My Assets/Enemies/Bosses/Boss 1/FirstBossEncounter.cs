using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using Random = UnityEngine.Random;

public class FirstBossEncounter : MonoBehaviour
{
    [Header("Projectiles Settings")]
    [SerializeField]
    private List<ProjectilePattern> _phase1ProjectilePatterns;
    [SerializeField]
    private List<Transform> _projectileSpawnPoints;
    private MultiProjectilePool _multiProjectilePool;

    [Header("AOE Settings")]
    [SerializeField]
    private float _postAOEDelay = 5f;
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

    [Header("AOE FX")]
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
    private float _AOEImpulseDuration = 1f;
    [SerializeField]
    private float _AOEImpulseRate = 1f;

    [Header("General FX")]
    [SerializeField]
    private List<AudioClip> _hitSFX;
    [SerializeField]
    private AudioClip _hurtSFX;
    [SerializeField]
    private float _hurtImpulseVelocity = 0.2f;
    [SerializeField]
    private float _hurtImpulseDuration = 2f;
    [SerializeField]
    private int _playHurtSFXHealthInterval = 400;
    private int _prevHurtSFXHealth;

    public Health Health { get; private set; }

    private float _distToPlayer;
    private bool _isPerformingAOE;
    private float _lastImpulseTime;
    private ProjectilePattern _currentProjectilePattern;
    private PlayerController _player;
    private Animator _animator;
    private CinemachineImpulseSource _impulseSource;
    private AudioSource _projectileChargeAudio;
    private AudioSource _projectileCooldownAudio;
    private AudioSource _AOEChargeAudio;
    private AudioSource _AOEAttackAudio;


    private void Start()
    {
        Health = GetComponent<Health>();
        Health.Died += OnDied;
        Health.DamageTaken += OnDamageTaken;
        _prevHurtSFXHealth = Health.CurrentHealth;
        _animator = GetComponent<Animator>();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _multiProjectilePool = GetComponent<MultiProjectilePool>();
        _player = GameManager.Instance.Player1;
        enabled = false;
    }

    public void EnteredBossZone()
    {
        StartCoroutine(ProjectilesCoroutine(_phase1ProjectilePatterns[0]));
        enabled = true;
    }

    private void Update()
    {
        _distToPlayer = Vector3.Distance(transform.position, _player.transform.position);

        if (!_currentProjectilePattern)
        {
            if (!_isPerformingAOE && _distToPlayer <= _AOEAgroRadius)
            {
                StartCoroutine(AOECoroutine());
                return;
            }

            StartCoroutine(ProjectilesCoroutine(SelectRandomPattern(1)));
        }
    }

    private ProjectilePattern SelectRandomPattern(int phase)
    {
        if (phase == 1)
        {
            return _phase1ProjectilePatterns[Random.Range(0, _phase1ProjectilePatterns.Count)];
        }

        if (phase == 2)
        {
            Debug.LogWarning($"Phase {phase} does not exist yet");
            return null;
        }

        Debug.LogWarning($"Phase {phase} not implemented");
        return null;
    }

    private IEnumerator ProjectilesCoroutine(ProjectilePattern pattern)
    {
        // Debug.Log($"Start Projectile Charge", _projectileChargeAudio);
        _currentProjectilePattern = pattern;
        _projectileChargeAudio = AudioManager.Instance.PlaySound(transform, pattern.ChargeSFX, true, false, pattern.ChargeVolume);
        var spawnPoint = _projectileSpawnPoints[0];

        yield return new WaitForSeconds(pattern.StartDelay);

        _projectileChargeAudio.Stop();
        _projectileChargeAudio = null;

        int count = 0;
        while (count < pattern.FireCount)
        {
            var dir = (_player.transform.position - spawnPoint.position).normalized;
            var proj = pattern.Spawn(_multiProjectilePool, spawnPoint, dir);
            count++;
            yield return new WaitForSeconds(pattern.FireRate);
        }
        
        pattern.OnPatternEnd();

        // Debug.Log($"Start Projectile cooldown", _projectileCooldownAudio);
        _projectileCooldownAudio =
            AudioManager.Instance.PlaySound(transform, pattern.CooldownSFX, true, false, pattern.CooldownVolume);
        yield return new WaitForSeconds(pattern.EndDelay);

        _projectileCooldownAudio = null;
        _currentProjectilePattern = null;
    }

    private IEnumerator AOECoroutine()
    {
        _isPerformingAOE = true;
        AudioManager.Instance.PlaySound(transform, _AOEZoneEnteredSFX, true, false, 1f, 0.7f);

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
            if (_player.Health.IsAlive() && !_player.IsDashing && _distToPlayer <= _AOEDamageRadius)
            {
                var knockBackDir = (_player.transform.position - transform.position).normalized;
                _player.Health.TakeDamage(_AOEDamage, knockBackDir, _AOEKnockback);
            }

            if (Time.time >= _lastImpulseTime + _AOEImpulseRate)
            {
                _impulseSource.ImpulseDefinition.ImpulseDuration = _AOEImpulseDuration;
                _impulseSource.GenerateImpulseWithVelocity(new Vector3(0f, _AOEImpulseVelocity, 0f));
                _lastImpulseTime = Time.time;
            }

            yield return null;
        }

        _animator.SetBool("IsPerformingAOE", false);
        _AOEAttackAudio.Stop();
        if (_AOEAttackVFX) _AOEAttackVFX.SetActive(false);

        yield return new WaitForSeconds(_postAOEDelay);

        _isPerformingAOE = false;
    }

    private void OnDamageTaken(Vector3 arg1, Knockback arg2)
    {
        var pitch = Random.Range(0.9f, 1.1f);
        AudioManager.Instance.PlaySound(transform, _hitSFX[Random.Range(0, _hitSFX.Count)], true, false, 1f, pitch);

        if (_prevHurtSFXHealth - Health.CurrentHealth >= _playHurtSFXHealthInterval)
        {
            AudioManager.Instance.PlaySound(transform, _hurtSFX, true, false, 1f, 1f);
            _impulseSource.ImpulseDefinition.ImpulseDuration = _hurtImpulseDuration;
            _impulseSource.GenerateImpulseWithVelocity(new Vector3(0f, _hurtImpulseVelocity, 0f));

            _prevHurtSFXHealth = Health.CurrentHealth;
        }
    }

    private void OnDied(GameObject obj)
    {
        gameObject.SetActive(false);
        _AOEChargeAudio?.Stop();
        _AOEAttackAudio?.Stop();
        _projectileChargeAudio?.Stop();
        _projectileCooldownAudio?.Stop();
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