using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance;
    
    [SerializeField]
    private GameObject _newGameButton;
    [SerializeField]
    private GameObject _newGameConfirmCanvas;
    [SerializeField]
    private GameObject _newGameCancelButton;
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


    private void OnEnable()
    {
        if (SaveLoadManager.SaveFileExists())
        {
            EventSystem.current.SetSelectedGameObject(_continueButton);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(_newGameButton);
            
            _continueButton.SetActive(false);

            var newNav = new Navigation();
            newNav.mode = Navigation.Mode.Explicit;
            newNav.selectOnUp = _quitButton.GetComponent<Selectable>();
            newNav.selectOnDown = _settingsButton.GetComponent<Selectable>();
            _newGameButton.GetComponent<Button>().navigation = newNav;

            newNav = new Navigation();
            newNav.mode = Navigation.Mode.Explicit;
            newNav.selectOnUp = _settingsButton.GetComponent<Selectable>();
            newNav.selectOnDown = _newGameButton.GetComponent<Selectable>();
            _quitButton.GetComponent<Button>().navigation = newNav;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        AudioManager.Instance.MusicAudioSource.volume = _menuMusicVolume;
        AudioManager.Instance.MusicAudioSource.clip = _menuMusic;
        AudioManager.Instance.MusicAudioSource.Play();
    }

    public void Button_NewGame()
    {
        if (SaveLoadManager.SaveFileExists())
        {
            GetComponent<Canvas>().enabled = false;
            _newGameConfirmCanvas.SetActive(true);
            EventSystem.current.SetSelectedGameObject(_newGameCancelButton);
        }
        else
        {
            Button_NewGameConfirm();
        }
    }

    public void Button_NewGameConfirm()
    {
        SaveLoadManager.ClearSavedSpawnPoint();
        StartGame();
    }

    public void Button_NewGameCancel()
    {
        _newGameConfirmCanvas.SetActive(false);
        GetComponent<Canvas>().enabled = true;
        if (SaveLoadManager.SaveFileExists())
        {
            EventSystem.current.SetSelectedGameObject(_continueButton);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(_newGameButton);
        }
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
        UIManager.Instance.Button_ExitGame();
    }
}