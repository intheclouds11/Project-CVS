using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _defaultButton;
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
        EventSystem.current.SetSelectedGameObject(_defaultButton);
        AudioManager.Instance.MusicAudioSource.volume = _menuMusicVolume;
        AudioManager.Instance.MusicAudioSource.clip = _menuMusic;
        AudioManager.Instance.MusicAudioSource.Play();
    }

    public void Button_NewGame()
    {
        PlayerSpawnManager.ClearSavedSpawnPoint();
        Button_Continue();
    }

    public void Button_Continue()
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

    public void Button_Settings()
    {
    }

    public void Button_ExitGame()
    {
        Application.Quit();
    }
}