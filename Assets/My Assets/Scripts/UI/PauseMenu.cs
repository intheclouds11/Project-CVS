using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _defaultButton;

    public static bool IsPaused;


    public void OnPauseButtonPressed()
    {
        if (!gameObject.activeInHierarchy)
        {
            OpenMenu();
        }
        else
        {
            ResumeGame();
        }
    }

    public void OpenMenu()
    {
        IsPaused = true;
        EventSystem.current.SetSelectedGameObject(_defaultButton);
        gameObject.SetActive(true);
        InputManager.Instance.ToggleInputsAllowed(false);
    }

    public void ResumeGame()
    {
        IsPaused = false;
        gameObject.SetActive(false);
        InputManager.Instance.ToggleInputsAllowed(true);
    }

    public void OnReturnToMainMenu()
    {
        IsPaused = false;
        gameObject.SetActive(false);
    }
}