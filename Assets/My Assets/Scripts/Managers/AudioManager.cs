using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private static List<AudioSource> _audioSources = new();

    private void Awake()
    {
        Instance = this;
        _audioSources = GetComponentsInChildren<AudioSource>().ToList();
    }

    /// <summary>
    /// Returns index of AudioSource that allows classes to control the AudioSource via AudioManager.
    /// </summary>
    /// <returns></returns>
    public static int PlaySound(Transform tr, AudioClip clip, bool follow = true, bool loop = false)
    {
        for (int i = 0; i < _audioSources.Count; i++)
        {
            var audioSource = _audioSources[i];
            if (!audioSource.isPlaying)
            {
                audioSource.loop = loop;
                audioSource.transform.position = tr.position;
                if (follow) audioSource.gameObject.GetComponent<Follower>().SetTarget(tr);
                audioSource.PlayOneShot(clip);
                return i;
            }
        }

        return -1;
    }

    public void StopSound(int index, bool detach = true)
    {
        var audioSource = _audioSources[index];
        if (detach)
        {
            audioSource.transform.parent = transform;
            audioSource.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        audioSource.Stop();
    }
}