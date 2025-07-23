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
        AudioManager.Instance.AdjustMasterLowPass(3000f, 0.5f);
        InputManager.Instance.ToggleInputsAllowed(false);
    }

    public void ResumeGame()
    {
        IsPaused = false;
        gameObject.SetActive(false);
        InputManager.Instance.ToggleInputsAllowed(true);
        AudioManager.Instance.AdjustMasterLowPass(22000f, 0.5f);
    }

    public void OnReturnToMainMenu()
    {
        IsPaused = false;
        gameObject.SetActive(false);
        AudioManager.Instance.AdjustMasterLowPass(22000f, 0f);
    }
}