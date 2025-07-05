using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

public class TimeManager : MonoBehaviour
{
    public float NewTimeScale = 1f;

    [ShowNonSerializedField]
    private float _currentTimeScale;
    private float _defaultTimeScale;

    private void Awake()
    {
        _currentTimeScale = Time.timeScale;
        _defaultTimeScale = _currentTimeScale;
    }

    private void Update()
    {
        if (InputManager.Instance.TimeScaleUpWasPressed && Keyboard.current.shiftKey.isPressed)
        {
            _currentTimeScale += 0.3f;
            Time.timeScale = _currentTimeScale;
            Debug.Log($"Timescale large increase to {_currentTimeScale}");
        }
        else if (InputManager.Instance.TimeScaleUpWasPressed)
        {
            _currentTimeScale += 0.1f;
            Time.timeScale = _currentTimeScale;
            Debug.Log($"Timescale small increase to {_currentTimeScale}");
        }
        else if (InputManager.Instance.TimeScaleDownWasPressed && Keyboard.current.shiftKey.isPressed)
        {
            _currentTimeScale -= 0.3f;
            Time.timeScale = _currentTimeScale;
            Debug.Log($"Timescale large decrease to {_currentTimeScale}");
        }
        else if (InputManager.Instance.TimeScaleDownWasPressed)
        {
            _currentTimeScale -= 0.1f;
            Time.timeScale = _currentTimeScale;
            Debug.Log($"Timescale small decrease to {_currentTimeScale}");
        }
        else if (InputManager.Instance.TimeScaleResetWasPressed)
        {
            _currentTimeScale = _defaultTimeScale;
            Time.timeScale = _currentTimeScale;
            Debug.Log($"Timescale reset to {_currentTimeScale}");
        }
    }

    [Button]
    public void UpdateTimeScale()
    {
        Time.timeScale = NewTimeScale;
        _currentTimeScale = NewTimeScale;
    }
}