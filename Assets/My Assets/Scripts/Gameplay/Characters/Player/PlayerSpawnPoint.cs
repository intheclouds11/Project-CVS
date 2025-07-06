using System;
using System.Linq;
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    public static event Action<PlayerController> PlayerSpawned;
    
    [SerializeField]
    private bool _activeRespawnPoint;
    [SerializeField]
    private GameObject _playerPrefab;

    private PlayerController _player;


    private void Start()
    {
        if (!_activeRespawnPoint) return;
        
        _player = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();

        if (!_player)
        {
            _player = Spawn();
        }
        else
        {
            _player.Respawn(transform.position, Quaternion.identity, true);
        }

        PlayerSpawned?.Invoke(_player);
        gameObject.SetActive(false);
    }

    public PlayerController Spawn()
    {
        var player = Instantiate(_playerPrefab, transform.position, Quaternion.identity).GetComponent<PlayerController>();
        DontDestroyOnLoad(player);
        return player;
    }
}