using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class SawBlade : MonoBehaviour
{
    [SerializeField]
    private float _baseDamage = 20f;
    [SerializeField]
    private float _critDamage = 120f;
    [SerializeField]
    private float _impulseForce = 5f;
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

    private float _newDamage;
    private float _newImpulseForce;
    private int _loopAudioSourceIndex;
    private bool _isCritAttack;
    private float _spawnTime;
    private bool _isReturning;
    private Rigidbody _rb;
    private CinemachineImpulseSource _impulseSource;


    private void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        Vector3 forceToAdd = transform.forward * _newImpulseForce;
        _rb.AddForce(forceToAdd, ForceMode.Impulse);

        // AudioManager.Instance.PlaySound(transform, _launchSfx, true, false, 0.6f);
        Invoke(nameof(PlayLoopAudio), 0.1f);
    }

    private void Update()
    {
        transform.rotation *= Quaternion.AngleAxis(transform.eulerAngles.y + Time.deltaTime * 360, Vector3.up);

        if (!_isReturning && _spawnTime + _returnStartTime <= Time.time)
        {
            _isReturning = true;
            var dir = (GameManager.Instance.Player1.transform.position - transform.position).normalized;
            Vector3 forceToAdd = new Vector3(dir.x, 0f, dir.z) * (_newImpulseForce * 4f);
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

        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            var enemyHit = other.GetComponentInParent<BaseEnemy>();
            if (enemyHit)
            {
                if (_isCritAttack) _impulseSource.GenerateImpulse();
                enemyHit.Health.TakeDamage(_newDamage);
            }

            PlayEffects();
        }

        Reset();
    }

    private void Reset()
    {
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Weapon");
        _rb.linearVelocity = Vector3.zero;
        _isReturning = false;
        transform.parent = GameManager.Instance.Player1.transform;
        gameObject.SetActive(false);
    }

    private void PlayEffects()
    {
        Instantiate(_impactVfx, transform.position, Quaternion.LookRotation(-transform.forward));

        AudioManager.Instance.PlaySound(transform, _impactSfx, true, false, 0.75f);
    }

    public void SetProperties(float chargeAmount, bool crit)
    {
        _newImpulseForce = _impulseForce * chargeAmount;
        _newDamage = crit ? _critDamage : _baseDamage * chargeAmount;
        Debug.Log($"currentDMG: {_newDamage}");
        _isCritAttack = crit;
    }
}