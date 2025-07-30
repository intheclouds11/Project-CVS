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
    protected int _currentHealth;
    [SerializeField]
    protected int _maxHealth;
    public int GetMaxHealth => _maxHealth;
    public bool Invincible;
    
    [Header("FX")]
    [SerializeField]
    protected AudioClip _damagedSFX;
    [SerializeField]
    protected float _damagedSFXVolume = 0.8f;
    [SerializeField]
    protected AudioClip _recoverHealthSFX;
    [FormerlySerializedAs("_diedSfx")]
    [SerializeField]
    protected AudioClip _diedSFX;
    [SerializeField]
    protected float _diedSFXVolume = 0.9f;
    [SerializeField]
    protected float _diedSFXPitch = 1f;
    [FormerlySerializedAs("_diedVfx")]
    [SerializeField]
    protected GameObject _diedVFX;
    [SerializeField]
    protected float _diedVFXDelay;
    
    public int CurrentHealth
    {
        get => _currentHealth;
        protected set => _currentHealth = value;
    }

    public event Action<Knockback> DamageTaken;
    public event Action<Knockback> Died;


    protected virtual void Awake()
    {
        CurrentHealth = _maxHealth;
    }

    public bool IsAlive()
    {
        return CurrentHealth > 0;
    }

    public virtual void TakeDamage(int damage, Knockback knockback, bool wasCritAttack = false)
    {
        if (GameManager.Instance.CurrentState is GameManager.GameState.Victory
            or GameManager.GameState.AwaitingWave or GameManager.GameState.GameOver) return;
        if (damage <= 0 || CurrentHealth <= 0) return;

        if (Invincible && !wasCritAttack)
        {
            OnDamaged(knockback);
            return;
        }

        int newHealth = CurrentHealth - damage;
        if (GameManager.Instance.EnemyAIEnabled) CurrentHealth = newHealth;

        if (CurrentHealth > 0)
        {
            OnDamaged(knockback);
        }
        else
        {
            OnDied(knockback);
        }
    }

    protected virtual void OnDamaged(Knockback knockback)
    {
        if (Invincible)
        {
            AudioManager.Instance.PlayInvincibleImpact(transform);
        }
        else if (_damagedSFX)
        {
            float pitch = Random.Range(0.9f, 1f);
            AudioManager.Instance.PlaySound(transform, _damagedSFX, true, false, _damagedSFXVolume, pitch);
        }
        
        DamageTaken?.Invoke(knockback);
    }

    protected virtual void OnDied( Knockback knockback)
    {
        AudioManager.Instance.PlaySound(transform, _diedSFX, true, false, _diedSFXVolume, _diedSFXPitch);
        if (_diedVFX)
        {
            Invoke(nameof(StartDeathVFX), _diedVFXDelay);
        }

        DamageTaken?.Invoke(knockback);
        Died?.Invoke(knockback);
    }

    private void StartDeathVFX()
    {
        Instantiate(_diedVFX, transform.position, transform.rotation);
    }

    public void OnRespawn()
    {
        CurrentHealth = _maxHealth;
    }
}