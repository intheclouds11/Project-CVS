using System;
using System.Collections;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

public class PlayerSpawnPoint : MonoBehaviour
{
    public static event Action<PlayerController> PlayerSpawned;

    [SerializeField]
    private bool _activeSpawnPoint;
    [SerializeField]
    private GameObject _playerPrefab;
    [SerializeField]
    private float _allowInputsDelay = 1f;

    private Vector3 _spawnPosition;
    private static PlayerSpawnPoint _savedSpawnPoint;


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
        if (!_savedSpawnPoint) LoadSavedSpawnPoint();
        if (!_activeSpawnPoint)
        {
            gameObject.SetActive(false);
            return;
        }

        PlayerController spawnedPlayer = GameManager.Instance.Player1;
        
        if (!spawnedPlayer)
        {
            spawnedPlayer = Spawn();
        }
        else
        {
            spawnedPlayer.Respawn(_spawnPosition, Quaternion.identity);
        }

        StartCoroutine(GiveControlCoroutine());
        PlayerSpawned?.Invoke(spawnedPlayer);
    }

    private static void LoadSavedSpawnPoint()
    {
        // Debug.Log($"Attempting to load savedSpawnPoint");
        try
        {
            _savedSpawnPoint = ES3.Load<PlayerSpawnPoint>("CurrentSpawnPoint");
            if (_savedSpawnPoint)
            {
                _savedSpawnPoint.ActivateSpawnPoint(false);
            }
            else
            {
                Debug.Log($"Couldn't load CurrentSpawnPoint");
            }
        }
        catch
        {
            // ignored
        }
    }

    private PlayerController Spawn()
    {
        var player = Instantiate(_playerPrefab, _spawnPosition, Quaternion.identity).GetComponent<PlayerController>();
        DontDestroyOnLoad(player);
        return player;
    }

    private IEnumerator GiveControlCoroutine()
    {
        yield return new WaitForSeconds(_allowInputsDelay);
        InputManager.Instance.ToggleInputsAllowed(true);
    }

    public void ActivateSpawnPoint(bool reachedCheckpoint)
    {
        _activeSpawnPoint = true;
        gameObject.SetActive(true);
        // TODO visual polish: play SpawnPointActivated animation (barrier creeps up to knee height)

        if (reachedCheckpoint)
        {
            ES3.Save("CurrentSpawnPoint", this);
            Debug.Log($"Saved CurrentSpawnPoint");
        }

        var spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None).ToList();
        foreach (var spawnPoint in spawnPoints.Where(spawnPoint => spawnPoint != this))
        {
            spawnPoint.DeactivateSpawnPoint();
        }
    }

    public void DeactivateSpawnPoint()
    {
        _activeSpawnPoint = false;
        gameObject.SetActive(false);
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