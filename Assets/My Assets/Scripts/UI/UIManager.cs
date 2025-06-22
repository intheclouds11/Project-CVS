using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField]
    private GameObject _pauseMenu;
    [SerializeField]
    private GameObject _endScreen;
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
            PauseScreen.Instance.OnPauseButtonPressed();
        }

        if (InputManager.Instance.ToggleChargeHUDWasPressed)
        {
            GameManager.Instance.Player1.PlayerAttack.ToggleChargeHUD();
        }
    }

    private bool CanShowPauseScreen()
    {
        return !_endScreen.activeSelf && SceneManager.GetActiveScene().name != "MainMenu";
    }

    public void ShowRespawnScreen()
    {
        _respawnScreen.SetActive(true);
    }

    public void ShowEndScreen()
    {
        _endScreen.SetActive(true);
    }

    public void Button_ResumeGame()
    {
        PauseScreen.Instance.ResumeGame();
    }

    public void Button_ReturnToMainMenu()
    {
        GameManager.Instance.OnReturnToMainMenu();
        if (_endScreen.activeSelf)
        {
            _endScreen.SetActive(false);
        }

        if (_pauseMenu.activeSelf)
        {
            PauseScreen.Instance.ResumeGame();
        }

        SceneManager.LoadScene("MainMenu");
    }

    public void Button_ExitGame()
    {
        Application.Quit();
    }
}