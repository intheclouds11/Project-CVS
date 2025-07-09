using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [field: SerializeField]
    public AudioSource MusicAudioSource { get; private set; }
    [SerializeField]
    private AudioMixer _audioMixer;
    [SerializeField]
    private GameObject _SFXParent;
    [SerializeField]
    private GameObject _loopAudioParent;


    private List<AudioSource> _SFXAudioSources = new();
    private List<AudioSource> _loopAudioSources = new();
    private int _usedSource;
    private Coroutine _masterLowPassCoroutine;
    public float InitialMusicVolume { get; private set; }


    private void Awake()
    {
        Instance = this;
        _SFXAudioSources = _SFXParent.GetComponentsInChildren<AudioSource>().ToList();
        _loopAudioSources = _loopAudioParent.GetComponentsInChildren<AudioSource>().ToList();
        InitialMusicVolume = MusicAudioSource.volume;
    }

    public void OnPlayerRespawned()
    {
        AdjustMasterLowPass(22000f, 2f);
        foreach (var loopAudioSource in _loopAudioSources)
        {
            loopAudioSource.Stop();
        }
    }

    /// <summary>
    /// Returns index of AudioSource that allows classes to control the AudioSource via AudioManager.
    /// </summary>
    /// <returns></returns>
    public AudioSource PlaySound(Transform tr, AudioClip clip, bool follow = true, bool loop = false, float volume = 1f,
        float pitch = 1f)
    {
        if (!clip)
        {
            Debug.LogWarning("No audio clip!", tr);
            return null;
        }

        var audioSource = _SFXAudioSources[_usedSource];

        if (audioSource.isPlaying)
        {
            foreach (var source in _SFXAudioSources)
            {
                if (!source.isPlaying)
                {
                    audioSource = source;
                    _usedSource = _SFXAudioSources.IndexOf(source);
                    break;
                }
            }
        }
        else
        {
            _usedSource++;
            if (_usedSource >= _SFXAudioSources.Count) _usedSource = 0;
        }

        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.transform.position = tr.position;
        if (follow) audioSource.gameObject.GetComponent<Follower>().SetTarget(tr);
        audioSource.clip = clip;
        audioSource.Play();
        return audioSource;
    }

    public AudioSource PlaySoundLoop(Transform tr, AudioClip clip, bool follow = true, float volume = 1f,
        float pitch = 1f)
    {
        AudioSource audioSource = null;

        foreach (var loopAudioSource in _loopAudioSources)
        {
            if (!loopAudioSource.isPlaying)
            {
                audioSource = loopAudioSource;
                break;
            }
        }

        if (!audioSource)
        {
            Debug.LogWarning($"No loopAudioSource available");
            return null;
        }

        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.transform.position = tr.position;
        if (follow) audioSource.gameObject.GetComponent<Follower>().SetTarget(tr);
        audioSource.clip = clip;
        audioSource.Play();
        return audioSource;
    }

    public void AdjustMusicGroupGain(float newGain)
    {
        _audioMixer.SetFloat("MusicGain", newGain);
    }

    public void AdjustAmbienceGroupGain(float newGain)
    {
        _audioMixer.SetFloat("AmbienceGain", newGain);
    }

    public void AdjustMasterLowPass(float cutoffFreq, float duration)
    {
        if (_masterLowPassCoroutine != null) StopCoroutine(_masterLowPassCoroutine);
        _masterLowPassCoroutine = StartCoroutine(AdjustMasterLowPassCoroutine(cutoffFreq, duration));
    }

    private IEnumerator AdjustMasterLowPassCoroutine(float desiredFreq, float duration)
    {
        float timeElapsed = 0f;
        _audioMixer.GetFloat("MasterLowpassCutoffFreq", out var currentFreq);

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float newFreq = Mathf.Lerp(currentFreq, desiredFreq, timeElapsed / duration);

            _audioMixer.SetFloat("MasterLowpassCutoffFreq", newFreq);
            yield return null;
        }

        _audioMixer.SetFloat("MasterLowpassCutoffFreq", desiredFreq);
        _masterLowPassCoroutine = null;
    }
}