using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateAfter(typeof(ZombieBootstrapSystem))]
public partial struct ZombieRoundSystem : ISystem
{
    Random random;

    public void OnCreate(ref SystemState state)
    {
        random = new Random(0x5EED1234u);
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieSingletonTag>(out Entity singletonEntity))
            return;

        ZombieSimAuthority authority = SystemAPI.GetComponent<ZombieSimAuthority>(singletonEntity);
        if (!authority.ShouldSimulate)
            return;

        if (!SystemAPI.TryGetSingletonEntity<ZombieSpawnConfig>(out Entity spawnConfigEntity))
            return;

        ZombieSpawnConfig spawnConfig = SystemAPI.GetComponent<ZombieSpawnConfig>(spawnConfigEntity);
        ZombieRoundConfig config = SystemAPI.GetComponent<ZombieRoundConfig>(spawnConfigEntity);

        DynamicBuffer<PlayerTargetElement> players = SystemAPI.GetBuffer<PlayerTargetElement>(singletonEntity);

        NativeList<float3> activePlayers = new NativeList<float3>(4, Allocator.Temp);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].IsRegistered && players[i].IsTargetable)
                activePlayers.Add(players[i].Position);
        }

        if (activePlayers.Length == 0)
        {
            activePlayers.Dispose();
            return;
        }

        ZombieRoundState round = SystemAPI.GetComponent<ZombieRoundState>(spawnConfigEntity);
        float deltaTime = SystemAPI.Time.DeltaTime;

        if (round.Round == 0)
            StartRound(ref round, 1, config);

        if (round.InIntermission)
        {
            round.IntermissionTimer -= deltaTime;
            if (round.IntermissionTimer <= 0f)
                StartRound(ref round, round.Round + 1, config);

            SystemAPI.SetComponent(spawnConfigEntity, round);
            activePlayers.Dispose();
            return;
        }

        int aliveCount = SystemAPI.QueryBuilder().WithAll<ZombieTag>().Build().CalculateEntityCount();

        if (round.RemainingToSpawn <= 0 && aliveCount == 0)
        {
            round.InIntermission = true;
            round.IntermissionTimer = config.IntermissionDuration;
            SystemAPI.SetComponent(spawnConfigEntity, round);
            activePlayers.Dispose();
            return;
        }

        float clusterFactor = CalculateClusterFactor(activePlayers, config);
        float rate = SpawnRateForRound(round.Round, config) * clusterFactor * activePlayers.Length;

        round.SpawnAccumulator += rate * deltaTime;

        int wanted = (int)round.SpawnAccumulator;
        if (wanted > 0)
        {
            round.SpawnAccumulator -= wanted;

            int headroom = math.max(0, config.MaxAlive - aliveCount);
            int toSpawn = math.min(wanted, math.min(round.RemainingToSpawn, headroom));

            if (toSpawn > 0)
            {
                float healthBonus = config.HealthPerRound * (round.Round - 1);
                float speedMult = math.min(1f + config.SpeedPerRound * (round.Round - 1), config.MaxSpeedMultiplier);
                int spawnedCount = SpawnBatch(ref state, singletonEntity, spawnConfig, config, activePlayers, toSpawn, healthBonus, speedMult);
                round.RemainingToSpawn -= spawnedCount;
            }
        }

        SystemAPI.SetComponent(spawnConfigEntity, round);
        activePlayers.Dispose();
    }

    void StartRound(ref ZombieRoundState round, int roundNumber, ZombieRoundConfig config)
    {
        int bank = (int)math.round(config.BaseRoundBank * math.pow(config.BankGrowth, roundNumber - 1));
        bank = math.max(1, bank);

        round.Round = roundNumber;
        round.TotalThisRound = bank;
        round.RemainingToSpawn = bank;
        round.KilledThisRound = 0;
        round.SpawnAccumulator = 0f;
        round.InIntermission = false;
        round.IntermissionTimer = 0f;
    }

    float SpawnRateForRound(int roundNumber, ZombieRoundConfig config)
    {
        float rate = config.BaseSpawnRate * math.pow(config.SpawnRateGrowth, roundNumber - 1);
        return math.min(rate, config.MaxSpawnRate);
    }

    float CalculateClusterFactor(NativeList<float3> activePlayers, ZombieRoundConfig config)
    {
        if (activePlayers.Length <= 1)
            return 1f;

        float3 centroid = float3.zero;
        for (int i = 0; i < activePlayers.Length; i++)
            centroid += activePlayers[i];
        centroid /= activePlayers.Length;

        float spread = 0f;
        for (int i = 0; i < activePlayers.Length; i++)
        {
            float3 delta = activePlayers[i] - centroid;
            delta.y = 0f;
            spread = math.max(spread, math.length(delta));
        }

        float t = math.saturate(spread / math.max(0.01f, config.ClusterRadius));
        return math.lerp(config.ClusterRateMultiplier, 1f, t);
    }

    int SpawnBatch(ref SystemState state, Entity singletonEntity, ZombieSpawnConfig spawnConfig,
        ZombieRoundConfig config, NativeList<float3> activePlayers, int count, float healthBonus, float speedMult)
    {
        EntityManager em = state.EntityManager;
        NativeList<Entity> pool = SystemAPI.GetComponent<ZombiePoolSingleton>(singletonEntity).Inactive;

        int groundMask = 0;
        if (SystemAPI.TryGetSingleton<ZombieWallConfig>(out ZombieWallConfig wallConfig))
            groundMask = wallConfig.GroundLayerMask;

        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            float3 anchor = activePlayers[random.NextInt(0, activePlayers.Length)];

            if (!TryFindSpawnPoint(anchor, config, groundMask, out float3 spawnPos))
                continue;

            Entity entity = ZombiePool.Acquire(em, pool, spawnConfig.Prefab);

            float groundOffset = 0f;
            if (em.HasComponent<ZombieGroundOffset>(entity))
                groundOffset = em.GetComponentData<ZombieGroundOffset>(entity).Value;

            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            em.SetComponentData(entity, transform.WithPosition(new float3(spawnPos.x, spawnPos.y + groundOffset, spawnPos.z)));

            float baseSpeed = 0f;
            int baseMaxHealth = 0;
            if (em.HasComponent<ZombieBaseStats>(entity))
            {
                ZombieBaseStats baseStats = em.GetComponentData<ZombieBaseStats>(entity);
                baseSpeed = baseStats.BaseMoveSpeed;
                baseMaxHealth = baseStats.BaseMaxHealth;
            }

            if (em.HasComponent<ZombieHealth>(entity) && baseMaxHealth > 0)
            {
                int scaledMax = baseMaxHealth + (int)healthBonus;
                em.SetComponentData(entity, new ZombieHealth { Current = scaledMax, Max = scaledMax });
            }

            if (em.HasComponent<ZombieMoveSpeed>(entity) && baseSpeed > 0f)
            {
                em.SetComponentData(entity, new ZombieMoveSpeed { Value = baseSpeed * speedMult });
            }

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

            spawned++;
        }

        return spawned;
    }

    bool TryFindSpawnPoint(float3 anchor, ZombieRoundConfig config, int groundMask, out float3 result)
    {
        result = float3.zero;

        int attempts = math.max(1, config.SpawnAttemptsPerZombie);

        for (int i = 0; i < attempts; i++)
        {
            float angle = random.NextFloat(0f, math.PI * 2f);
            float radius = random.NextFloat(config.SpawnRadiusMin, config.SpawnRadiusMax);

            float3 candidate = anchor + new float3(math.cos(angle) * radius, 0f, math.sin(angle) * radius);

            Vector3 origin = new Vector3(candidate.x, anchor.y + 30f, candidate.z);

            bool hit = groundMask != 0
                ? Physics.Raycast(origin, Vector3.down, out RaycastHit info, 120f, groundMask)
                : Physics.Raycast(origin, Vector3.down, out info, 120f);

            if (hit)
            {
                result = new float3(candidate.x, info.point.y, candidate.z);
                return true;
            }
        }

        return false;
    }
}