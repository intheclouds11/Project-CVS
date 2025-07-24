using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utils;
using Random = UnityEngine.Random;

public class FirstBossEncounter : MonoBehaviour
{
    [SerializeField]
    private float _phase2HealthRatio = 0.5f;
    [Header("Projectiles Settings")]
    [SerializeField]
    private List<ProjectilePattern> _phase1ProjectilePatterns;
    [SerializeField]
    private List<Transform> _projectileSpawnPoints;

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
    [SerializeField]
    private Color _phase2Color;
    [SerializeField]
    private float _phase2ColorTransitionDuration = 1.5f;
    [SerializeField]
    private MeshRenderer _bodyMesh;
    
    [Header("Debug")]
    [SerializeField]
    private Slider _phase1HealthBar;
    [SerializeField]
    private Slider _phase2HealthBar;

    public Health Health { get; private set; }

    private int _phase1RemainingHealth;
    private bool _hasEnteredPhase2;
    private float _distToPlayer;
    private bool _isPerformingAOE;
    private float _lastImpulseTime;
    private ProjectilePattern _currentPattern;
    private MultiProjectilePool _multiProjectilePool;
    private PlayerController _player;
    private Animator _animator;
    private CinemachineImpulseSource _impulseSource;
    private AudioSource _projectileChargeAudio;
    private AudioSource _projectileCooldownAudio;
    private AudioSource _AOEChargeAudio;
    private AudioSource _AOEAttackAudio;
    private AudioSource _hurtAudio;
    private Coroutine _AOECoroutine;
    private Coroutine _projectileCoroutine;


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

    private void OnDisable()
    {
        foreach (var phase1ProjectilePattern in _phase1ProjectilePatterns)
        {
            phase1ProjectilePattern.Init();
        }

        if (_AOEAttackAudio) _AOEAttackAudio.Stop();
        if (_AOEChargeAudio) _AOEChargeAudio.Stop();
        if (_projectileCooldownAudio) _projectileCooldownAudio.Stop();
        if (_projectileChargeAudio) _projectileChargeAudio.Stop();
    }

    public void EnteredBossZone()
    {
        enabled = true;
        if (Application.isEditor)
        {
            _phase1HealthBar.transform.parent.gameObject.SetActive(true);
            _phase1HealthBar.maxValue = Health.GetMaxHealth - Health.GetMaxHealth * _phase2HealthRatio;
            _phase1HealthBar.value = _phase1HealthBar.maxValue;
            _phase2HealthBar.maxValue = Health.GetMaxHealth * _phase2HealthRatio;
            _phase2HealthBar.value = _phase2HealthBar.maxValue;
        }
        else
        {
            _phase1HealthBar.transform.parent.gameObject.SetActive(false);
        }

        _projectileCoroutine = StartCoroutine(ProjectilesCoroutine(_phase1ProjectilePatterns[0]));
    }

    private void Update()
    {
        _distToPlayer = GameManager.Instance.GetDistanceFromPlayer(transform);

        if (!_currentPattern)
        {
            if (!_isPerformingAOE && _distToPlayer <= _AOEAgroRadius)
            {
                _AOECoroutine = StartCoroutine(AOECoroutine());
                return;
            }

            _projectileCoroutine = StartCoroutine(ProjectilesCoroutine(SelectRandomPattern(1)));
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
        _currentPattern = pattern;

        while (_currentPattern)
        {
            _projectileChargeAudio = AudioManager.Instance.PlaySound(transform, _currentPattern.ChargeSFX, true, false,
                _currentPattern.ChargeVolume, _currentPattern.ChargePitch);

            _currentPattern.StartChargeVFX(_projectileSpawnPoints);

            yield return new WaitForSeconds(_currentPattern.StartDelay);

            _projectileChargeAudio.Stop();
            _projectileChargeAudio = null;

            int count = 0;
            while (count < _currentPattern.FireCount)
            {
                count++;
                _currentPattern.Spawn(_multiProjectilePool, _projectileSpawnPoints);
                yield return new WaitForSeconds(_currentPattern.TimeBetweenShots);
            }

            _currentPattern.Init();

            if (!_currentPattern.FollowupPattern)
            {
                _projectileCooldownAudio = AudioManager.Instance.PlaySound(transform, _currentPattern.CooldownSFX, true, false,
                    _currentPattern.CooldownVolume);
            }

            yield return new WaitForSeconds(_currentPattern.EndDelay);

            _currentPattern = _currentPattern.FollowupPattern;
        }

        _projectileCooldownAudio = null;
        _currentPattern = null;
        _projectileCoroutine = null;
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
        _AOECoroutine = null;
    }

    private void OnDamageTaken(Vector3 arg1, Knockback arg2)
    {
        var pitch = Random.Range(0.9f, 1.1f);
        AudioManager.Instance.PlaySound(transform, _hitSFX[Random.Range(0, _hitSFX.Count)], true, false, 1f, pitch);

        if (_prevHurtSFXHealth - Health.CurrentHealth >= _playHurtSFXHealthInterval)
        {
            if (!_hurtAudio || !_hurtAudio.isPlaying)
                _hurtAudio = AudioManager.Instance.PlaySound(transform, _hurtSFX, true, false, 1f, 1f);
            _impulseSource.ImpulseDefinition.ImpulseDuration = _hurtImpulseDuration;
            _impulseSource.GenerateImpulseWithVelocity(new Vector3(0f, _hurtImpulseVelocity, 0f));

            _prevHurtSFXHealth = Health.CurrentHealth;
        }

        _phase1RemainingHealth = (int) (Health.CurrentHealth - Health.GetMaxHealth * _phase2HealthRatio);
        if (_phase1RemainingHealth > 0)
        {
            _phase1HealthBar.value = _phase1RemainingHealth;
        }
        else
        {
            _phase1HealthBar.value = 0f;
            _phase2HealthBar.value = Health.CurrentHealth;

            if (!_hasEnteredPhase2)
            {
                _hasEnteredPhase2 = true;
                StartCoroutine(MeshTransition());
            }
        }
    }

    private void OnDied(GameObject obj)
    {
        if (_projectileCoroutine != null) StopCoroutine(_projectileCoroutine);
        if (_AOECoroutine != null) StopCoroutine(_AOECoroutine);
        _AOEChargeAudio?.Stop();
        _AOEAttackAudio?.Stop();
        _projectileChargeAudio?.Stop();
        _projectileCooldownAudio?.Stop();
        _phase1HealthBar.transform.parent.gameObject.SetActive(false);
    }

    private IEnumerator MeshTransition()
    {
        var startTime = Time.time;
        while (Time.time < startTime + _phase2ColorTransitionDuration)
        {
            _bodyMesh.material.color =
                Color.Lerp(_bodyMesh.material.color, _phase2Color, Time.deltaTime / _phase2ColorTransitionDuration);
            yield return null;
        }

        _bodyMesh.material.color = _phase2Color;
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