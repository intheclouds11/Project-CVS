using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class SawBlade : MonoBehaviour
{
    [SerializeField]
    private float _damage = 5f;
    [SerializeField]
    private float _speed = 10f;
    [SerializeField]
    private AudioClip _launchSfx;
    [SerializeField]
    private AudioClip _bladeSpinLoopSfx;
    [SerializeField]
    private AudioClip _impactSfx;
    [SerializeField]
    private GameObject _impactVfx;

    private CinemachineImpulseSource _impulseSource;
    private int _loopAudioSourceIndex;


    private void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Start()
    {
        AudioManager.Instance.PlaySound(transform, _launchSfx, true, false, 0.6f);
        Invoke(nameof(PlayLoopAudio), 0.1f);

        Vector3 forceToAdd = transform.forward * _speed;
        GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
    }

    private void PlayLoopAudio()
    {
        _loopAudioSourceIndex = AudioManager.Instance.PlaySound(transform, _bladeSpinLoopSfx, true, true, 0.6f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"Hit: {other.transform.name}", other.transform);

        AudioManager.Instance.StopSound(_loopAudioSourceIndex);
        _impulseSource.GenerateImpulse();

        var enemyHit = other.GetComponentInParent<BaseEnemy>();
        if (enemyHit)
        {
            enemyHit.Health.TakeDamage(_damage);
        }

        PlayEffects();
        Destroy(gameObject);
    }

    private void PlayEffects()
    {
        Instantiate(_impactVfx, transform.position, Quaternion.LookRotation(-transform.forward));

        AudioManager.Instance.PlaySound(transform, _impactSfx);
    }
}