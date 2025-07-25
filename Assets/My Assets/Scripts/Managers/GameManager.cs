using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public bool EnemyAIEnabled { get; private set; } = true;
    public static event Action<bool> EnemyAIToggled;


    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(transform.root);
    }

    private void Update()
    {
        if (!UIManager.Instance.IsAMenuOpen())
        {
            if (InputManager.Instance.ToggleGodModeWasPressed)
            {
                GodMode = !GodMode;
                if (GodMode) Player1.PlayerCharges.ForceFullRecharge();
                Debug.Log($"[GameManager] GodMode {GodMode}");
            }

            if (InputManager.Instance.ToggleEnemyAIWasPressed)
            {
                EnemyAIEnabled = !EnemyAIEnabled;
                EnemyAIToggled?.Invoke(EnemyAIEnabled);
                Debug.Log($"[GameManager] EnemyAIEnabled: {EnemyAIEnabled}");
            }
        }
    }

    private void OnEnable()
    {
        PlayerSpawnManager.PlayerSpawned += OnPlayerSpawned;
        EnemyManager.AllEnemiesCleared += OnAllEnemiesCleared;
    }

    private void OnDisable()
    {
        PlayerSpawnManager.PlayerSpawned -= OnPlayerSpawned;
        EnemyManager.AllEnemiesCleared -= OnAllEnemiesCleared;
    }

    public float GetDistanceFromPlayer(Transform fromTransform)
    {
        return Vector3.Distance(fromTransform.position, Player1.transform.position);
    }

    public void GameStart()
    {
        CurrentState = GameState.Playing;
    }

    private void OnPlayerSpawned(PlayerController player)
    {
        Player1 = player;
        Player1.Health.Died += OnPlayerDied;
    }

    private void OnAllEnemiesCleared()
    {
        // StartCoroutine(OnAllEnemiesClearedCoroutine());
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
        }
    }

    private void OnPlayerDied(GameObject deadObj)
    {
        CurrentState = GameState.GameOver;
        UIManager.Instance.ToggleRespawnScreen(true);
        InputManager.Instance.ToggleInputsAllowed(false);
    }

    public void OnReturnToMainMenu()
    {
        UIManager.Instance.ToggleRespawnScreen(false);

        StopAllCoroutines();
        HUD.Instance.GetLoseUI.SetActive(false);
        HUD.Instance.GetWinUI.SetActive(false);
        HUD.Instance.GetWaveCompleteUI.SetActive(false);

        EnemyManager.Instance.DeregisterAllEnemies();
    }

    public void OnMainMenuStart()
    {
        if (Player1)
        {
            Destroy(Player1.gameObject);
            Player1 = null;
        }
    }

    public void OnRespawn()
    {
        StopAllCoroutines();
        HUD.Instance.GetLoseUI.SetActive(false);
        HUD.Instance.GetWinUI.SetActive(false);
        HUD.Instance.GetWaveCompleteUI.SetActive(false);

        EnemyManager.Instance.DeregisterAllEnemies();
    }
}