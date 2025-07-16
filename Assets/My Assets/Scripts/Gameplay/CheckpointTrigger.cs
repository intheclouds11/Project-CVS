using System;
using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField]
    private PlayerSpawnPoint _newSpawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        _newSpawnPoint.ActivateSpawnPoint(true);
    }
}
