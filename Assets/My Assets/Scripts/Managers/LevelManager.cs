using System;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelManager : MonoBehaviour
{
    [SerializeField]
    private AudioClip _music;
    [SerializeField]
    private float _musicVolume = 0.8f;

    
    private void Awake()
    {
        var musicAudioSource = AudioManager.Instance.MusicAudioSource;
        // Don't replay level music
        if (musicAudioSource.isPlaying && musicAudioSource.clip == _music)
        {
            return;
        }
        musicAudioSource.clip = _music;
        musicAudioSource.volume = _musicVolume;
        musicAudioSource.Play();
    }
}
