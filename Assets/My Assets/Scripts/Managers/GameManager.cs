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

    public static GameManager Instance;
    public PlayerController Player1 { get; private set; }
    public bool GodMode { get; private set; }


    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(transform.root);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        if (InputManager.Instance.ToggleGodModeWasPressed)
        {
            GodMode = !GodMode;
            Debug.Log($"GameManager: GodMode set to {GodMode}");
        }
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
        CurrentState = GameState.GameOver;
        UIManager.Instance.ShowRespawnScreen();
    }

    public void OnReturnToMainMenu()
    {
        StopAllCoroutines();
        HUD.Instance.GetLoseUI.SetActive(false);
        HUD.Instance.GetWinUI.SetActive(false);
        HUD.Instance.GetWaveCompleteUI.SetActive(false);

        EnemyManager.Instance.DeregisterAllEnemies();
    }
}