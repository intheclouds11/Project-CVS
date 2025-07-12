using System.Collections.Generic;
using UnityEngine;

public class MultiProjectilePool : MonoBehaviour
{
    [System.Serializable]
    public class PoolEntry
    {
        public GameObject prefab;
        public int size;
    }

    [SerializeField] private PoolEntry[] pools;

    private Dictionary<string, Queue<GameObject>> poolDict = new();

    private void Awake()
    {
        foreach (var entry in pools)
        {
            var parent = new GameObject($"{entry.prefab.name} Pool").transform;
            parent.parent = transform;
            Queue<GameObject> objectQueue = new Queue<GameObject>();

            for (int i = 0; i < entry.size; i++)
            {
                GameObject obj = Instantiate(entry.prefab, parent);
                obj.SetActive(false);
                objectQueue.Enqueue(obj);
            }

            poolDict[entry.prefab.name] = objectQueue;
        }
    }

    public GameObject Get(string key)
    {
        if (!poolDict.TryGetValue(key, out var queue))
        {
            Debug.LogWarning($"No pool found for key: {key}");
            return null;
        }

        if (queue.Count == 0)
        {
            Debug.LogWarning($"Pool for key {key} is empty. Consider increasing pool size.");
            return null;
        }

        var obj = queue.Dequeue();
        // obj.SetActive(true);
        return obj;
    }

    public void Return(string key, GameObject obj)
    {
        if (!poolDict.ContainsKey(key))
        {
            Debug.LogWarning($"No pool to return to for key: {key}");
            Destroy(obj); // last resort
            return;
        }

        obj.SetActive(false);
        poolDict[key].Enqueue(obj);
    }
}