using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        StartMenu,
        Playing,
        Victory,
        AwaitingWave,
        GameOver,
        Paused
    }

    public GameState CurrentState { get; private set; } = GameState.StartMenu;

    [SerializeField]
    private float _playerRespawnTime = 2f;

    public static GameManager Instance;
    public PlayerController Player1 { get; private set; }


    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(transform.root);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void OnEnable()
    {
        PlayerSpawnPoint.PlayerSpawned += OnPlayerSpawned;
        EnemyManager.AllEnemiesCleared += OnAllEnemiesCleared;
    }

    private void OnDisable()
    {
        PlayerSpawnPoint.PlayerSpawned -= OnPlayerSpawned;
        EnemyManager.AllEnemiesCleared -= OnAllEnemiesCleared;
    }

    public void GameStart()
    {
        CurrentState = GameState.Playing;
    }

    private void OnPlayerSpawned()
    {
        Player1 = FindAnyObjectByType<PlayerController>();
        Player1.Health.Died += OnPlayerDied;
    }

    private void OnAllEnemiesCleared()
    {
        StartCoroutine(OnAllEnemiesClearedCoroutine());
    }

    private IEnumerator OnAllEnemiesClearedCoroutine()
    {
        if (WaveManager.Instance.AnyWavesRemaining())
        {
            HUD.Instance.GetWaveCompleteUI.SetActive(true);
            CurrentState = GameState.AwaitingWave;
            yield return new WaitForSeconds(3f);
            
            CurrentState = GameState.Playing;
            WaveManager.Instance.StartNextWave();
            HUD.Instance.GetWaveCompleteUI.SetActive(false);
        }
        else
        {
            CurrentState = GameState.Victory;
            HUD.Instance.GetWinUI.SetActive(true);
            yield return new WaitForSeconds(2f);
            
            HUD.Instance.GetWinUI.SetActive(false);
            Player1.enabled = false;
            UIManager.Instance.ShowEndScreen();
        }
    }

    private void OnPlayerDied(GameObject deadObj)
    {
        StartCoroutine(OnPlayerDiedCoroutine());
    }
    
    private IEnumerator OnPlayerDiedCoroutine()
    {
        HUD.Instance.GetLoseUI.SetActive(true);
        yield return new WaitForSeconds(_playerRespawnTime);

        HUD.Instance.GetLoseUI.SetActive(false);
        UIManager.Instance.ShowEndScreen();
    }

    public void OnReturnToMainMenu()
    {
        StopAllCoroutines();
        HUD.Instance.GetLoseUI.SetActive(false);
        HUD.Instance.GetWinUI.SetActive(false);
        HUD.Instance.GetWaveCompleteUI.SetActive(false);

        Player1?.Respawn(Vector3.zero, Quaternion.identity, false);
        EnemyManager.Instance.DeregisterAllEnemies();
    }
}