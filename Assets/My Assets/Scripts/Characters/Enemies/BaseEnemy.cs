using System;
using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour
{
    public bool canTarget = true;

    public Health Health { get; protected set; }
    protected PlayerController _player;


    protected virtual void Awake()
    {
        Health = GetComponent<Health>();
    }

    protected void OnEnable()
    {
        Health.Died += OnDied;
        PlayerSpawnPoint.PlayerSpawned += OnPlayerSpawned;
    }

    protected void OnDisable()
    {
        Health.Died -= OnDied;
        PlayerSpawnPoint.PlayerSpawned -= OnPlayerSpawned;
    }

    protected void Start()
    {
        EnemyManager.Instance.RegisterEnemy(this);
    }

    private void OnPlayerSpawned()
    {
        _player = GameManager.Instance.Player1;
    }

    protected void OnDied(GameObject obj)
    {
        gameObject.SetActive(false);
    }
}