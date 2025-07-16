using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class PlayerHealth : Health
{
    [Header("Player Health")]
    [SerializeField]
    protected float _damageInvincibilityDuration = 0.5f;
    [SerializeField]
    protected float _damagedVignetteIntensity = 0.55f;
    [SerializeField]
    protected float _damagedLowPassFreq = 5000f;
    [SerializeField]
    protected float _damagedLowPassAdjustSpeed = 1.5f;
    [SerializeField]
    protected float _damagedSaturation = -40f;

    public bool IsInvincible()
    {
        return _lastDamageTime + _damageInvincibilityDuration >= Time.time;
    }

    protected float _lastDamageTime;
    protected float _startingVignetteIntensity;
    protected float _startingSaturation;
    protected Volume _globalVolume;
    protected Vignette _vignette;
    protected ColorAdjustments _colorAdjustments;


    protected override void Awake()
    {
        base.Awake();
        _globalVolume = FindAnyObjectByType<Volume>();
        _globalVolume.profile.TryGet(out _vignette);
        _globalVolume.profile.TryGet(out _colorAdjustments);
        _startingVignetteIntensity = _vignette.intensity.value;
        _startingSaturation = _colorAdjustments.saturation.value;
        // todo: possibly also zoom in while player injured
    }

    protected virtual void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected virtual void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // todo: better way to retain reference to scene Volume?
    protected virtual void OnSceneLoaded(Scene loadedScene, LoadSceneMode arg1)
    {
        if (loadedScene.name.Equals("MainMenu")) return;
        _globalVolume = FindAnyObjectByType<Volume>();
        _globalVolume.profile.TryGet(out _vignette);
        _globalVolume.profile.TryGet(out _colorAdjustments);
    }

    public override void TakeDamage(int damage, Vector3 knockbackDir, Knockback knockback)
    {
        if (GameManager.Instance.CurrentState is GameManager.GameState.Victory
            or GameManager.GameState.AwaitingWave or GameManager.GameState.GameOver) return;
        if (IsInvincible() || damage <= 0 || CurrentHealth <= 0) return;

        if (CurrentHealth == _maxHealth) AudioManager.Instance.AdjustMasterLowPass(_damagedLowPassFreq, _damagedLowPassAdjustSpeed);
        
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
        _vignette.intensity.value = _damagedVignetteIntensity;
        _colorAdjustments.saturation.value = _damagedSaturation;
    }

    public void RecoverHP(int amount)
    {
        if (Mathf.Approximately(_vignette.intensity.value, _startingVignetteIntensity)) return;

        var newHealth = CurrentHealth + amount;
        CurrentHealth = newHealth > _maxHealth ? _maxHealth : newHealth;
        _vignette.intensity.value = _startingVignetteIntensity;
        _colorAdjustments.saturation.value = _startingSaturation;

        AudioManager.Instance.AdjustMasterLowPass(22000f, _damagedLowPassAdjustSpeed);
        AudioManager.Instance.PlaySound(transform, _recoverHealthSFX, true, false, _damagedSFXVolume);
    }
}