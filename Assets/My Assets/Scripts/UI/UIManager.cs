using System;
using UnityEngine;
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


    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (CanShowPauseScreen() && InputManager.Instance.PauseWasPressed)
        {
            _pauseMenu.OnPauseButtonPressed();
        }

        if (InputManager.Instance.ToggleChargeHUDWasPressed)
        {
            GameManager.Instance.Player1.PlayerAttack.ToggleChargeHUD();
        }
    }

    private bool CanShowPauseScreen()
    {
        return SceneManager.GetActiveScene().name != "MainMenu" && !_settingsMenu.activeInHierarchy;
    }

    public bool IsPauseMenuOpen()
    {
        return _pauseMenu.gameObject.activeInHierarchy;
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
            _pauseMenu.CloseMenu();
        }

        SceneManager.LoadScene("MainMenu");
    }

    public void Button_ShowSettingsMenu()
    {
        var mainMenu = FindAnyObjectByType<MainMenu>();
        if (mainMenu)
        {
            mainMenu.GetComponent<Canvas>().enabled = false;
        }
        else if (_pauseMenu.gameObject.activeInHierarchy)
        {
            _pauseMenu.gameObject.SetActive(false);
        }

        _settingsMenu.SetActive(true);
    }

    public void ExitSettingsMenu()
    {
        var mainMenu = FindAnyObjectByType<MainMenu>();
        if (mainMenu)
        {
            mainMenu.GetComponent<Canvas>().enabled = true;
        }
        else
        {
            _pauseMenu.OpenMenu();
        }

        _settingsMenu.SetActive(false);
    }

    public void Button_ExitGame()
    {
        Application.Quit();
    }
}