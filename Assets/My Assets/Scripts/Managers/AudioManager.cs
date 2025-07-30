using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField]
    private AudioClip _invincibleImpactSFX;
    [SerializeField]
    private float _invincibleImpactVolume = 1f;
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

    private void Update()
    {
        if (InputManager.Instance.ToggleMusicWasPressed)
        {
            if (GetMusicGroupGain() <= -70f)
            {
                SetMusicGroupGain(0f);
                PlayerPrefs.SetFloat("MusicVolume", 1f);
            }
            else
            {
                SetMusicGroupGain(-80f);
                PlayerPrefs.SetFloat("MusicVolume", 0f);
            }
        }
    }

    public void OnPlayerRespawned()
    {
        AdjustMasterLowPass(22000f, 1f);
    }

    /// <summary>
    /// Returns index of AudioSource that allows classes to control the AudioSource via AudioManager.
    /// </summary>
    /// <returns></returns>
    public AudioSource PlaySound(Transform tr, AudioClip clip, bool follow = true, bool loop = false, float volume = 1f,
        float pitch = 1f, float spatialBlend = 1f)
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
        audioSource.spatialBlend = spatialBlend;
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

    public void PlayInvincibleImpact(Transform t)
    {
        var pitch = Random.Range(0.9f, 1.1f);
        PlaySound(t, _invincibleImpactSFX, true, false, _invincibleImpactVolume, pitch);
    }

    public float GetMusicGroupGain()
    {
        _audioMixer.GetFloat("MusicGain", out var gain);
        return gain;
    }

    public void SetMusicGroupGain(float newGain)
    {
        _audioMixer.SetFloat("MusicGain", newGain);
    }

    public void AdjustMusicGroupGain(float offset)
    {
        _audioMixer.SetFloat("MusicGain", GetMusicGroupGain() + offset);
    }

    public float GetAmbienceGroupGain()
    {
        _audioMixer.GetFloat("AmbienceGain", out var gain);
        return gain;
    }

    public void SetAmbienceGroupGain(float newGain)
    {
        _audioMixer.SetFloat("AmbienceGain", newGain);
    }

    public void AdjustAmbienceGroupGain(float offset)
    {
        _audioMixer.SetFloat("AmbienceGain", GetAmbienceGroupGain() + offset);
    }

    public float GetSFXGroupGain()
    {
        _audioMixer.GetFloat("SFXGain", out var gain);
        return gain;
    }

    public void SetSFXGroupGain(float newGain)
    {
        _audioMixer.SetFloat("SFXGain", newGain);
    }

    public void AdjustSFXGroupGain(float offset)
    {
        _audioMixer.SetFloat("SFXGain", GetSFXGroupGain() - offset);
    }

    public void AdjustMasterLowPass(float cutoffFreq, float duration = 0f)
    {
        if (duration > 0)
        {
            if (_masterLowPassCoroutine != null) StopCoroutine(_masterLowPassCoroutine);
            _masterLowPassCoroutine = StartCoroutine(AdjustMasterLowPassCoroutine(cutoffFreq, duration));
        }
        else
        {
            _audioMixer.SetFloat("MasterLowpassCutoffFreq", 22000f);
        }
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

    public void StartNewMusic(AudioClip newClip, float volume)
    {
        if (MusicAudioSource.clip == newClip) return;
        MusicAudioSource.clip = newClip;
        MusicAudioSource.volume = volume;
        if (newClip) MusicAudioSource.Play();
    }

    private Coroutine _transitionMusicCoroutine;

    public void TransitionMusic(AudioClip newClip, float volume, float fadeOutDuration, float fadeInDuration)
    {
        if (_transitionMusicCoroutine != null) StopCoroutine(_transitionMusicCoroutine);
        _transitionMusicCoroutine = StartCoroutine(TransitionMusicCoroutine(newClip, volume, fadeOutDuration, fadeInDuration));
    }

    private IEnumerator TransitionMusicCoroutine(AudioClip newClip, float volume, float fadeOutDuration, float fadeInDuration)
    {
        if (fadeOutDuration > 0 && MusicAudioSource.isPlaying)
        {
            var startVolume = MusicAudioSource.volume;
            while (MusicAudioSource.volume > 0)
            {
                MusicAudioSource.volume -= startVolume * Time.deltaTime / fadeOutDuration;
                yield return null;
            }

            MusicAudioSource.volume = 0f;
        }

        MusicAudioSource.clip = newClip;
        if (newClip)
        {
            MusicAudioSource.Play();
            if (fadeInDuration > 0)
            {
                while (MusicAudioSource.volume < volume)
                {
                    MusicAudioSource.volume += Time.deltaTime / fadeInDuration;
                    yield return null;
                }
            }

            MusicAudioSource.volume = volume;
        }

        _transitionMusicCoroutine = null;
    }
}