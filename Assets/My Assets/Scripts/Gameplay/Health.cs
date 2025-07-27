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
    
    public int CurrentHealth
    {
        get => _currentHealth;
        protected set => _currentHealth = value;
    }

    public event Action<Vector3, Knockback> DamageTaken;
    public event Action<GameObject> Died;


    protected virtual void Awake()
    {
        CurrentHealth = _maxHealth;
    }

    public bool IsAlive()
    {
        return CurrentHealth > 0;
    }

    public virtual void TakeDamage(int damage, Vector3 knockbackDir, Knockback knockback, bool wasCritAttack = false)
    {
        if (GameManager.Instance.CurrentState is GameManager.GameState.Victory
            or GameManager.GameState.AwaitingWave or GameManager.GameState.GameOver) return;
        if (damage <= 0 || CurrentHealth <= 0) return;

        if (Invincible && !wasCritAttack)
        {
            OnDamaged(knockbackDir, knockback);
            return;
        }

        int newHealth = CurrentHealth - damage;
        if (GameManager.Instance.EnemyAIEnabled) CurrentHealth = newHealth;

        if (CurrentHealth > 0)
        {
            OnDamaged(knockbackDir, knockback);
        }
        else
        {
            OnDied();
        }
    }

    protected virtual void OnDamaged(Vector3 knockbackDir, Knockback knockback)
    {
        if (_damagedSFX)
        {
            float pitch = Random.Range(0.9f, 1f);
            AudioManager.Instance.PlaySound(transform, _damagedSFX, true, false, _damagedSFXVolume, pitch);
        }

        DamageTaken?.Invoke(knockbackDir, knockback);
    }

    protected virtual void OnDied()
    {
        AudioManager.Instance.PlaySound(transform, _diedSFX, true, false, _diedSFXVolume, _diedSFXPitch);
        if (_diedVFX)
        {
            Instantiate(_diedVFX, transform.position, transform.rotation);
        }

        Died?.Invoke(gameObject);
    }

    public void OnRespawn()
    {
        CurrentHealth = _maxHealth;
    }
}