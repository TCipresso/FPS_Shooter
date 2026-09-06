using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

// Authoring for the continuous zombie spawner. Field names/defaults mirror the new game's
// GameObject spawners so values port over 1:1:
//   RadiusEnemySpawner, EnemyDifficultyHandler, EnemyPopulationManager.
public class ZombieSpawnAuthoring : MonoBehaviour
{
    [System.Serializable]
    public class WeightedZombie
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }

    [Header("Who To Spawn (RadiusEnemySpawner.enemies)")]
    public List<WeightedZombie> zombies = new List<WeightedZombie>();

    [Header("Population Cap (EnemyPopulationManager.maxActiveEnemies)")]
    public int maxAlive = 300;

    [Header("Difficulty (EnemyDifficultyHandler)")]
    public float difficultyInterval = 15f;
    public float baseSpawnRate = 0.5f;
    public float maxSpawnRate = 3f;
    public float spawnRateGrowthAmount = 0.1f;
    public float healthMultiplierPerInterval = 1.05f;
    public float damageMultiplierPerInterval = 1.04f;
    public float speedMultiplierPerInterval = 1.02f;
    public float maxStatMultiplier = 5f;

    [Header("Ring Placement (RadiusEnemySpawner)")]
    public float minRadius = 15f;
    public float maxRadius = 30f;
    public float raycastHeight = 200f;
    [Range(0f, 90f)] public float maxSlopeDegrees = 40f;
    public int maxAttemptsPerSpawn = 5;
    public int spawnsPerTick = 1;

    [Header("Recycling (EnemyPopulationManager)")]
    [Tooltip("A zombie is only recycled if it's THIS far from the player AND off-screen. Keep it large - recycling is for when the player outruns the horde, not for zombies that wander a bit.")]
    public float recycleDistance = 120f;
    public float recycleRadiusMin = 25f;
    public float recycleRadiusMax = 45f;
    public int recycleChecksPerFrame = 15;

    [Header("Debug / Stress Test")]
    [Tooltip("When on: ignores spawn rate AND maxAlive, just fills the map to debugTargetCount.")]
    public bool debugStressTest = false;
    public int debugTargetCount = 2000;
    [Tooltip("How many to spawn per frame while ramping up to the target (keeps the fill from being one huge hitch).")]
    public int debugSpawnPerFrame = 100;

    class Baker : Baker<ZombieSpawnAuthoring>
    {
        public override void Bake(ZombieSpawnAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            DynamicBuffer<ZombieSpawnPrefabElement> prefabSet = AddBuffer<ZombieSpawnPrefabElement>(entity);
            foreach (WeightedZombie wz in authoring.zombies)
            {
                if (wz == null || wz.prefab == null)
                    continue;

                prefabSet.Add(new ZombieSpawnPrefabElement
                {
                    Prefab = GetEntity(wz.prefab, TransformUsageFlags.Dynamic),
                    Weight = Mathf.Max(0f, wz.weight)
                });
            }

            AddComponent(entity, new ZombieConstantSpawnConfig
            {
                MaxAlive = authoring.maxAlive,
                DifficultyInterval = authoring.difficultyInterval,
                BaseSpawnRate = authoring.baseSpawnRate,
                MaxSpawnRate = authoring.maxSpawnRate,
                SpawnRateGrowthAmount = authoring.spawnRateGrowthAmount,
                HealthMultiplierPerInterval = authoring.healthMultiplierPerInterval,
                DamageMultiplierPerInterval = authoring.damageMultiplierPerInterval,
                SpeedMultiplierPerInterval = authoring.speedMultiplierPerInterval,
                MaxStatMultiplier = authoring.maxStatMultiplier,
                MinRadius = authoring.minRadius,
                MaxRadius = authoring.maxRadius,
                RaycastHeight = authoring.raycastHeight,
                MaxSlopeDegrees = authoring.maxSlopeDegrees,
                MaxAttemptsPerSpawn = authoring.maxAttemptsPerSpawn,
                SpawnsPerTick = authoring.spawnsPerTick,
                RecycleDistance = authoring.recycleDistance,
                RecycleRadiusMin = authoring.recycleRadiusMin,
                RecycleRadiusMax = authoring.recycleRadiusMax,
                RecycleChecksPerFrame = authoring.recycleChecksPerFrame
            });

            AddComponent(entity, new ZombieSpawnDebug
            {
                Enabled = authoring.debugStressTest,
                TargetCount = authoring.debugTargetCount,
                SpawnPerFrame = authoring.debugSpawnPerFrame
            });

            AddComponent(entity, new ZombieSpawnState { Elapsed = 0f, SpawnTimer = 0f, RecycleCursor = 0 });
            AddComponent(entity, new ZombieSpawnStats
            {
                Alive = 0,
                TotalSpawned = 0,
                TotalKilled = 0,
                CurrentSpawnRate = 0f,
                StatMultiplierSteps = 0f
            });
        }
    }
}
