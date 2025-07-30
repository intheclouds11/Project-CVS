using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Spinner : MonoBehaviour
{
    [SerializeField]
    private float _degreesPerSecond = 360f;
    [SerializeField]
    private AudioClip _damageSFX;
    [SerializeField]
    private Knockback _knockback;
    
  
    private void Update()
    {
        transform.Rotate(Vector3.up, _degreesPerSecond * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            var playerHit = other.GetComponent<PlayerController>();
            var enemyHit = other.GetComponentInParent<BaseEnemy>();
            if (playerHit && playerHit.Health.IsAlive())
            {
                _knockback.Direction = (playerHit.transform.position - transform.position).normalized;
                playerHit.Health.TakeDamage(int.MaxValue, _knockback);
            }
            else if (enemyHit && enemyHit.Health.IsAlive())
            {
                _knockback.Direction = (enemyHit.transform.position - transform.position).normalized;
                enemyHit.Health.TakeDamage(int.MaxValue, null, true);
            }

            AudioManager.Instance.PlaySound(transform, _damageSFX);
        }
    }
}
