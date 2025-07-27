using System;
using System.Collections.Generic;
using NaughtyAttributes;
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
    public ProjectilePattern FollowupPattern;

    [Header("Projectile Settings")]
    public GameObject ProjectilePrefab;
    public float StartDelay = 3f;
    public float EndDelay = 3f;
    public float TimeBetweenShots = 0.5f;
    public float Speed = 10f;
    public int FireCount = 10;
    public BossSpawnPointSelection SpawnPointSelection;
    public bool AimAtPlayer;
    [field: HideIf(nameof(AimAtPlayer)), Header("Spread")]
    public float StartAngle = 0f;
    [HideIf(nameof(AimAtPlayer))]
    public float EndAngle = 90f;

    [Header("Special")]
    public PatternAbilityUsage abilityUsage;

    [Header("FX")]
    public GameObject SpawnPointChargeVFX;
    public AudioClip ChargeSFX;
    public float ChargeVolume = 1f;
    public float ChargePitch = 1f;
    public GameObject SpawnPointFireVFX;
    public AudioClip SpawnSFX;
    public float SpawnVolume = 1f;
    public AudioClip CooldownSFX;
    public float CooldownVolume = 1f;

    private int _currentFireCount;
    private float _spreadAngleOffset;
    private float _currentSpreadAngleOffset;
    private Transform _lastUsedSpawnPoint;
    private Transform _nextSpawnPoint;
    

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        _currentFireCount = 0;
        _currentSpreadAngleOffset = StartAngle;
        _spreadAngleOffset = (EndAngle - StartAngle) / FireCount;
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
        Instantiate(SpawnPointFireVFX, spawnPoint.position, Quaternion.LookRotation(spawnPoint.forward));

        var projObj = pool.Get(ProjectilePrefab.name);
        projObj.transform.position = spawnPoint.position;
        projObj.transform.rotation = spawnPoint.rotation;

        var proj = projObj.GetComponent<Projectile>();
        proj.Init(pool, ProjectilePrefab.name, enableAbility);
        projObj.SetActive(true);

        Vector3 direction;
        if (AimAtPlayer)
        {
            direction = (GameManager.Instance.Player1.transform.position - spawnPoint.position).normalized;
        }
        else
        {
            var rotation = Quaternion.Euler(0f, _currentSpreadAngleOffset, 0f);
            direction = rotation * spawnPoint.forward;
            _currentSpreadAngleOffset += _spreadAngleOffset;
        }

        var dirNoPitch = new Vector3(direction.x, 0f, direction.z).normalized;
        proj.Rb.linearVelocity = dirNoPitch * Speed;

        var pitch = Random.Range(0.9f, 1.1f);
        AudioManager.Instance.PlaySound(proj.transform, SpawnSFX, true, false, SpawnVolume, pitch);

        return projObj;
    }

    public Transform GetSpawnPoint(List<Transform> spawnPoints, bool isSpawning = true)
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

        if (isSpawning) _lastUsedSpawnPoint = spawnPoint;

        return spawnPoint;
    }
}