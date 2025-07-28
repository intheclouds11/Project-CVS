using System;
using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

public class MusicTriggerZone : MonoBehaviour
{
    [SerializeField]
    private AudioClip _newMusicClip;
    [SerializeField]
    private float _musicVolume = 1f;

    
    [SerializeField]
    private float _fadeOutTime = 0.25f;
    [SerializeField]
    private float _fadeInTime = 0.7f;
    [SerializeField]
    private float _ambienceGainOffset;
    

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (_fadeInTime > 0 || _fadeOutTime > 0)
        {
            AudioManager.Instance.TransitionMusic(_newMusicClip, _musicVolume, _fadeOutTime, _fadeInTime);
        }
        else
        {
            AudioManager.Instance.StartNewMusic(_newMusicClip, _musicVolume);
        }

        if (_ambienceGainOffset != 0)
        {
            AudioManager.Instance.AdjustAmbienceGroupGain(_ambienceGainOffset);
        }
    }
}