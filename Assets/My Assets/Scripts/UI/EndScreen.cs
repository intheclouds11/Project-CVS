using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class EndScreen : MonoBehaviour
{
    [SerializeField]
    private GameObject _defaultButton;
    
    private Canvas _canvas;
    

    private void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(_defaultButton);
    }

    private void Awake()
    {
        _canvas = gameObject.GetComponent<Canvas>();
        _canvas.enabled = true;
        gameObject.SetActive(false);
    }
}
