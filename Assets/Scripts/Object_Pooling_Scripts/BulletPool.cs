using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [System.Serializable]
    public class PoolEntry
    {
        public string key;
        public GameObject prefab;
        public int initialSize = 10;
    }

    [Header("Pool Entries")]
    public List<PoolEntry> poolEntries = new List<PoolEntry>();

    Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (PoolEntry entry in poolEntries)
        {
            Queue<GameObject> pool = new Queue<GameObject>();
            prefabLookup[entry.key] = entry.prefab;

            for (int i = 0; i < entry.initialSize; i++)
            {
                GameObject obj = CreateNew(entry.prefab);
                pool.Enqueue(obj);
            }

            pools[entry.key] = pool;
        }
    }

    GameObject CreateNew(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj;
    }

    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogWarning($"[BulletPool] No pool found for key: {key}");
            return null;
        }

        Queue<GameObject> pool = pools[key];

        GameObject obj;
        if (pool.Count > 0)
            obj = pool.Dequeue();
        else
        {
            // Pool exhausted, create a new one
            obj = CreateNew(prefabLookup[key]);
            Debug.Log($"[BulletPool] Pool '{key}' exhausted, creating new instance.");
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null) poolable.OnSpawn();

        return obj;
    }

    public void EnsurePoolSize(string key, GameObject prefab, int desiredSize)
    {
        if (!pools.ContainsKey(key))
        {
            pools[key] = new Queue<GameObject>();
            prefabLookup[key] = prefab;
        }

        Queue<GameObject> pool = pools[key];
        int current = pool.Count;

        if (current < desiredSize)
        {
            int toAdd = desiredSize - current;
            for (int i = 0; i < toAdd; i++)
                pool.Enqueue(CreateNew(prefabLookup[key]));

            Debug.Log($"[BulletPool] Grew pool '{key}' by {toAdd} to {desiredSize}.");
        }
    }

    public void Return(string key, GameObject obj)
    {
        if (!pools.ContainsKey(key))
        {
            Destroy(obj);
            return;
        }

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null) poolable.OnReturnToPool();

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pools[key].Enqueue(obj);
    }
}