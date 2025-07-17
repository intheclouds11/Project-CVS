using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public static bool SaveFileExists()
    {
        try
        {
            ES3.Load<string>("SpawnPointName");
            return true;
        }
        catch
        {
            // Debug.Log($"Tried loading saved SpawnPointName, but nothing found. _activeSpawnPoint set to {_activeSpawnPoint}");
            return false;
        }
    }

    public static bool TryGetSavedSpawnPointName(out string savedSpawnPointName)
    {
        try
        {
            savedSpawnPointName = ES3.Load<string>("SpawnPointName");
            return true;
        }
        catch
        {
            // Debug.Log($"Tried loading saved SpawnPointName, but nothing found. _activeSpawnPoint set to {_activeSpawnPoint}");
            savedSpawnPointName = null;
            return false;
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
#endif
}