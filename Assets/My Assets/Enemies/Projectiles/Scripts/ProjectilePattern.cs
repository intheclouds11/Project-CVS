using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum PatternAbilityUsage
{
    None,
    First,
    Last,
    All,
    Random
}

public enum BossSpawnPointSelection
{
    Left,
    Center,
    Right,
    Alternate,
    Random
}

[CreateAssetMenu(fileName = "NewProjectilePattern", menuName = "Boss/Projectile Pattern")]
public class ProjectilePattern : ScriptableObject
{
    public string PatternName;

    [Header("Projectile Settings")]
    public GameObject ProjectilePrefab;
    public bool AimAtPlayer;
    public float StartDelay = 3f;
    public float EndDelay = 3f;
    public float FireRate = 0.5f;
    public float Speed = 10f;
    public int FireCount = 10;
    public BossSpawnPointSelection SpawnPointSelection;

    [Header("Special")]
    public PatternAbilityUsage abilityUsage;

    [Header("FX")]
    public AudioClip ChargeSFX;
    public float ChargeVolume = 1f;
    public AudioClip SpawnSFX;
    public float SpawnVolume = 1f;
    public AudioClip CooldownSFX;
    public float CooldownVolume = 1f;

    private int _currentFireCount;
    private Transform _lastUsedSpawnPoint;


    private void OnEnable()
    {
        _currentFireCount = 0;
    }

    public GameObject Spawn(MultiProjectilePool pool, List<Transform> spawnPoints)
    {
        _currentFireCount++;
        bool enableAbility = abilityUsage == PatternAbilityUsage.First && _currentFireCount == 1 ||
                             abilityUsage == PatternAbilityUsage.Last && _currentFireCount == FireCount ||
                             abilityUsage == PatternAbilityUsage.All ||
                             abilityUsage == PatternAbilityUsage.Random && _currentFireCount == Random.Range(1, FireCount);

        if (_currentFireCount > FireCount)
        {
            Debug.LogError("[ProjectilePattern] currentFireCount is somehow greater than FireCount.");
        }

        var spawnPoint = GetSpawnPoint(spawnPoints);

        var projObj = pool.Get(ProjectilePrefab.name);
        projObj.transform.position = spawnPoint.position;
        projObj.transform.rotation = spawnPoint.rotation;

        var proj = projObj.GetComponent<Projectile>();
        proj.Init(pool, ProjectilePrefab.name, enableAbility);
        projObj.SetActive(true);

        var dirToPlayer = (GameManager.Instance.Player1.transform.position - spawnPoint.position).normalized;
        var direction = AimAtPlayer ? dirToPlayer : spawnPoint.forward;
        proj.Rb.linearVelocity = direction * Speed;

        var pitch = Random.Range(0.9f, 1.1f);
        AudioManager.Instance.PlaySound(proj.transform, SpawnSFX, true, false, SpawnVolume, pitch);

        return projObj;
    }

    private Transform GetSpawnPoint(List<Transform> spawnPoints)
    {
        Transform spawnPoint = null;
        if (SpawnPointSelection == BossSpawnPointSelection.Left)
        {
            spawnPoint = spawnPoints[0];
        }
        else if (SpawnPointSelection == BossSpawnPointSelection.Center)
        {
            spawnPoint = spawnPoints[1];
        }
        else if (SpawnPointSelection == BossSpawnPointSelection.Right)
        {
            spawnPoint = spawnPoints[2];
        }
        else if (SpawnPointSelection == BossSpawnPointSelection.Alternate)
        {
            if (!_lastUsedSpawnPoint)
            {
                spawnPoint = spawnPoints[0];
            }
            else if (_lastUsedSpawnPoint == spawnPoints[0])
            {
                spawnPoint = spawnPoints[1];
            }
            else if (_lastUsedSpawnPoint == spawnPoints[1])
            {
                spawnPoint = spawnPoints[2];
            }
            else
            {
                spawnPoint = spawnPoints[0];
            }
        }
        else if (SpawnPointSelection == BossSpawnPointSelection.Random)
        {
            spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            while (spawnPoint == _lastUsedSpawnPoint)
            {
                spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            }
        }

        _lastUsedSpawnPoint = spawnPoint;

        return spawnPoint;
    }

    public void OnPatternEnd()
    {
        _currentFireCount = 0;
    }
}