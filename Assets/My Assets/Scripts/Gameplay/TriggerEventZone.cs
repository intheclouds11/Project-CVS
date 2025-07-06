using System;
using UnityEngine;
using UnityEngine.Events;

public class TriggerEventZone : MonoBehaviour
{
    [SerializeField]
    private UnityEvent _eventToTrigger;

    private bool _wasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_wasTriggered || other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        _wasTriggered = true;
        
        _eventToTrigger.Invoke();
    }
}
