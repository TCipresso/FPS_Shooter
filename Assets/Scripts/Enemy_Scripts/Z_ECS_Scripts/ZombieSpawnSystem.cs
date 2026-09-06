using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// Continuous radius spawner. ECS port of RadiusEnemySpawner + EnemyDifficultyHandler
// (population cap from EnemyPopulationManager; recycling lives in ZombieRecycleSystem).
[UpdateAfter(typeof(ZombieBootstrapSystem))]
public partial struct ZombieSpawnSystem : ISystem
{
    Random random;

    public void OnCreate(ref SystemState state)
    {
        random = new Random(0x5EED1234u);
        state.RequireForUpdate<ZombieSingletonTag>();
        state.RequireForUpdate<ZombieConstantSpawnConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity singletonEntity = SystemAPI.GetSingletonEntity<ZombieSingletonTag>();
        Entity configEntity = SystemAPI.GetSingletonEntity<ZombieConstantSpawnConfig>();

        ZombieConstantSpawnConfig config = SystemAPI.GetComponent<ZombieConstantSpawnConfig>(configEntity);
        ZombieSpawnState spawnState = SystemAPI.GetComponent<ZombieSpawnState>(configEntity);

        DynamicBuffer<ZombieSpawnPrefabElement> prefabBuffer = SystemAPI.GetBuffer<ZombieSpawnPrefabElement>(configEntity);
        if (prefabBuffer.Length == 0)
            return;

        // Copy out before any structural change (Instantiate/SetEnabled) invalidates the buffer.
        NativeArray<ZombieSpawnPrefabElement> prefabSet = prefabBuffer.ToNativeArray(Allocator.Temp);

        DynamicBuffer<PlayerTargetElement> players = SystemAPI.GetBuffer<PlayerTargetElement>(singletonEntity);
        float3 anchor = float3.zero;
        bool hasAnchor = false;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].IsRegistered && players[i].IsTargetable)
            {
                anchor = players[i].Position;
                hasAnchor = true;
                break;
            }
        }

        int aliveCount = SystemAPI.QueryBuilder().WithAll<ZombieTag>().Build().CalculateEntityCount();
        float deltaTime = SystemAPI.Time.DeltaTime;

        float steps = config.DifficultyInterval > 0.01f ? spawnState.Elapsed / config.DifficultyInterval : 0f;
        int wholeSteps = (int)math.floor(steps);
        float spawnRate = math.min(config.BaseSpawnRate + wholeSteps * config.SpawnRateGrowthAmount, config.MaxSpawnRate);
        spawnRate = math.max(spawnRate, 0.01f);

        int spawnedThisTick = 0;

        if (hasAnchor)
        {
            spawnState.Elapsed += deltaTime;

            int groundMask = 0;
            if (SystemAPI.TryGetSingleton<ZombieWallConfig>(out ZombieWallConfig wallConfig))
                groundMask = wallConfig.GroundLayerMask;

            float healthMult = math.min(math.pow(config.HealthMultiplierPerInterval, steps), config.MaxStatMultiplier);
            float damageMult = math.min(math.pow(config.DamageMultiplierPerInterval, steps), config.MaxStatMultiplier);
            float speedMult = math.min(math.pow(config.SpeedMultiplierPerInterval, steps), config.MaxStatMultiplier);

            EntityManager em = state.EntityManager;
            NativeList<Entity> pool = SystemAPI.GetComponent<ZombiePoolSingleton>(singletonEntity).Inactive;

            ZombieSpawnDebug dbg = default;
            if (SystemAPI.HasComponent<ZombieSpawnDebug>(configEntity))
                dbg = SystemAPI.GetComponent<ZombieSpawnDebug>(configEntity);
            bool stress = dbg.Enabled;

            if (stress)
            {
                // Ignore rate + MaxAlive; fill up to TargetCount, SpawnPerFrame at a time.
                int perFrame = math.max(1, dbg.SpawnPerFrame);
                int want = math.min(perFrame, dbg.TargetCount - aliveCount);
                for (int s = 0; s < want; s++)
                {
                    if (!TryFindSpawnPoint(anchor, config, groundMask, out float3 spawnPos))
                        continue;

                    int prefabIndex = PickWeighted(prefabSet);
                    SpawnOne(em, pool, prefabIndex, prefabSet[prefabIndex].Prefab, spawnPos, healthMult, damageMult, speedMult);
                    spawnedThisTick++;
                }
            }
            else
            {
                spawnState.SpawnTimer += deltaTime;

                float spawnInterval = 1f / spawnRate;
                if (spawnState.SpawnTimer >= spawnInterval)
                {
                    spawnState.SpawnTimer -= spawnInterval;

                    int spawnsPerTick = math.max(1, config.SpawnsPerTick);
                    for (int s = 0; s < spawnsPerTick; s++)
                    {
                        if (aliveCount + spawnedThisTick >= config.MaxAlive)
                            break;

                        if (!TryFindSpawnPoint(anchor, config, groundMask, out float3 spawnPos))
                            continue;

                        int prefabIndex = PickWeighted(prefabSet);
                        SpawnOne(em, pool, prefabIndex, prefabSet[prefabIndex].Prefab, spawnPos, healthMult, damageMult, speedMult);
                        spawnedThisTick++;
                    }
                }
            }
        }

        prefabSet.Dispose();

        SystemAPI.SetComponent(configEntity, spawnState);

        if (SystemAPI.TryGetSingletonRW<ZombieSpawnStats>(out var stats))
        {
            stats.ValueRW.Alive = aliveCount + spawnedThisTick;
            stats.ValueRW.TotalSpawned += spawnedThisTick;
            stats.ValueRW.CurrentSpawnRate = spawnRate;
            stats.ValueRW.StatMultiplierSteps = steps;
        }
    }

    int PickWeighted(NativeArray<ZombieSpawnPrefabElement> prefabSet)
    {
        float total = 0f;
        for (int i = 0; i < prefabSet.Length; i++)
            total += math.max(0f, prefabSet[i].Weight);

        if (total <= 0f)
            return random.NextInt(0, prefabSet.Length);

        float roll = random.NextFloat(0f, total);
        float cumulative = 0f;
        for (int i = 0; i < prefabSet.Length; i++)
        {
            cumulative += math.max(0f, prefabSet[i].Weight);
            if (roll <= cumulative)
                return i;
        }
        return prefabSet.Length - 1;
    }

    void SpawnOne(EntityManager em, NativeList<Entity> pool, int prefabIndex, Entity prefab, float3 spawnPos,
        float healthMult, float damageMult, float speedMult)
    {
        Entity entity = ZombiePool.Acquire(em, pool, prefabIndex, prefab);

        float groundOffset = 0f;
        if (em.HasComponent<ZombieGroundOffset>(entity))
            groundOffset = em.GetComponentData<ZombieGroundOffset>(entity).Value;

        LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
        em.SetComponentData(entity, transform.WithPosition(new float3(spawnPos.x, spawnPos.y + groundOffset, spawnPos.z)));

        int baseMaxHealth = 0;
        float baseSpeed = 0f;
        int baseContactDamage = 0;
        if (em.HasComponent<ZombieBaseStats>(entity))
        {
            ZombieBaseStats baseStats = em.GetComponentData<ZombieBaseStats>(entity);
            baseMaxHealth = baseStats.BaseMaxHealth;
            baseSpeed = baseStats.BaseMoveSpeed;
            baseContactDamage = baseStats.BaseContactDamage;
        }

        if (em.HasComponent<ZombieHealth>(entity) && baseMaxHealth > 0)
        {
            int scaledMax = math.max(1, (int)math.round(baseMaxHealth * healthMult));
            em.SetComponentData(entity, new ZombieHealth { Current = scaledMax, Max = scaledMax });
        }

        if (em.HasComponent<ZombieMoveSpeed>(entity) && baseSpeed > 0f)
            em.SetComponentData(entity, new ZombieMoveSpeed { Value = baseSpeed * speedMult });

        if (em.HasComponent<ZombieContactDamage>(entity) && baseContactDamage > 0)
            em.SetComponentData(entity, new ZombieContactDamage { Value = math.max(1, (int)math.round(baseContactDamage * damageMult)) });

        if (em.HasComponent<ZombieTarget>(entity))
        {
            em.SetComponentData(entity, new ZombieTarget
            {
                Index = -1,
                Position = float3.zero,
                HasTarget = false,
                RecheckTimer = random.NextFloat(0f, 0.35f)
            });
        }
    }

    bool TryFindSpawnPoint(float3 anchor, ZombieConstantSpawnConfig config, int groundMask, out float3 result)
    {
        result = float3.zero;

        int attempts = math.max(1, config.MaxAttemptsPerSpawn);
        float rayHeight = math.max(1f, config.RaycastHeight);

        for (int i = 0; i < attempts; i++)
        {
            float angle = random.NextFloat(0f, math.PI * 2f);
            float radius = random.NextFloat(config.MinRadius, config.MaxRadius);
            float3 candidate = anchor + new float3(math.cos(angle) * radius, 0f, math.sin(angle) * radius);

            Vector3 origin = new Vector3(candidate.x, anchor.y + rayHeight, candidate.z);

            bool hit = groundMask != 0
                ? Physics.Raycast(origin, Vector3.down, out RaycastHit info, rayHeight * 2f, groundMask)
                : Physics.Raycast(origin, Vector3.down, out info, rayHeight * 2f);

            if (!hit)
                continue;

            if (Vector3.Angle(info.normal, Vector3.up) > config.MaxSlopeDegrees)
                continue;

            result = new float3(candidate.x, info.point.y, candidate.z);
            return true;
        }

        return false;
    }
}
