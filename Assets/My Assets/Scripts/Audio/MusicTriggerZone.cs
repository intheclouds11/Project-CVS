using System;
using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

public class MusicTriggerZone : MonoBehaviour
{
    [SerializeField]
    private AudioClip _newAudioClip;

    [SerializeField]
    private bool _triggerOnce = true;
    [SerializeField]
    private bool _fadeOutCurrentMusic = true;
    [field: SerializeField, ShowIf(nameof(_fadeOutCurrentMusic))]
    private float _fadeOutTime = 0.25f;
    [SerializeField]
    private bool _fadeInMusic = true;
    [field: SerializeField, ShowIf(nameof(_fadeInMusic))]
    private float _fadeInTime = 0.7f;
    [field: SerializeField, ShowIf(nameof(_fadeInMusic))]
    private float _fadeInVolumeTarget = 1f;
    [SerializeField]
    private bool _adjustAmbienceGain;
    [field: SerializeField, ShowIf(nameof(_adjustAmbienceGain))]
    private float _ambienceGainOffset;

    private bool _wasTriggered;
    private Coroutine _fadeCoroutine;


    private void OnTriggerEnter(Collider other)
    {
        if ((_triggerOnce && _wasTriggered) || other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        _wasTriggered = true;

        var musicAudioSource = AudioManager.Instance.MusicAudioSource;
        if (_fadeInMusic || (_fadeOutCurrentMusic && musicAudioSource.volume > 0))
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeMusicCoroutine(musicAudioSource));
        }
        else
        {
            musicAudioSource.clip = _newAudioClip;
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

        audioSource.clip = _newAudioClip;

        if (_fadeInMusic)
        {
            audioSource.Play();

            while (audioSource.volume < _fadeInVolumeTarget)
            {
                audioSource.volume += Time.deltaTime / _fadeInTime;
                yield return null;
            }

            audioSource.volume = _fadeInVolumeTarget;
        }

        _fadeCoroutine = null;
    }
}