using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField]
    private float _maxHealth;
    [SerializeField]
    private AudioClip _diedSfx;
    [SerializeField]
    private GameObject _diedVfx;

    public float CurrentHealth { get; private set; }
    public event Action DamageTaken;
    public event Action<GameObject> Died;


    private void Awake()
    {
        CurrentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0 || GameManager.Instance.CurrentState is GameManager.GameState.Victory or GameManager.GameState.AwaitingWave
                or GameManager.GameState.GameOver) return;

        CurrentHealth -= damage;
        DamageTaken?.Invoke();

        if (CurrentHealth <= 0)
        {
            OnDied();
        }
    }

    private void OnDied()
    {
        AudioManager.Instance.PlaySound(transform, _diedSfx);
        Instantiate(_diedVfx, transform.position, transform.rotation);
        Died?.Invoke(gameObject);
    }

    public void OnRespawn()
    {
        CurrentHealth = _maxHealth;
    }
}