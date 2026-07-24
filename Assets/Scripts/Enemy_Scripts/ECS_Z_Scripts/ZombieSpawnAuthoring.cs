using Unity.Entities;
using UnityEngine;

public class ZombieSpawnAuthoring : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject zPrefab;

    [Header("Population")]
    public int maxAlive = 1000;

    [Header("Round Bank")]
    public int baseRoundBank = 8;
    public float bankGrowth = 1.15f;

    [Header("Spawn Rate")]
    public float baseSpawnRate = 1.5f;
    public float spawnRateGrowth = 1.1f;
    public float maxSpawnRate = 20f;

    [Header("Escalation (per round)")]
    public float healthPerRound = 15f;
    public float speedPerRound = 0.05f;
    public float maxSpeedMultiplier = 2.5f;

    [Header("Pacing")]
    public float intermissionDuration = 6f;

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

            AddComponent(entity, new ZombieRoundState
            {
                Round = 0,
                RemainingToSpawn = 0,
                TotalThisRound = 0,
                KilledThisRound = 0,
                SpawnAccumulator = 0f,
                IntermissionTimer = 0f,
                InIntermission = false
            });

            AddComponent(entity, new ZombieRoundConfig
            {
                MaxAlive = authoring.maxAlive,
                BaseRoundBank = authoring.baseRoundBank,
                BankGrowth = authoring.bankGrowth,
                BaseSpawnRate = authoring.baseSpawnRate,
                SpawnRateGrowth = authoring.spawnRateGrowth,
                MaxSpawnRate = authoring.maxSpawnRate,
                IntermissionDuration = authoring.intermissionDuration,
                SpawnRadiusMin = authoring.spawnRadiusMin,
                SpawnRadiusMax = authoring.spawnRadiusMax,
                ClusterRadius = authoring.clusterRadius,
                ClusterRateMultiplier = authoring.clusterRateMultiplier,
                SpawnAttemptsPerZombie = authoring.spawnAttemptsPerZombie,
                HealthPerRound = authoring.healthPerRound,
                SpeedPerRound = authoring.speedPerRound,
                MaxSpeedMultiplier = authoring.maxSpeedMultiplier,
                DeathDuration = authoring.deathDuration
            });
        }
    }
}