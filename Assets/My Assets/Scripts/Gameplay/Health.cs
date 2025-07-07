using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Health : MonoBehaviour
{
    [SerializeField]
    private int _currentHealth;
    [SerializeField]
    private int _maxHealth;
    [SerializeField]
    private float _damageKnockbackAmount;
    [SerializeField]
    private float _damageKnockbackDuration = 1f;
    [SerializeField]
    private float _damageInvincibilityDuration = 0f;
    [SerializeField]
    private AudioClip _damagedSFX;
    [SerializeField]
    private float _damagedVignetteIntensity = 0.7f;
    [SerializeField]
    private float _damagedSaturation = -40f;
    [SerializeField]
    private float _damagedSFXVolume = 0.8f;
    [SerializeField]
    private AudioClip _recoverHealthSFX;
    [FormerlySerializedAs("_diedSfx")]
    [SerializeField]
    private AudioClip _diedSFX;
    [SerializeField]
    private float _diedSFXVolume = 0.9f;
    [FormerlySerializedAs("_diedVfx")]
    [SerializeField]
    private GameObject _diedVFX;

    public int CurrentHealth
    {
        get => _currentHealth;
        private set => _currentHealth = value;
    }

    /// Vector3: knockbackDir, float: _damageKnockbackAmount, float: _damageKnockbackDuration
    public event Action<Vector3, float, float> DamageTaken;

    public event Action<GameObject> Died;
    private float _lastDamageTime;

    private Volume _globalVolume;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;
    private float _startingVignetteIntensity;
    private float _startingSaturation;
    private bool _isPlayerHealth;


    private void Awake()
    {
        CurrentHealth = _maxHealth;

        _isPlayerHealth = GetComponent<PlayerController>();
        if (_isPlayerHealth)
        {
            _globalVolume = FindAnyObjectByType<Volume>();
            _globalVolume.profile.TryGet(out _vignette);
            _globalVolume.profile.TryGet(out _colorAdjustments);
            _startingVignetteIntensity = _vignette.intensity.value;
            _startingSaturation = _colorAdjustments.saturation.value;
            // todo: possibly also zoom in while player injured
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // todo: better way to retain reference to scene Volume?
    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        _globalVolume = FindAnyObjectByType<Volume>();
        _globalVolume.profile.TryGet(out _vignette);
        _globalVolume.profile.TryGet(out _colorAdjustments);
    }

    public void RecoverHP(int amount)
    {
        if (Mathf.Approximately(_vignette.intensity.value, _startingVignetteIntensity)) return;

        var newHealth = CurrentHealth + amount;
        CurrentHealth = newHealth > _maxHealth ? _maxHealth : newHealth;
        _vignette.intensity.value = _startingVignetteIntensity;
        _colorAdjustments.saturation.value = _startingSaturation;

        AudioManager.Instance.PlaySound(transform, _recoverHealthSFX, true, false, _damagedSFXVolume);
    }

    public bool IsAlive()
    {
        return CurrentHealth > 0;
    }

    public void TakeDamage(int damage, Vector3 knockbackDir)
    {
        if (GameManager.Instance.CurrentState is GameManager.GameState.Victory
            or GameManager.GameState.AwaitingWave or GameManager.GameState.GameOver) return;
        if (damage <= 0 || CurrentHealth <= 0) return;
        if (_lastDamageTime + _damageInvincibilityDuration >= Time.time) return;

        
        int newHealth = CurrentHealth - damage;
        bool isGodMode = _isPlayerHealth && GameManager.Instance.GodMode;

        if (newHealth > 0)
        {
            OnDamaged(knockbackDir);
        }
        else if (!isGodMode)
        {
            OnDied();
        }

        if (!isGodMode) CurrentHealth = newHealth;
        _lastDamageTime = Time.time;
    }

    private void OnDamaged(Vector3 knockbackDir)
    {
        if (_damagedSFX)
        {
            float pitch = Random.Range(0.9f, 1f);
            AudioManager.Instance.PlaySound(transform, _damagedSFX, true, false, _damagedSFXVolume, pitch);
        }

        if (_isPlayerHealth)
        {
            _vignette.intensity.value = _damagedVignetteIntensity;
            _colorAdjustments.saturation.value = _damagedSaturation;
        }

        DamageTaken?.Invoke(knockbackDir, _damageKnockbackAmount, _damageKnockbackDuration);
    }

    private void OnDied()
    {
        float pitch = Random.Range(0.9f, 1f);
        AudioManager.Instance.PlaySound(transform, _diedSFX, true, false, _diedSFXVolume, pitch);
        Instantiate(_diedVFX, transform.position, transform.rotation);

        Died?.Invoke(gameObject);
    }

    public void OnRespawn()
    {
        CurrentHealth = _maxHealth;
    }
}