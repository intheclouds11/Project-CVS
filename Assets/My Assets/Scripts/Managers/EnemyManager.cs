using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// To scale up, have enemies notify EnemyManager when they spawn
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;
    public static event Action AllEnemiesCleared;

    private List<BaseEnemy> _activeEnemies = new();


    private void Awake()
    {
        Instance = this;
    }

    public void RegisterEnemy(BaseEnemy enemy)
    {
        if (!_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Add(enemy);
            enemy.Health.Died += OnEnemyDied;
        }
    }

    public void DeregisterEnemy(BaseEnemy enemy)
    {
        if (_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Remove(enemy);
            enemy.Health.Died -= OnEnemyDied;
        }
    }

    public void DeregisterAllEnemies()
    {
        foreach (var activeEnemy in _activeEnemies)
        {
            activeEnemy.Health.Died -= OnEnemyDied;
        }

        _activeEnemies.Clear();
    }

    private void OnEnemyDied(GameObject deadEnemy)
    {
        bool clearedAllEnemies = true;
        foreach (var enemy in _activeEnemies)
        {
            if (enemy.Health.CurrentHealth > 0)
            {
                clearedAllEnemies = false;
                break;
            }
        }

        DeregisterEnemy(deadEnemy.GetComponent<BaseEnemy>());

        if (clearedAllEnemies)
        {
            AllEnemiesCleared?.Invoke();
        }
    }
}