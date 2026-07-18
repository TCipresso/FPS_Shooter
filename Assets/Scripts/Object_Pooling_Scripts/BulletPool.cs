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

    class PooledItem
    {
        public GameObject GameObject;
        public Transform Transform;
        public IPoolable Poolable;
    }

    [Header("Pool Entries")]
    public List<PoolEntry> poolEntries = new List<PoolEntry>();

    Dictionary<string, Queue<PooledItem>> pools = new Dictionary<string, Queue<PooledItem>>();
    Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();
    Dictionary<GameObject, PooledItem> itemLookup = new Dictionary<GameObject, PooledItem>();

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
            Queue<PooledItem> pool = new Queue<PooledItem>();
            prefabLookup[entry.key] = entry.prefab;
            for (int i = 0; i < entry.initialSize; i++)
                pool.Enqueue(CreateNew(entry.prefab));
            pools[entry.key] = pool;
        }
    }

    PooledItem CreateNew(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);

        PooledItem item = new PooledItem
        {
            GameObject = obj,
            Transform = obj.transform,
            Poolable = obj.GetComponent<IPoolable>()
        };

        itemLookup[obj] = item;
        return item;
    }

    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        if (!pools.TryGetValue(key, out Queue<PooledItem> pool))
        {
            Debug.LogWarning($"[BulletPool] No pool found for key: {key}");
            return null;
        }

        PooledItem item = pool.Count > 0 ? pool.Dequeue() : CreateNew(prefabLookup[key]);
        item.Transform.SetPositionAndRotation(position, rotation);
        item.GameObject.SetActive(true);
        item.Poolable?.OnSpawn();
        return item.GameObject;
    }

    public void EnsurePoolSize(string key, GameObject prefab, int desiredSize)
    {
        if (!pools.TryGetValue(key, out Queue<PooledItem> pool))
        {
            pool = new Queue<PooledItem>();
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
        if (!pools.TryGetValue(key, out Queue<PooledItem> pool))
        {
            Destroy(obj);
            return;
        }

        if (!itemLookup.TryGetValue(obj, out PooledItem item))
        {
            Destroy(obj);
            return;
        }

        item.Poolable?.OnReturnToPool();
        item.GameObject.SetActive(false);
        pool.Enqueue(item);
    }
}