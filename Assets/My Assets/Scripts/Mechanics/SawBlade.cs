using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class SawBlade : MonoBehaviour
{
    public bool isPlayerProjectile;
    
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
    private AudioClip _deflectedSfx;
    [SerializeField]
    private GameObject _impactVfx;
    
    [SerializeField]
    private bool hasSplashDamage;
    [SerializeField]
    private float splashRadius = 0.3f;
    [SerializeField]
    private GameObject _splashVfxPrefab;

    public bool IsDeflected;
    
    private CinemachineImpulseSource _impulseSource;
    private int _loopAudioSourceIndex;


    private void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Start()
    {
        if (isPlayerProjectile)
        {
            GetComponent<Rigidbody>().excludeLayers = LayerMask.GetMask("Friendly");
            // FindAnyObjectByType<CinemachineCamera>().Follow = transform;
        }
        else
        {
            gameObject.tag = "EnemyProjectile";
        }

        AudioManager.PlaySound(transform, _launchSfx);
        Invoke(nameof(PlayLoopAudio), 0.1f);
        
        Vector3 forceToAdd = transform.forward * _speed;
        GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
    }

    private void PlayLoopAudio()
    {
        _loopAudioSourceIndex = AudioManager.PlaySound(transform, _bladeSpinLoopSfx, true, true);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"Hit: {other.transform.name}", other.transform);

        if (isPlayerProjectile && other.attachedRigidbody && other.attachedRigidbody.CompareTag("EnemyProjectile"))
        {
            Debug.Log("DEFLECT PROJECTILE");
            
            other.attachedRigidbody.GetComponent<SawBlade>().IsDeflected = true;
            Vector3 forceToAdd = -other.attachedRigidbody.transform.forward * _speed;
            other.attachedRigidbody.linearVelocity = Vector3.zero;
            other.attachedRigidbody.AddForce(forceToAdd, ForceMode.Impulse);
            
            AudioManager.Instance.StopSound(_loopAudioSourceIndex);
            Destroy(gameObject);
            return;
        }

        StartCoroutine(OnHitCoroutine(other));
    }

    private IEnumerator OnHitCoroutine(Collider other)
    {
        yield return null;
        
        if (IsDeflected)
        {
            AudioManager.PlaySound(transform, _deflectedSfx);
            IsDeflected = false;
            isPlayerProjectile = true;
            yield break;
        }
        
        _impulseSource.GenerateImpulse();

        if (!hasSplashDamage)
        {
            var playerHit = other.GetComponentInParent<PlayerController>();
            var enemyHit = other.GetComponentInParent<BaseEnemy>();
            
            if (playerHit && !isPlayerProjectile)
            {
                playerHit.Health.TakeDamage(_damage);
            }
            else if (enemyHit && isPlayerProjectile)
            {
                enemyHit.Health.TakeDamage(_damage);
            }
        }
        else
        {
            var hitColliders = Physics.OverlapSphere(transform.position, splashRadius);
            foreach (var hitCollider in hitColliders)
            {
                var playerHit = hitCollider.GetComponentInParent<PlayerController>();
                var enemyHit = hitCollider.GetComponentInParent<BaseEnemy>();
                
                if (playerHit && !isPlayerProjectile)
                {
                    playerHit.Health.TakeDamage(_damage);
                }
                else if (enemyHit && isPlayerProjectile)
                {
                    enemyHit.Health.TakeDamage(_damage);
                }
            }
        }

        if (isPlayerProjectile)
        {
            // FindAnyObjectByType<CinemachineCamera>().Follow = GameManager.Instance.player.GetLookAtTransform();
        }
        
        PlayEffects();
        Destroy(gameObject);
    }

    private void PlayEffects()
    {
        if (!hasSplashDamage)
        {
            Instantiate(_impactVfx, transform.position, Quaternion.LookRotation(-transform.forward));
        }
        else
        {
            Instantiate(_splashVfxPrefab, transform.position, _splashVfxPrefab.transform.rotation);
        }

        AudioManager.PlaySound(transform, _impactSfx);
    }
}