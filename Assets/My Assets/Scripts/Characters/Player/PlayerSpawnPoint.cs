using System;
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    public static event Action PlayerSpawned;
    [SerializeField]
    private GameObject _playerPrefab;

    private GameObject _player;


    private void Start()
    {
        _player = FindAnyObjectByType<PlayerController>()?.gameObject;

        if (!_player)
        {
            _player = Spawn();
        }
        else
        {
            _player.GetComponent<PlayerController>().Respawn(transform.position, Quaternion.identity, true);
        }

        PlayerSpawned?.Invoke();
        gameObject.SetActive(false);
    }

    public GameObject Spawn()
    {
        return Instantiate(_playerPrefab, transform.position, Quaternion.identity);
    }
}