using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private List<AudioSource> _audioSources = new();
    private int _usedSource;

    private void Awake()
    {
        Instance = this;
        _audioSources = GetComponentsInChildren<AudioSource>().ToList();
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

        var audioSource = _audioSources[_usedSource];
        
        _usedSource++;
        if (_usedSource >= _audioSources.Count) _usedSource = 0;

        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.transform.position = tr.position;
        if (follow) audioSource.gameObject.GetComponent<Follower>().SetTarget(tr);
        audioSource.clip = clip;
        audioSource.Play();
        return audioSource;
    }
}