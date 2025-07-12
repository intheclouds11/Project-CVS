using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;
using Object = UnityEngine.Object;

public class PlayerSpawnPoint : MonoBehaviour
{
    public static event Action<PlayerController> PlayerSpawned;

    [SerializeField]
    private bool _activeRespawnPoint;
    [SerializeField]
    private GameObject _playerPrefab;

    private PlayerController _player;
    private Vector3 _spawnPosition;


    private void Awake()
    {
        _spawnPosition = transform.position;

#if UNITY_EDITOR
        if (EditorPrefs.GetBool("spawnAtCursor"))
        {
            var sceneViewCursorWorldPosition = new Vector3(EditorPrefs.GetFloat("SpawnX"), EditorPrefs.GetFloat("SpawnY"),
                EditorPrefs.GetFloat("SpawnZ"));
            _spawnPosition = sceneViewCursorWorldPosition;
        }
#endif
    }

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
            _player.Respawn(_spawnPosition, Quaternion.identity, true);
        }

        PlayerSpawned?.Invoke(_player);
        // gameObject.SetActive(false);
    }

    public PlayerController Spawn()
    {
        var player = Instantiate(_playerPrefab, _spawnPosition, Quaternion.identity).GetComponent<PlayerController>();
        DontDestroyOnLoad(player);
        return player;
    }
}

#if UNITY_EDITOR

[InitializeOnLoad]
public static class PlayModeSpawnPointSetter
{
    static PlayModeSpawnPointSetter()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (!Keyboard.current.ctrlKey.isPressed && !Keyboard.current.pKey.isPressed)
            {
                EditorPrefs.SetBool("spawnAtCursor", false);
                return;
            }
            
            SceneView sceneView = SceneView.lastActiveSceneView;

            if (EditorWindow.focusedWindow != sceneView)
            {
                EditorPrefs.SetBool("spawnAtCursor", false);
                Debug.LogWarning("[Scene Spawn] Scene View not focused. spawnAtCursor set to false.");

                return;
            }

            if (sceneView == null)
            {
                Debug.LogWarning("No Scene View available.");
                return;
            }

            Vector2 mousePos = Event.current?.mousePosition ?? new Vector2(Screen.width / 2f, Screen.height / 2f);
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMask.GetMask("Ground")))
            {
                Vector3 hitPoint = hit.point;
                EditorPrefs.SetBool("spawnAtCursor", true);
                EditorPrefs.SetFloat("SpawnX", hitPoint.x);
                EditorPrefs.SetFloat("SpawnY", hitPoint.y);
                EditorPrefs.SetFloat("SpawnZ", hitPoint.z);
                Debug.Log($"[Scene Spawn] Saved spawn point: {hitPoint}. spawnAtCursor set to true!");
            }
            else
            {
                Debug.LogWarning("[Scene Spawn] Raycast did not hit ground.");
            }
        }
    }
}
#endif