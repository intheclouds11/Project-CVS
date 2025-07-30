using UnityEngine;

public class Torus : BaseEnemy
{
	
	
	protected override void OnTriggerEnter(Collider other)
	{
		base.OnTriggerEnter(other);
		if (enabled && other.gameObject.CompareTag("Player"))
		{
			var playerHit = other.GetComponent<PlayerController>();
			if (playerHit)
			{
				_damagePlayerKnockback.Direction = (playerHit.transform.position - transform.position).normalized;
				playerHit.Health.TakeDamage(_baseDamage, _damagePlayerKnockback);
				OnDamagedPlayer();
			}
		}
	}
}
