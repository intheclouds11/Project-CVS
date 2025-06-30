using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PauseScreen : MonoBehaviour
{
    public static PauseScreen Instance;
    public static bool IsPaused;
    [SerializeField]
    private GameObject _defaultButton;
    private Canvas _canvas;


    private void Awake()
    {
        Instance = this;
        _canvas = gameObject.GetComponent<Canvas>();
        _canvas.enabled = true;
        gameObject.SetActive(false);
    }

    public void OnPauseButtonPressed()
    {
        if (!gameObject.activeInHierarchy)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    private void PauseGame()
    {
        IsPaused = true;
        // Time.timeScale = 0f;
        EventSystem.current.SetSelectedGameObject(_defaultButton);
        gameObject.SetActive(true);
    }

    public void ResumeGame()
    {
        IsPaused = false;
        // Time.timeScale = 1f;
        EventSystem.current.SetSelectedGameObject(null); // prevents last clicked button remaining highlighted
        gameObject.SetActive(false);
    }
}