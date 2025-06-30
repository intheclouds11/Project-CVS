using System;
using NaughtyAttributes;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public float NewTimeScale = 1f;

    [ShowNonSerializedField]
    private float _currentTimeScale;

    private void Awake()
    {
        _currentTimeScale = Time.timeScale;
    }

    [Button]
    public void UpdateTimeScale()
    {
        Time.timeScale = NewTimeScale;
        _currentTimeScale = NewTimeScale;
    }
}
