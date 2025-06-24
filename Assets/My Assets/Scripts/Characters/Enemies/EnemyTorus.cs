using System;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyTorus : BaseEnemy
{
    [SerializeField]
    private float _agroRange = 5f;
    [SerializeField]
    private float _disengageRange = 10f;
    [SerializeField]
    private float _moveSpeed = 1f;

    private bool _chasePlayer;
    

    private void Update()
    {
        var distToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        if (distToPlayer <= _agroRange)
        {
            _chasePlayer = true;
        }

        if (_chasePlayer && _player.Health.IsAlive())
        {
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, _moveSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var playerHit = other.GetComponent<PlayerController>();
        if (playerHit)
        {
            playerHit.Health.TakeDamage(1);
        }
    }
}
