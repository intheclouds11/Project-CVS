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


    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (CanShowPauseScreen() && InputManager.instance.pauseMenuInputPressed)
        {
            PauseMenu.Instance.OnPauseButtonPressed();
        }
    }

    private bool CanShowPauseScreen()
    {
        return !_endScreen.activeSelf && SceneManager.GetActiveScene().name != "MainMenu";
    }

    public void ShowEndScreen()
    {
        _endScreen.SetActive(true);
    }
    
    public void Button_ResumeGame()
    {
        PauseMenu.Instance.ResumeGame();
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
            PauseMenu.Instance.ResumeGame();
        }
        SceneManager.LoadScene("MainMenu");
    }

    public void Button_ExitGame()
    {
        Application.Quit();
    }
}
