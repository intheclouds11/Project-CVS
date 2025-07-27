using System;
using System.Collections;
using System.Linq;
using NaughtyAttributes;
#if UNITY_EDITOR
using Unity.Cinemachine;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField]
    private AudioClip _unlockCheckpointSFX;
    [SerializeField]
    private Animator _animator;

    private AudioSource _spawnAudio;


    public void PlaySpawnAudio()
    {
        _spawnAudio.Play();
    }

    public void Activate(bool reachedCheckpoint)
    {
        enabled = true;
        _animator.gameObject.SetActive(true);

        if (reachedCheckpoint)
        {
            _animator.SetTrigger("Checkpoint");
            AudioManager.Instance.PlaySound(transform, _unlockCheckpointSFX, true, false, 0.5f, 1.2f);
        }
    }

    public void Deactivate(bool reachedCheckpoint)
    {
        enabled = false;
        _animator.gameObject.SetActive(false);

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

        var notAUniqueName = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None).ToList()
            .Any(sp => sp.name.Equals(_playerSpawnPoint.name) && sp != _playerSpawnPoint);
        if (notAUniqueName)
        {
            Debug.LogWarning($"PlayerSpawnPoint {_playerSpawnPoint.name} does not have a unique name.");
            _playerSpawnPoint.enabled = false;
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(25f);
        if (GUILayout.Button("Save Checkpoint"))
        {
            ES3.Save("SpawnPointName", _playerSpawnPoint.name);
            Debug.Log($"Saved SpawnPointName: {_playerSpawnPoint.name}");
        }

        if (_wasDisabled && _playerSpawnPoint.enabled)
        {
            _wasDisabled = false;

            FindAnyObjectByType<CinemachineCamera>().Target.TrackingTarget = _playerSpawnPoint.transform;

            var otherEnabledSpawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None).ToList()
                .Where(sp => sp != _playerSpawnPoint && sp.enabled);
            foreach (var otherSpawnPoint in otherEnabledSpawnPoints)
            {
                otherSpawnPoint.enabled = false;
                Debug.Log($"Disabled {otherSpawnPoint.name}", otherSpawnPoint);
            }
        }
        else if (!_wasDisabled && !_playerSpawnPoint.enabled)
        {
            _wasDisabled = true;
        }
    }
}
#endif