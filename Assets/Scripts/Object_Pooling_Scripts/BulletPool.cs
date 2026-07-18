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
        if (!pools.TryGetValue(key, out Queue<GameObject> pool))
        {
            Debug.LogWarning($"[BulletPool] No pool found for key: {key}");
            return null;
        }

        GameObject obj = pool.Count > 0 ? pool.Dequeue() : CreateNew(prefabLookup[key]);

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null) poolable.OnSpawn();
        return obj;
    }

    public void EnsurePoolSize(string key, GameObject prefab, int desiredSize)
    {
        if (!pools.TryGetValue(key, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools[key] = pool;
            prefabLookup[key] = prefab;
        }
        int current = pool.Count;
        if (current < desiredSize)
        {
            int toAdd = desiredSize - current;
            for (int i = 0; i < toAdd; i++)
                pool.Enqueue(CreateNew(prefabLookup[key]));
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