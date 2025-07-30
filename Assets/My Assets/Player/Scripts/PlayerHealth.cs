using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlayerHealth : Health
{
    [Header("Player Health")]
    [SerializeField]
    private float _autoHealDelay = 2f;
    [SerializeField]
    private float _autoRecoverHPVisualsDuration = 3f;
    [SerializeField]
    private float _recoverHPVisualsDuration = 1f;
    [SerializeField]
    private float _invincibilityDuration = 0.5f;
    [SerializeField]
    private float _invincibilityFlashRate = 5f;
    [SerializeField]
    private float _damagedVignetteIntensity = 0.55f;
    [SerializeField]
    private float _damagedLowPassFreq = 5000f;
    [SerializeField]
    private float _damagedLowPassAdjustDuration = 2;
    [SerializeField]
    private float _damagedSaturation = -40f;
    [SerializeField]
    private float _damagedAberration = 1f;

    public bool IsDamaged => !Mathf.Approximately(_vignette.intensity.value, _startingVignetteIntensity);

    private float _lastDamageTime;
    private bool _wasInvincible;
    private float _startingVignetteIntensity;
    private float _startingSaturation;
    private float _startingChromaticAberration;
    private Volume _globalVolume;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;
    private ChromaticAberration _chromaticAberration;
    private PlayerController _player;
    private Coroutine _autoHealCoroutine;
    private Coroutine _healthFXCoroutine;


    protected override void Awake()
    {
        base.Awake();
        _player = GetComponent<PlayerController>();
        _globalVolume = FindAnyObjectByType<Volume>();
        _globalVolume.profile.TryGet(out _vignette);
        _globalVolume.profile.TryGet(out _colorAdjustments);
        _globalVolume.profile.TryGet(out _chromaticAberration);
        _startingVignetteIntensity = _vignette.intensity.value;
        _startingSaturation = _colorAdjustments.saturation.value;
        _startingChromaticAberration = _chromaticAberration.intensity.value;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (_autoHealCoroutine != null) return;

        if (CurrentHealth < _maxHealth && !IsInvincible() && !EnemyManager.Instance.AnyAggroedEnemies())
        {
            _autoHealCoroutine = StartCoroutine(AutoHealCoroutine());
            return;
        }

        if (IsInvincible())
        {
            _wasInvincible = true;
        }
        else if (_wasInvincible)
        {
            _wasInvincible = false;
            if (IsAlive())
            {
                _player.FadeMeshRenderers(true, 0.1f);
                _player.TogglePlayerTriggerCollider(true);
            }
        }
    }

    private IEnumerator AutoHealCoroutine()
    {
        yield return new WaitForSeconds(_autoHealDelay);
        RecoverHP(1, true);
        _autoHealCoroutine = null;
    }

    // todo: better way to retain reference to scene Volume?
    private void OnSceneLoaded(Scene loadedScene, LoadSceneMode arg1)
    {
        if (loadedScene.name.Equals("MainMenu")) return;
        _globalVolume = FindAnyObjectByType<Volume>();
        _globalVolume.profile.TryGet(out _vignette);
        _globalVolume.profile.TryGet(out _colorAdjustments);
        _globalVolume.profile.TryGet(out _chromaticAberration);
    }

    public override void TakeDamage(int damage, Knockback knockback, bool wasCritAttack = false)
    {
        if (GameManager.Instance.CurrentState is GameManager.GameState.Victory
            or GameManager.GameState.AwaitingWave or GameManager.GameState.GameOver) return;

        if (IsInvincible() || damage <= 0 || CurrentHealth <= 0) return;

        if (CurrentHealth == _maxHealth)
            AudioManager.Instance.AdjustMasterLowPass(_damagedLowPassFreq, _damagedLowPassAdjustDuration * 0.5f);

        int newHealth = CurrentHealth - damage;
        if (!GameManager.Instance.GodMode) CurrentHealth = newHealth;

        if (CurrentHealth > 0)
        {
            OnDamaged(knockback);
        }
        else
        {
            OnDied(knockback);
        }

        _lastDamageTime = Time.time;
    }

    protected override void OnDamaged(Knockback knockback)
    {
        base.OnDamaged(knockback);
        _player.FlashMeshRenderers(_invincibilityDuration, _invincibilityFlashRate);
        _player.TogglePlayerTriggerCollider(false);

        if (_healthFXCoroutine != null) StopCoroutine(_healthFXCoroutine);
        StartCoroutine(HealthFXCoroutine(false));
    }

    private IEnumerator HealthFXCoroutine(bool recoveredHP, bool wasAutoHeal = false)
    {
        var targetVignette = recoveredHP ? _startingVignetteIntensity : _damagedVignetteIntensity;
        var targetSaturation = recoveredHP ? _startingSaturation : _damagedSaturation;
        var targetAberration = recoveredHP ? _startingChromaticAberration : _damagedAberration;
        var timeElapsed = 0f;
        var duration = wasAutoHeal ? _autoRecoverHPVisualsDuration : _recoverHPVisualsDuration;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, targetVignette, timeElapsed / duration);
            _colorAdjustments.saturation.value = Mathf.Lerp(_colorAdjustments.saturation.value, targetSaturation,
                timeElapsed / duration);
            _chromaticAberration.intensity.value = Mathf.Lerp(_chromaticAberration.intensity.value, targetAberration,
                timeElapsed / duration);
            yield return null;
        }

        _vignette.intensity.value = targetVignette;
        _colorAdjustments.saturation.value = targetSaturation;
        _chromaticAberration.intensity.value = targetAberration;

        _healthFXCoroutine = null;
    }

    public void RecoverHP(int amount, bool wasAutoHeal = false)
    {
        if (CurrentHealth >= _maxHealth) return;

        if (_healthFXCoroutine != null) StopCoroutine(_healthFXCoroutine);
        StartCoroutine(HealthFXCoroutine(true, wasAutoHeal));

        var newHealth = CurrentHealth + amount;
        CurrentHealth = newHealth > _maxHealth ? _maxHealth : newHealth;

        AudioManager.Instance.AdjustMasterLowPass(22000f, _damagedLowPassAdjustDuration);
        if (!wasAutoHeal)
        {
            AudioManager.Instance.PlaySound(transform, _recoverHealthSFX, true, false, _damagedSFXVolume);
        }
    }

    public bool IsInvincible()
    {
        return _lastDamageTime + _invincibilityDuration >= Time.time || Invincible;
    }
}