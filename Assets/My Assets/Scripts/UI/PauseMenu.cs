using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _defaultButton;


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
        EventSystem.current.SetSelectedGameObject(_defaultButton);
        gameObject.SetActive(true);
        InputManager.Instance.ToggleInputsAllowed(false);
    }

    public void ResumeGame()
    {
        CloseMenu();
        InputManager.Instance.ToggleInputsAllowed(true);
    }

    public void CloseMenu()
    {
        gameObject.SetActive(false);
    }
}