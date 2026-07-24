using Unity.Entities;
using UnityEngine;

public class ZombieSpawnAuthoring : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject zPrefab;

    [Header("Population")]
    public int maxAlive = 1000;

    [Header("Spawn Rate (per second)")]
    public float baseSpawnRate = 3f;
    public float ratePerMinute = 2f;
    public float maxSpawnRate = 40f;

    [Header("Escalation (per minute of play)")]
    public float healthPerMinute = 25f;
    public float speedPerMinute = 0.15f;
    public float maxSpeedMultiplier = 2.5f;

    [Header("Placement")]
    public float spawnRadiusMin = 25f;
    public float spawnRadiusMax = 45f;
    public int spawnAttemptsPerZombie = 4;

    [Header("Player Clustering")]
    public float clusterRadius = 30f;
    public float clusterRateMultiplier = 1.8f;

    [Header("Death")]
    public float deathDuration = 0.35f;

    class Baker : Baker<ZombieSpawnAuthoring>
    {
        public override void Bake(ZombieSpawnAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            Entity prefabEntity = GetEntity(authoring.zPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new ZombieSpawnConfig { Prefab = prefabEntity });

            AddComponent(entity, new ZombieSpawnState
            {
                ElapsedTime = 0f,
                SpawnAccumulator = 0f
            });

            AddComponent(entity, new ZombieSpawnTuning
            {
                MaxAlive = authoring.maxAlive,
                BaseSpawnRate = authoring.baseSpawnRate,
                RatePerMinute = authoring.ratePerMinute,
                MaxSpawnRate = authoring.maxSpawnRate,
                SpawnRadiusMin = authoring.spawnRadiusMin,
                SpawnRadiusMax = authoring.spawnRadiusMax,
                ClusterRadius = authoring.clusterRadius,
                ClusterRateMultiplier = authoring.clusterRateMultiplier,
                SpawnAttemptsPerZombie = authoring.spawnAttemptsPerZombie,
                HealthPerMinute = authoring.healthPerMinute,
                SpeedPerMinute = authoring.speedPerMinute,
                MaxSpeedMultiplier = authoring.maxSpeedMultiplier,
                DeathDuration = authoring.deathDuration
            });
        }
    }
}