using UnityEngine;
using System.Collections.Generic;

public class PickupPool : MonoBehaviour
{
    public static PickupPool Instance { get; private set; }

    [System.Serializable]
    public class PoolEntry
    {
        public GameObject prefab;
        public int initialSize = 20;
    }

    [Header("Pool Entries")]
    public List<PoolEntry> poolEntries = new List<PoolEntry>();

    Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();
    List<GameObject> activeInstances = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (PoolEntry entry in poolEntries)
        {
            if (entry.prefab == null) continue;
            Queue<GameObject> pool = GetOrCreateQueue(entry.prefab);
            for (int i = 0; i < entry.initialSize; i++)
                pool.Enqueue(CreateNew(entry.prefab));
        }
    }

    Queue<GameObject> GetOrCreateQueue(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools[prefab] = pool;
        }
        return pool;
    }

    GameObject CreateNew(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        instanceToPrefab[obj] = prefab;
        return obj;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        Queue<GameObject> pool = GetOrCreateQueue(prefab);
        GameObject obj = pool.Count > 0 ? pool.Dequeue() : CreateNew(prefab);

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        activeInstances.Add(obj);
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;

        if (!instanceToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        activeInstances.Remove(obj);
        pools[prefab].Enqueue(obj);
    }

    public void ReturnAllActive()
    {
        for (int i = activeInstances.Count - 1; i >= 0; i--)
            Return(activeInstances[i]);
    }
}