using UnityEngine;

public class ImpactEffectPool : MonoBehaviour
{
    public static ImpactEffectPool Instance { get; private set; }

    [Header("World Impact")]
    public ParticleSystem worldImpactPrefab;
    public int worldPoolSize = 20;

    [Header("Zombie Impact")]
    public ParticleSystem zombieImpactPrefab;
    public int zombiePoolSize = 30;

    ParticleSystem[] worldPool;
    ParticleSystem[] zombiePool;
    int worldIndex = 0;
    int zombieIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        worldPool = BuildPool(worldImpactPrefab, worldPoolSize);
        zombiePool = BuildPool(zombieImpactPrefab, zombiePoolSize);
    }

    ParticleSystem[] BuildPool(ParticleSystem prefab, int size)
    {
        if (prefab == null) return null;
        ParticleSystem[] pool = new ParticleSystem[size];
        for (int i = 0; i < size; i++)
        {
            ParticleSystem ps = Instantiate(prefab, transform);
            ps.gameObject.SetActive(false);
            pool[i] = ps;
        }
        return pool;
    }

    public void SpawnWorld(Vector3 point, Vector3 normal)
    {
        Spawn(worldPool, ref worldIndex, point, normal);
    }

    public void SpawnZombie(Vector3 point, Vector3 normal)
    {
        Spawn(zombiePool, ref zombieIndex, point, normal);
    }

    void Spawn(ParticleSystem[] pool, ref int index, Vector3 point, Vector3 normal)
    {
        if (pool == null || pool.Length == 0) return;

        ParticleSystem ps = pool[index];
        index = (index + 1) % pool.Length;

        // Move and orient
        ps.transform.position = point;
        ps.transform.rotation = Quaternion.LookRotation(normal);

        // Reset and play
        ps.gameObject.SetActive(true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }

    void Update()
    {
        // Disable particle systems that have finished playing.
        // Cheap — just an IsAlive check per pool entry.
        DeactivateFinished(worldPool);
        DeactivateFinished(zombiePool);
    }

    void DeactivateFinished(ParticleSystem[] pool)
    {
        if (pool == null) return;
        for (int i = 0; i < pool.Length; i++)
        {
            ParticleSystem ps = pool[i];
            if (ps == null) continue;
            if (ps.gameObject.activeSelf && !ps.IsAlive(true))
                ps.gameObject.SetActive(false);
        }
    }
}