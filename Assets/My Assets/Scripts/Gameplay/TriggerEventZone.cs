using System;
using UnityEngine;
using UnityEngine.Events;

public class TriggerEventZone : MonoBehaviour
{
    [SerializeField]
    private UnityEvent _eventToTrigger;
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        _eventToTrigger.Invoke();
        
        GetComponent<Collider>().enabled = false;
    }
}