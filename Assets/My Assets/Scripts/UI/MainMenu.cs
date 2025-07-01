using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _defaultButton;

    private void Start()
    {
        EventSystem.current.SetSelectedGameObject(_defaultButton);
    }

    public void StartGame()
    {
        // Todo: Add transition
        InputManager.Instance.Vibrate(0.6f, 0.5f, 1f);
        GameManager.Instance.GameStart();
        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
