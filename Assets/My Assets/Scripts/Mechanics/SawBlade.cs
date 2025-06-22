using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class SawBlade : MonoBehaviour
{
    [SerializeField]
    private float _baseDamage = 1f;
    [SerializeField]
    private float _critDamage = 3f;
    [SerializeField]
    private float _speed = 10f;
    [SerializeField]
    private float _returnStartTime = 1.5f;
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
    private bool _isCritAttack;
    private float _spawnTime;
    private bool _isReturning;
    private Rigidbody _rb;


    private void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _spawnTime = Time.time;
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // AudioManager.Instance.PlaySound(transform, _launchSfx, true, false, 0.6f);
        Invoke(nameof(PlayLoopAudio), 0.1f);

        Vector3 forceToAdd = transform.forward * _speed;
        _rb.AddForce(forceToAdd, ForceMode.Impulse);
    }

    private void Update()
    {
        if (!_isReturning && _spawnTime + _returnStartTime <= Time.time)
        {
            _isReturning = true;
            var dir = (GameManager.Instance.Player1.transform.position - transform.position).normalized;
            Vector3 forceToAdd = new Vector3(dir.x, 0f, dir.z) * (_speed * 2f);
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(forceToAdd, ForceMode.Impulse);
            transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");
        }
    }

    private void PlayLoopAudio()
    {
        _loopAudioSourceIndex = AudioManager.Instance.PlaySound(transform, _bladeSpinLoopSfx, true, true, 0.6f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"Hit: {other.transform.name}", other.transform);

        AudioManager.Instance.StopSound(_loopAudioSourceIndex);

        var enemyHit = other.GetComponentInParent<BaseEnemy>();
        if (enemyHit)
        {
            if (_isCritAttack) _impulseSource.GenerateImpulse();
            enemyHit.Health.TakeDamage(_isCritAttack ? _critDamage : _baseDamage);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Destroy(gameObject);
            return;
        }

        PlayEffects();
        Destroy(gameObject);
    }

    private void PlayEffects()
    {
        Instantiate(_impactVfx, transform.position, Quaternion.LookRotation(-transform.forward));

        AudioManager.Instance.PlaySound(transform, _impactSfx, true, false, 0.75f);
    }

    public void SetIsCritAttack(bool crit)
    {
        _isCritAttack = crit;
    }
}