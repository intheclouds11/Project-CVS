using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class EndScreen : MonoBehaviour
{
    [SerializeField]
    private GameObject _defaultButton;

    private void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(_defaultButton);
    }
}
