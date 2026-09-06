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
    Transform[] worldTransforms;
    Transform[] zombieTransforms;
    float[] worldDeactivateAt;
    float[] zombieDeactivateAt;
    float worldEffectDuration;
    float zombieEffectDuration;

    int worldIndex = 0;
    int zombieIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        worldEffectDuration = GetEffectDuration(worldImpactPrefab);
        zombieEffectDuration = GetEffectDuration(zombieImpactPrefab);

        BuildPool(worldImpactPrefab, worldPoolSize, out worldPool, out worldTransforms, out worldDeactivateAt);
        BuildPool(zombieImpactPrefab, zombiePoolSize, out zombiePool, out zombieTransforms, out zombieDeactivateAt);
    }

    float GetEffectDuration(ParticleSystem prefab)
    {
        if (prefab == null) return 1f;

        ParticleSystem.MainModule main = prefab.main;
        float lifetime = main.startLifetime.mode == ParticleSystemCurveMode.Constant
            ? main.startLifetime.constant
            : main.startLifetime.constantMax;

        return main.duration + lifetime;
    }

    void BuildPool(ParticleSystem prefab, int size, out ParticleSystem[] pool, out Transform[] transforms, out float[] deactivateAt)
    {
        pool = null;
        transforms = null;
        deactivateAt = null;
        if (prefab == null) return;

        pool = new ParticleSystem[size];
        transforms = new Transform[size];
        deactivateAt = new float[size];

        for (int i = 0; i < size; i++)
        {
            ParticleSystem ps = Instantiate(prefab, transform);
            ps.gameObject.SetActive(false);
            pool[i] = ps;
            transforms[i] = ps.transform;
            deactivateAt[i] = -1f;
        }
    }

    public void SpawnWorld(Vector3 point, Vector3 normal)
    {
        Spawn(worldPool, worldTransforms, worldDeactivateAt, ref worldIndex, worldEffectDuration, point, normal);
    }

    public void SpawnZombie(Vector3 point, Vector3 normal)
    {
        Spawn(zombiePool, zombieTransforms, zombieDeactivateAt, ref zombieIndex, zombieEffectDuration, point, normal);
    }

    void Spawn(ParticleSystem[] pool, Transform[] transforms, float[] deactivateAt, ref int index, float duration, Vector3 point, Vector3 normal)
    {
        if (pool == null || pool.Length == 0) return;

        ParticleSystem ps = pool[index];
        Transform t = transforms[index];

        t.SetPositionAndRotation(point, Quaternion.LookRotation(normal));

        // Avoid the expensive Stop(withChildren, StopEmittingAndClear) + activation churn per
        // hit. The ring buffer has cycled by the time we wrap; a few stray particles is invisible.
        if (!ps.gameObject.activeSelf)
            ps.gameObject.SetActive(true);
        ps.Play();

        deactivateAt[index] = Time.time + duration;
        index = (index + 1) % pool.Length;
    }

    void Update()
    {
        DeactivateFinished(worldPool, worldDeactivateAt);
        DeactivateFinished(zombiePool, zombieDeactivateAt);
    }

    void DeactivateFinished(ParticleSystem[] pool, float[] deactivateAt)
    {
        if (pool == null) return;

        for (int i = 0; i < pool.Length; i++)
        {
            if (deactivateAt[i] < 0f) continue;
            if (Time.time < deactivateAt[i]) continue;

            pool[i].gameObject.SetActive(false);
            deactivateAt[i] = -1f;
        }
    }
}