using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField]
    private PauseMenu _pauseMenu;
    [SerializeField]
    private GameObject _settingsMenu;
    [SerializeField]
    private GameObject _respawnScreen;

    private GameObject lastSelected;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        lastSelected = EventSystem.current.currentSelectedGameObject;
    }

    private void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null) EventSystem.current.SetSelectedGameObject(lastSelected);
        else lastSelected = EventSystem.current.currentSelectedGameObject;

        if (InputManager.Instance.PauseWasPressed)
        {
            if (CanTogglePauseScreen())
            {
                _pauseMenu.OnPauseButtonPressed();
            }
            else if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                ExitPauseMenu();
            }
        }
        else if (InputManager.Instance.GamepadEastButtonWasPressed)
        {
            if (_pauseMenu.isActiveAndEnabled)
            {
                _pauseMenu.OnPauseButtonPressed();
            }
            else if (_settingsMenu.activeInHierarchy)
            {
                ExitSettingsMenu(true);
            }
        }
    }

    private bool CanTogglePauseScreen()
    {
        return SceneManager.GetActiveScene().name != "MainMenu" && !_settingsMenu.activeInHierarchy;
    }

    public bool IsAMenuOpen()
    {
        return _pauseMenu.gameObject.activeInHierarchy || _settingsMenu.activeInHierarchy ||
               SceneManager.GetActiveScene().name == "MainMenu";
    }

    public void ShowRespawnScreen()
    {
        _respawnScreen.SetActive(true);
    }

    public void Button_ResumeGame()
    {
        _pauseMenu.ResumeGame();
    }

    public void Button_ReturnToMainMenu()
    {
        GameManager.Instance.OnReturnToMainMenu();

        if (_pauseMenu.gameObject.activeSelf)
        {
            _pauseMenu.gameObject.SetActive(false);
        }

        SceneManager.LoadScene("MainMenu");
    }

    public void Button_ShowSettingsMenu()
    {
        var mainMenu = FindAnyObjectByType<MainMenu>();
        if (mainMenu)
        {
            mainMenu.gameObject.SetActive(false);
        }
        else if (_pauseMenu.gameObject.activeInHierarchy)
        {
            _pauseMenu.gameObject.SetActive(false);
        }

        _settingsMenu.SetActive(true);
    }

    public void ExitPauseMenu()
    {
        if (_settingsMenu.activeInHierarchy)
        {
            ExitSettingsMenu();
        }

        _pauseMenu.ResumeGame();
    }

    public void ExitSettingsMenu(bool returnToPauseMenu = false)
    {
        if (MainMenu.Instance)
        {
            MainMenu.Instance.gameObject.SetActive(true);
        }
        else if (returnToPauseMenu)
        {
            _pauseMenu.OpenMenu();
        }

        _settingsMenu.GetComponent<SettingsMenu>().OnExitSettings();
    }

    public void Button_ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}