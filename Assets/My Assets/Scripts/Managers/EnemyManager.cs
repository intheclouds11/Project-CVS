using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// To scale up, have enemies notify EnemyManager when they spawn
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;
    public static event Action AllEnemiesCleared;

    public List<BaseEnemy> _activeEnemies { get; private set; } = new();


    private void Awake()
    {
        Instance = this;
    }

    public void RegisterEnemy(BaseEnemy enemy)
    {
        if (!_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Add(enemy);
        }
    }

    public void DeregisterEnemy(BaseEnemy enemy)
    {
        if (_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Remove(enemy);
        }
    }

    public void DeregisterAllEnemies()
    {
        _activeEnemies.Clear();
    }

    public bool AnyAggroedEnemies()
    {
        return _activeEnemies.Any(activeEnemy => activeEnemy.IsAggroed);
    }
}