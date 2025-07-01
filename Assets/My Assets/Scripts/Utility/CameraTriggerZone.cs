using UnityEngine;
using UnityEngine.Serialization;

public class CameraTriggerZone : MonoBehaviour
{
    [field: SerializeField]
    public float ZoomSpeed { get; private set; } = 0.5f;
    [field: SerializeField]
    public float ZoomModifier { get; private set; } = 1.1f;
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        CameraTriggerZoneManager.Instance.EnteredZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        CameraTriggerZoneManager.Instance.ExitedZone(this);
    }
}