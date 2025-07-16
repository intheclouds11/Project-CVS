using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField]
    private PlayerSpawnPoint _newSpawnPoint;
    [SerializeField]
    private List<BaseEnemy> _requiredEnemiesKilled;

    private bool _hasEntered;


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _newSpawnPoint.enabled) return;

        _hasEntered = true;
    }

    private void Update()
    {
        if (_hasEntered && _requiredEnemiesKilled != null && !_requiredEnemiesKilled.Any(e => e.Health.IsAlive()))
        {
            Debug.Log("Checkpoint unlocked");
            PlayerSpawnManager.Instance.ActivateSpawnPoint(_newSpawnPoint, true);
            enabled = false;
        }
    }
}