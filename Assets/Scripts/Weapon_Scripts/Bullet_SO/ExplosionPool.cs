using System.Collections.Generic;
using UnityEngine;

public class ExplosionPool : MonoBehaviour
{
    public static ExplosionPool Instance;

    class Pool
    {
        public GameObject prefab;
        public Queue<GameObject> free = new Queue<GameObject>();
    }

    struct Active
    {
        public GameObject go;
        public GameObject prefab;
        public float returnTime;
    }

    Dictionary<GameObject, Pool> pools = new Dictionary<GameObject, Pool>();
    List<Active> active = new List<Active>(32);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

    public void Spawn(GameObject prefab, Vector3 position, float duration)
    {
        if (prefab == null) return;

        Pool pool = GetOrCreatePool(prefab);
        GameObject go = pool.free.Count > 0 ? pool.free.Dequeue() : Instantiate(prefab);

        go.transform.position = position;
        go.SetActive(true);

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        active.Add(new Active { go = go, prefab = prefab, returnTime = Time.time + duration });
    }

    void Update()
    {
        float now = Time.time;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (now < active[i].returnTime) continue;

            Active a = active[i];
            active.RemoveAt(i);

            if (a.go == null) continue;

            a.go.SetActive(false);
            if (pools.TryGetValue(a.prefab, out Pool pool))
                pool.free.Enqueue(a.go);
        }
    }
}
