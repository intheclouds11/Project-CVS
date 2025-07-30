using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class CustomAudioStart : MonoBehaviour
{
    [SerializeField]
    private bool _startTimeOffset;
    [field: SerializeField, ShowIf(nameof(_startTimeOffset))]
    private float _maxOffset = 2f;
    [field: SerializeField, ShowIf(nameof(_startTimeOffset))]
    private float _minOffset = 0.1f;
    [SerializeField]
    private bool _fadeIn;
    [field: SerializeField, ShowIf(nameof(_fadeIn))]
    private float _fadeInDuration = 2f;
    [field: SerializeField, ShowIf(nameof(_fadeIn))]
    private float _fadeInVolumeTarget;

    private AudioSource _audioSource;


    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_startTimeOffset)
            _audioSource.time = Mathf.Clamp(Random.Range(_minOffset, _maxOffset), _minOffset, _audioSource.clip.length);
        if (_fadeIn)
            StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        if (_fadeInVolumeTarget == 0) _fadeInVolumeTarget = _audioSource.volume;
        _audioSource.volume = 0f;
        
        while (_audioSource.volume < _fadeInVolumeTarget)
        {
            _audioSource.volume += Time.deltaTime / _fadeInDuration;
            yield return null;
        }

        _audioSource.volume = _fadeInVolumeTarget;
    }
}