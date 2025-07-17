using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _newGameButton;
    [SerializeField]
    private GameObject _newGameConfirmCanvas;
    [SerializeField]
    private GameObject _continueButton;
    [SerializeField]
    private GameObject _settingsButton;
    [SerializeField]
    private GameObject _quitButton;
    [SerializeField]
    private AudioClip _menuMusic;
    [SerializeField]
    private float _menuMusicVolume = 0.8f;
    [SerializeField]
    private AudioClip _startGameSFX;
    [SerializeField]
    private float _startGameFadeDuration = 1f;

    private void Start()
    {
        if (PlayerSpawnManager.Instance.HasSpawnPointSave)
        {
            EventSystem.current.SetSelectedGameObject(_continueButton);
        }
        else
        {
            _continueButton.SetActive(false);
            EventSystem.current.SetSelectedGameObject(_newGameButton);
        }

        AudioManager.Instance.MusicAudioSource.volume = _menuMusicVolume;
        AudioManager.Instance.MusicAudioSource.clip = _menuMusic;
        AudioManager.Instance.MusicAudioSource.Play();
    }

    public void Button_NewGame()
    {
        if (PlayerSpawnManager.Instance.HasSpawnPointSave)
        {
            GetComponent<Canvas>().enabled = false;
            _newGameConfirmCanvas.SetActive(true);
        }
        else
        {
            Button_NewGameConfirm();
        }
    }

    public void Button_NewGameConfirm()
    {
        PlayerSpawnManager.ClearSavedSpawnPoint();
        StartGame();
    }

    public void Button_NewGameCancel()
    {
        _newGameConfirmCanvas.SetActive(false);
        GetComponent<Canvas>().enabled = true;
    }

    public void Button_Continue()
    {
        StartGame();
    }

    private void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        // Todo art pass: Add transition

        InputManager.Instance.Vibrate(0.4f, 0.1f, 0.5f);
        AudioManager.Instance.PlaySound(transform, _startGameSFX);
        yield return new WaitForSeconds(1);
        GameManager.Instance.GameStart();
        SceneManager.LoadScene(1);
    }

    public void Button_ShowSettings()
    {
        UIManager.Instance.Button_ShowSettingsMenu();
    }

    public void Button_ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}