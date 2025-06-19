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
        GameManager.Instance.GameStart();
        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
