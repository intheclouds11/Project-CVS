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
    private float _returnMaxDistance = 5f;
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
    private int _loopAudioSourceIndex = -1;
    public bool IsCritAttack { get; private set; }
    private float _spawnTime;
    private bool _isReturning;
    private Rigidbody _rb;
    private PlayerController _player;
    private CinemachineImpulseSource _impulseSource;


    private void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _player = GameManager.Instance.Player1;
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        Vector3 forceToAdd = transform.forward * _newImpulseForce;
        _rb.AddForce(forceToAdd, ForceMode.Impulse);

        PlayLoopAudio();
    }

    private void Update()
    {
        transform.rotation *= Quaternion.AngleAxis(transform.eulerAngles.y + Time.deltaTime * 360, Vector3.up);

        if (_isReturning) return;

        if (Time.time >= _spawnTime + _returnStartTime) // When returnStartTime is reached
        {
            if (IsCritAttack)
            {
                _rb.linearVelocity = Vector3.zero;
            }
            else
            {
                ReturnToPlayer();
            }
        }

        if (Vector3.Distance(transform.position, _player.transform.position) >= _returnMaxDistance)
        {
            ReturnToPlayer();
        }
    }

    public void ReturnToPlayer()
    {
        _isReturning = true;
        var dir = (_player.transform.position - transform.position).normalized;
        Vector3 forceToAdd = new Vector3(dir.x, 0f, dir.z) * (_impulseForce * 4f);
        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(forceToAdd, ForceMode.Impulse);
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void PlayLoopAudio()
    {
        AudioManager.Instance.StopSound(_loopAudioSourceIndex);
        _loopAudioSourceIndex = AudioManager.Instance.PlaySound(transform, _bladeSpinLoopSfx, true, true, 0.6f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"Hit: {other.transform.name}", other.transform);
        
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            var enemyHit = other.GetComponentInParent<BaseEnemy>();
            if (enemyHit)
            {
                if (IsCritAttack) _impulseSource.GenerateImpulse();
                enemyHit.Health.TakeDamage(_newDamage);
            }

            PlayEffects();
        }

        ResetToDefaultState();
    }

    public void ResetToDefaultState()
    {
        AudioManager.Instance.StopSound(_loopAudioSourceIndex);
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Weapon");
        _rb.linearVelocity = Vector3.zero;
        _isReturning = false;
        transform.parent = _player.transform;
        gameObject.SetActive(false);
    }

    private void PlayEffects()
    {
        Instantiate(_impactVfx, transform.position, Quaternion.LookRotation(-transform.forward));

        AudioManager.Instance.PlaySound(transform, _impactSfx, true, false, 0.75f);
    }

    public void OnAttack(float chargeAmount, bool crit)
    {
        _newImpulseForce = _impulseForce * chargeAmount;
        _newDamage = crit ? _critDamage : _baseDamage * chargeAmount;
        IsCritAttack = crit;
        // Debug.Log($"currentDMG: {_newDamage}");
    }
}