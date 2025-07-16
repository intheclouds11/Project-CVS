using System;
using System.Collections;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField]
    private AudioClip _enabledSFX;
    
    public void Activate(bool reachedCheckpoint)
    {
        enabled = true;

        if (reachedCheckpoint)
        {
            // TODO: VFX and sound
            AudioManager.Instance.PlaySound(transform, _enabledSFX, true, false, 0.8f, 1.2f);
        }
    }

    public void Deactivate(bool reachedCheckpoint)
    {
        enabled = false;
        if (reachedCheckpoint)
        {
            // VFX?
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerSpawnPoint))]
public class PlayerSpawnPointEditor : Editor
{
    private PlayerSpawnPoint _playerSpawnPoint;
    private bool _wasDisabled;

    private void OnEnable()
    {
        _playerSpawnPoint = target as PlayerSpawnPoint;
        _wasDisabled = !_playerSpawnPoint.enabled;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (_wasDisabled && _playerSpawnPoint.enabled)
        {
            _wasDisabled = false;

            var otherEnabledSpawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None).ToList()
                .Where(sp => sp != _playerSpawnPoint && sp.enabled);
            foreach (var otherSpawnPoint in otherEnabledSpawnPoints)
            {
                otherSpawnPoint.enabled = false;
                Debug.Log($"Disabled {otherSpawnPoint}", otherSpawnPoint);
            }
        }
        else if (!_wasDisabled && !_playerSpawnPoint.enabled)
        {
            _wasDisabled = true;
        }
    }
}
#endif