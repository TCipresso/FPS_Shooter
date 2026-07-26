using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    class Pool
    {
        public GameObject prefab;
        public Queue<ProjectileBase> free = new Queue<ProjectileBase>();
    }

    Dictionary<GameObject, Pool> pools = new Dictionary<GameObject, Pool>();
    List<ProjectileBase> active = new List<ProjectileBase>(64);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void EnsurePoolSize(GameObject prefab, int size)
    {
        if (prefab == null) return;

        Pool pool = GetOrCreatePool(prefab);
        while (pool.free.Count < size)
            pool.free.Enqueue(CreateInstance(pool));
    }

    Pool GetOrCreatePool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Pool pool))
        {
            pool = new Pool { prefab = prefab };
            pools[prefab] = pool;
        }
        return pool;
    }

    ProjectileBase CreateInstance(Pool pool)
    {
        GameObject go = Instantiate(pool.prefab);
        go.SetActive(false);

        ProjectileBase p = go.GetComponent<ProjectileBase>();
        if (p == null) p = go.AddComponent<ProjectileBase>();

        p.pool = pool.prefab;
        return p;
    }

    public ProjectileBase Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;

        Pool pool = GetOrCreatePool(prefab);
        ProjectileBase p = pool.free.Count > 0 ? pool.free.Dequeue() : CreateInstance(pool);

        p.transform.SetPositionAndRotation(pos, rot);
        p.gameObject.SetActive(true);
        active.Add(p);
        return p;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            ProjectileBase p = active[i];
            if (p == null)
            {
                active.RemoveAt(i);
                continue;
            }

            if (p.Tick(dt))
            {
                active.RemoveAt(i);
                p.gameObject.SetActive(false);

                if (p.pool != null && pools.TryGetValue(p.pool, out Pool pool))
                    pool.free.Enqueue(p);
            }
        }
    }
}
