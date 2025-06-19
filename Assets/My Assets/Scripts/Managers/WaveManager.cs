using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaveManager : MonoBehaviour
{
    [Serializable]
    public class Wave
    {
        public GameObject enemyPrefab;
        public Transform spawnPoint;
        public int enemyCount;
        public float spawnInterval = 1f;
    }

    public static WaveManager Instance;

    public List<Wave> Waves = new();
    public Transform[] SpawnPoints;
    
    private EnemyManager _enemyManager;
    private int currentWaveIndex = -1;
    private bool isSpawning;

    private void Awake()
    {
        Instance = this;
    }

    public bool AnyWavesRemaining()
    {
        return currentWaveIndex < Waves.Count - 1;
    }

    public void StartNextWave()
    {
        if (isSpawning)
        {
            Debug.LogWarning("WAVEMANAGER: Already spawning a wave!");
            return;
        }

        currentWaveIndex++;

        if (currentWaveIndex >= Waves.Count)
        {
            Debug.Log("WAVEMANAGER: All waves complete");
            return;
        }

        StartCoroutine(SpawnWaveCoroutine(Waves[currentWaveIndex]));
    }

    private IEnumerator SpawnWaveCoroutine(Wave wave)
    {
        isSpawning = true;

        for (int i = 0; i < wave.enemyCount; i++)
        {
            Transform spawnPoint;
            if (wave.spawnPoint)
            {
                spawnPoint = wave.spawnPoint;
            }
            else
            {
                spawnPoint = SpawnPoints[i];
            }

            var enemy = Instantiate(
                wave.enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation);
            
            EnemyManager.Instance.RegisterEnemy(enemy.GetComponent<BaseEnemy>());

            yield return new WaitForSeconds(wave.spawnInterval);
        }

        isSpawning = false;
    }
}