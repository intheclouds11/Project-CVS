using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlayerHealth : Health
{
    [Header("Player Health")]
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

    // todo: better way to retain reference to scene Volume?
    private void OnSceneLoaded(Scene loadedScene, LoadSceneMode arg1)
    {
        if (loadedScene.name.Equals("MainMenu")) return;
        _globalVolume = FindAnyObjectByType<Volume>();
        _globalVolume.profile.TryGet(out _vignette);
        _globalVolume.profile.TryGet(out _colorAdjustments);
        _globalVolume.profile.TryGet(out _chromaticAberration);
    }

    public override void TakeDamage(int damage, Vector3 knockbackDir, Knockback knockback, bool wasCritAttack = false)
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
            OnDamaged(knockbackDir, knockback);
        }
        else
        {
            OnDied();
        }

        _lastDamageTime = Time.time;
    }

    protected override void OnDamaged(Vector3 knockbackDir, Knockback knockback)
    {
        base.OnDamaged(knockbackDir, knockback);
        _player.FlashMeshRenderers(_invincibilityDuration, _invincibilityFlashRate);
        _player.TogglePlayerTriggerCollider(false);

        _vignette.intensity.value = _damagedVignetteIntensity;
        _colorAdjustments.saturation.value = _damagedSaturation;
        _chromaticAberration.intensity.value = _damagedAberration;
    }

    public void RecoverHP(int amount)
    {
        if (Mathf.Approximately(_vignette.intensity.value, _startingVignetteIntensity)) return;

        var newHealth = CurrentHealth + amount;
        CurrentHealth = newHealth > _maxHealth ? _maxHealth : newHealth;
        _vignette.intensity.value = _startingVignetteIntensity;
        _colorAdjustments.saturation.value = _startingSaturation;
        _chromaticAberration.intensity.value = _startingChromaticAberration;

        AudioManager.Instance.AdjustMasterLowPass(22000f, _damagedLowPassAdjustDuration);
        AudioManager.Instance.PlaySound(transform, _recoverHealthSFX, true, false, _damagedSFXVolume);
    }

    public bool IsInvincible()
    {
        return _lastDamageTime + _invincibilityDuration >= Time.time || Invincible;
    }
}