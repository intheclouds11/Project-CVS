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
    private bool _fadeOutCurrentMusic = true;
    [field: SerializeField, ShowIf(nameof(_fadeOutCurrentMusic))]
    private float _fadeOutTime = 0.25f;
    [SerializeField]
    private bool _fadeInMusic = true;
    [field: SerializeField, ShowIf(nameof(_fadeInMusic))]
    private float _fadeInTime = 0.7f;
    [SerializeField]
    private bool _adjustAmbienceGain;
    [field: SerializeField, ShowIf(nameof(_adjustAmbienceGain))]
    private float _ambienceGainOffset;

    private Coroutine _fadeCoroutine;
    private AudioSource _musicAudioSource;


    private void Start()
    {
        _musicAudioSource = AudioManager.Instance.MusicAudioSource;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _musicAudioSource.clip == _newMusicClip) return;

        if (_fadeInMusic || (_fadeOutCurrentMusic && _musicAudioSource.volume > 0))
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeMusicCoroutine(_musicAudioSource));
        }
        else
        {
            _musicAudioSource.clip = _newMusicClip;
            _musicAudioSource.volume = _musicVolume;
            _musicAudioSource.Play();
        }

        if (_adjustAmbienceGain)
        {
            AudioManager.Instance.AdjustAmbienceGroupGain(_ambienceGainOffset);
        }
    }

    private IEnumerator FadeMusicCoroutine(AudioSource audioSource)
    {
        if (_fadeOutCurrentMusic && audioSource.isPlaying)
        {
            var startVolume = audioSource.volume;
            while (audioSource.volume > 0)
            {
                audioSource.volume -= startVolume * Time.deltaTime / _fadeOutTime;
                yield return null;
            }

            audioSource.volume = 0f;
        }

        audioSource.clip = _newMusicClip;
        audioSource.Play();

        if (_fadeInMusic)
        {
            while (audioSource.volume < _musicVolume)
            {
                audioSource.volume += Time.deltaTime / _fadeInTime;
                yield return null;
            }
        }

        audioSource.volume = _musicVolume;
        _fadeCoroutine = null;
    }
}