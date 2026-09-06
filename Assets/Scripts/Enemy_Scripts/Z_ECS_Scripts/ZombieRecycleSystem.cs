using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// ECS port of EnemyPopulationManager's recycling: zombies that wander too far from the
// player are teleported back into a ring around the player instead of being destroyed.
// Round-robins a fixed budget of entities per frame.
[UpdateAfter(typeof(ZombieSpawnSystem))]
public partial struct ZombieRecycleSystem : ISystem
{
    Random random;

    public void OnCreate(ref SystemState state)
    {
        random = new Random(0xC0FFEE11u);
        state.RequireForUpdate<ZombieSingletonTag>();
        state.RequireForUpdate<ZombieConstantSpawnConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity singletonEntity = SystemAPI.GetSingletonEntity<ZombieSingletonTag>();
        Entity configEntity = SystemAPI.GetSingletonEntity<ZombieConstantSpawnConfig>();

        ZombieConstantSpawnConfig config = SystemAPI.GetComponent<ZombieConstantSpawnConfig>(configEntity);
        ZombieSpawnState spawnState = SystemAPI.GetComponent<ZombieSpawnState>(configEntity);

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

        if (!hasAnchor || config.RecycleDistance <= 0f)
            return;

        state.Dependency.Complete();

        EntityManager em = state.EntityManager;
        NativeArray<Entity> zombies = SystemAPI.QueryBuilder().WithAll<ZombieTag>().Build().ToEntityArray(Allocator.Temp);
        if (zombies.Length == 0)
        {
            zombies.Dispose();
            return;
        }

        int groundMask = 0;
        if (SystemAPI.TryGetSingleton<ZombieWallConfig>(out ZombieWallConfig wallConfig))
            groundMask = wallConfig.GroundLayerMask;

        float recycleSqr = config.RecycleDistance * config.RecycleDistance;
        float rayHeight = math.max(1f, config.RaycastHeight);
        int budget = math.min(math.max(1, config.RecycleChecksPerFrame), zombies.Length);

        int cursor = spawnState.RecycleCursor;
        for (int n = 0; n < budget; n++)
        {
            int idx = (cursor + n) % zombies.Length;
            Entity zombie = zombies[idx];
            if (!em.Exists(zombie))
                continue;

            LocalTransform transform = em.GetComponentData<LocalTransform>(zombie);
            float3 flatDelta = transform.Position - anchor;
            flatDelta.y = 0f;
            if (math.lengthsq(flatDelta) <= recycleSqr)
                continue;

            if (TryFindRecyclePoint(anchor, config, groundMask, rayHeight, out float3 dest))
            {
                float groundOffset = em.HasComponent<ZombieGroundOffset>(zombie)
                    ? em.GetComponentData<ZombieGroundOffset>(zombie).Value
                    : 0f;

                em.SetComponentData(zombie, transform.WithPosition(new float3(dest.x, dest.y + groundOffset, dest.z)));

                if (em.HasComponent<ZombieVerticalVelocity>(zombie))
                    em.SetComponentData(zombie, new ZombieVerticalVelocity { Value = 0f });
                if (em.HasComponent<ZombieClimbState>(zombie))
                    em.SetComponentData(zombie, new ZombieClimbState { WasBlocked = false, WasWallBlocked = false });
            }
        }

        spawnState.RecycleCursor = (cursor + budget) % zombies.Length;
        SystemAPI.SetComponent(configEntity, spawnState);

        zombies.Dispose();
    }

    bool TryFindRecyclePoint(float3 anchor, ZombieConstantSpawnConfig config, int groundMask, float rayHeight, out float3 result)
    {
        result = float3.zero;

        for (int attempt = 0; attempt < 5; attempt++)
        {
            float angle = random.NextFloat(0f, math.PI * 2f);
            float dist = random.NextFloat(config.RecycleRadiusMin, config.RecycleRadiusMax);
            float3 candidate = anchor + new float3(math.cos(angle) * dist, 0f, math.sin(angle) * dist);

            Vector3 origin = new Vector3(candidate.x, anchor.y + rayHeight, candidate.z);
            bool hit = groundMask != 0
                ? Physics.Raycast(origin, Vector3.down, out RaycastHit info, rayHeight * 2f, groundMask)
                : Physics.Raycast(origin, Vector3.down, out info, rayHeight * 2f);

            if (!hit)
                continue;

            result = new float3(candidate.x, info.point.y, candidate.z);
            return true;
        }

        return false;
    }
}
