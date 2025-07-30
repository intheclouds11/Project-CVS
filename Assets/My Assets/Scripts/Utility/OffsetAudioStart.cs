using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class OffsetAudioStart : MonoBehaviour
{
    [SerializeField]
    private AudioSource _audioSource;
    [SerializeField]
    private float _maxOffset = 2f;
    [SerializeField]
    private float _minOffset = 0.1f;


    private void Awake()
    {
        if (!_audioSource) _audioSource = GetComponent<AudioSource>();
        _audioSource.time = Mathf.Clamp(Random.Range(_minOffset, _maxOffset), _minOffset, _audioSource.clip.length);
    }
}