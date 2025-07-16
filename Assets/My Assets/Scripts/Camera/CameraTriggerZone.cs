using UnityEngine;
using UnityEngine.Serialization;

public class CameraTriggerZone : MonoBehaviour
{
    [Header("Zoom")]
    [field: SerializeField]
    public float ZoomSpeed { get; private set; } = 0.5f;
    [field: SerializeField]
    public float ZoomModifier { get; private set; } = 1.1f;
    
    [Header("Pan")]
    [field: SerializeField]
    public float PanSpeed { get; private set; } = 0.5f;
    [field: SerializeField]
    public float PanModifier { get; private set; } = 5f;
    
    [Header("Pan")]
    [field: SerializeField]
    public float PitchSpeed { get; private set; } = 0.5f;
    [field: SerializeField]
    public float PitchModifier { get; private set; } = 5f;
    

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CameraTriggerZoneManager.Instance.EnteredZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CameraTriggerZoneManager.Instance.ExitedZone(this);
    }
}