using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField]
    private float _inputsEnabledDelay = 1f;

    public static PlayerSpawnManager Instance;
    public PlayerSpawnPoint _activeSpawnPoint { get; private set; }
    public static event Action<PlayerController> PlayerSpawned;

    private List<PlayerSpawnPoint> _spawnPoints = new();


    private void Awake()
    {
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene loadedScene, LoadSceneMode arg1)
    {
        if (loadedScene.name == "MainMenu") return;

        _spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None).ToList();

        TryLoadSavedSpawnPoint();

        PlayerController spawnedPlayer = GameManager.Instance.Player1;
        if (!spawnedPlayer)
        {
            spawnedPlayer = Spawn(_activeSpawnPoint);
        }
        else
        {
            spawnedPlayer.Respawn(_activeSpawnPoint);
        }

#if UNITY_EDITOR
        if (EditorPrefs.GetBool("spawnAtCursor"))
        {
            Debug.Log($"Spawned player at cursor world hit position");
            var sceneViewCursorWorldPosition = new Vector3(EditorPrefs.GetFloat("SpawnX"), EditorPrefs.GetFloat("SpawnY"),
                EditorPrefs.GetFloat("SpawnZ"));
            spawnedPlayer.transform.position = sceneViewCursorWorldPosition;
        }
#endif

        StartCoroutine(GiveControlCoroutine());
        PlayerSpawned?.Invoke(spawnedPlayer);
    }

    public void TryLoadSavedSpawnPoint()
    {
        try
        {
            var savedSpawnPointName = ES3.Load<string>("SpawnPointName");
            foreach (var spawnPoint in _spawnPoints.Where(p => p.name == savedSpawnPointName))
            {
                ActivateSpawnPoint(spawnPoint, false);
                break;
            }
        }
        catch
        {
            _activeSpawnPoint = _spawnPoints.Find(sp => sp.enabled);
            if (!_activeSpawnPoint)
            {
                Debug.LogError($"No activeSpawnPoint found");
            }
            else
            {
                // Debug.Log($"Tried loading saved SpawnPointName, but nothing found. _activeSpawnPoint set to {_activeSpawnPoint}");
            }
        }
    }

    private PlayerController Spawn(PlayerSpawnPoint spawnPoint)
    {
        var loadedPlayer = Resources.Load<GameObject>("Player");
        var player = Instantiate(loadedPlayer, spawnPoint.transform.position, Quaternion.identity).GetComponent<PlayerController>();
        player.RotationTransform.rotation = spawnPoint.transform.rotation;
        DontDestroyOnLoad(player);
        return player;
    }

    private IEnumerator GiveControlCoroutine()
    {
        yield return new WaitForSeconds(_inputsEnabledDelay);
        InputManager.Instance.ToggleInputsAllowed(true);
    }

    public void ActivateSpawnPoint(PlayerSpawnPoint spawnPoint, bool reachedCheckpoint)
    {
        spawnPoint.Activate(reachedCheckpoint);
        _activeSpawnPoint = spawnPoint;

        // TODO visual polish: play SpawnPointActivated animation (barrier creeps up to knee height)
        // TODO: play checkpoint unlocked sfx or show save icon in bottom corner of screen

        if (reachedCheckpoint)
        {
            ES3.Save("SpawnPointName", spawnPoint.gameObject.name);
            // Debug.Log($"Saved SpawnPointName {spawnPoint.gameObject.name}");
        }

        foreach (var sp in _spawnPoints.Where(p => p != _activeSpawnPoint))
        {
            sp.Deactivate(reachedCheckpoint);
        }
    }
    
    public static void ClearSavedSpawnPoint()
    {
        ES3.DeleteKey("SpawnPointName");
    }


#if UNITY_EDITOR

    [MenuItem("Tools/ClearSavedSpawnPoint")]
    public static void Menu_ClearSavedSpawnPoint()
    {
        ClearSavedSpawnPoint();
    }

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
}