using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// ECS port of EnemyPopulationManager's recycling: zombies that wander too far from the
// player are teleported back into a ring around the player instead of being destroyed.
// Round-robins a fixed budget of entities per frame, and NEVER moves a zombie the camera
// can see - so the player never witnesses a pop.
[UpdateAfter(typeof(ZombieSpawnSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ZombieRecycleSystem : ISystem
{
    Random random;
    static readonly Plane[] FrustumPlanes = new Plane[6];

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

        // Clamp so recycling can never trigger anywhere near the spawn / recycle rings -
        // otherwise a zombie recycled into the ring immediately re-qualifies and ping-pongs.
        float minSafe = math.max(config.MaxRadius, config.RecycleRadiusMax) + 60f;
        float recycleDistance = math.max(config.RecycleDistance, minSafe);
        float recycleSqr = recycleDistance * recycleDistance;
        float rayHeight = math.max(1f, config.RaycastHeight);
        int budget = math.min(math.max(1, config.RecycleChecksPerFrame), zombies.Length);

        // Frustum of the player's camera - a far zombie inside it is never recycled.
        Camera cam = Camera.main;
        bool haveFrustum = cam != null;
        if (haveFrustum)
            GeometryUtility.CalculateFrustumPlanes(cam, FrustumPlanes);

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

            // Far, but on screen -> leave it. Never pop something the player is looking at.
            if (haveFrustum)
            {
                Bounds b = new Bounds((Vector3)transform.Position, new Vector3(4f, 5f, 4f));
                if (GeometryUtility.TestPlanesAABB(FrustumPlanes, b))
                    continue;
            }

            if (TryFindRecyclePoint(anchor, config, groundMask, rayHeight, out float3 dest))
            {
                float groundOffset = em.HasComponent<ZombieGroundOffset>(zombie)
                    ? em.GetComponentData<ZombieGroundOffset>(zombie).Value
                    : 0f;

                LocalTransform moved = transform.WithPosition(new float3(dest.x, dest.y + groundOffset, dest.z));
                em.SetComponentData(zombie, moved);
                // Keep the render matrix in sync this frame, not next - avoids a 1-frame pop.
                if (em.HasComponent<LocalToWorld>(zombie))
                    em.SetComponentData(zombie, new LocalToWorld { Value = float4x4.TRS(moved.Position, moved.Rotation, new float3(moved.Scale)) });

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
