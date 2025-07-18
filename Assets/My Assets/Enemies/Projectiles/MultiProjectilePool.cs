using System.Collections.Generic;
using System.Linq;
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

    private Dictionary<string, List<GameObject>> poolDict = new();

    private void Awake()
    {
        foreach (var entry in pools)
        {
            var parent = new GameObject($"{entry.prefab.name} Pool").transform;
            parent.parent = transform;
            parent.localPosition = Vector3.zero;
            List<GameObject> projectilePool = new List<GameObject>();

            for (int i = 0; i < entry.size; i++)
            {
                GameObject obj = Instantiate(entry.prefab, parent);
                obj.SetActive(false);
                projectilePool.Add(obj);
            }

            poolDict[entry.prefab.name] = projectilePool;
        }
    }

    public GameObject Get(string key)
    {
        if (!poolDict.TryGetValue(key, out List<GameObject> projectilePool))
        {
            Debug.LogWarning($"[MultiProjectilePool] No pool found for key: {key}");
            return null;
        }

        if (projectilePool.Count == 0)
        {
            Debug.LogWarning($"[MultiProjectilePool] Pool for key {key} is empty. Consider increasing pool size.");
            return null;
        }

        var inactiveProjectile = projectilePool.Find(o => !o.activeInHierarchy);
        if (!inactiveProjectile)
        {
            Debug.LogError($"[MultiProjectilePool] No inactive projectiles available for {key}");
            return null;
        }

        projectilePool.Remove(inactiveProjectile);
        return inactiveProjectile;
    }

    public void Return(string key, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.localPosition = Vector3.zero;

        if (!poolDict.ContainsKey(key))
        {
            Debug.LogWarning($"[MultiProjectilePool] No pool to return to for key: {key}");
            Destroy(obj); // last resort
            return;
        }

        poolDict[key].Add(obj);
    }
}