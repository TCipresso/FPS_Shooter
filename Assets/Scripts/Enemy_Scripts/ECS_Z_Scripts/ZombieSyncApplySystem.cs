using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[UpdateAfter(typeof(ZombieNetIdSystem))]
public partial struct ZombieSyncApplySystem : ISystem
{
    const float StaleTimeout = 2f;
    const double DespawnSuppressionWindow = 1.5d;

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieSingletonTag>(out Entity singletonEntity))
            return;

        ZombieSimAuthority authority = SystemAPI.GetComponent<ZombieSimAuthority>(singletonEntity);
        if (!authority.IsNetworked || authority.IsServer)
            return;

        if (!SystemAPI.TryGetSingleton<ZombieSpawnConfig>(out ZombieSpawnConfig spawnConfig))
            return;

        state.Dependency.Complete();

        EntityManager em = state.EntityManager;
        double now = SystemAPI.Time.ElapsedTime;

        NativeList<Entity> pool = SystemAPI.GetComponent<ZombiePoolSingleton>(singletonEntity).Inactive;
        NativeParallelHashMap<int, double> recentDespawns = SystemAPI.GetComponent<ZombieRecentDespawns>(singletonEntity).Map;
        NativeParallelHashMap<int, Entity> idMap = SystemAPI.GetComponent<ZombieNetIdMap>(singletonEntity).Map;

        ApplyDespawns(ref state, singletonEntity, em, pool, idMap, recentDespawns, now);
        ApplySnapshots(ref state, singletonEntity, em, pool, idMap, recentDespawns, spawnConfig, now);
        RemoveStale(ref state, em, pool, now);
        Interpolate(ref state);
    }

    void ApplyDespawns(ref SystemState state, Entity singletonEntity, EntityManager em, NativeList<Entity> pool,
        NativeParallelHashMap<int, Entity> idMap, NativeParallelHashMap<int, double> recentDespawns, double now)
    {
        NativeQueue<ushort> despawns = SystemAPI.GetComponent<ZombieClientDespawnQueue>(singletonEntity).Queue;
        if (despawns.IsEmpty())
            return;

        while (despawns.TryDequeue(out ushort netId))
        {
            recentDespawns[netId] = now;

            if (idMap.TryGetValue(netId, out Entity entity) && em.Exists(entity))
            {
                idMap.Remove(netId);
                ZombiePool.Release(em, pool, entity);
            }
        }
    }

    void ApplySnapshots(ref SystemState state, Entity singletonEntity, EntityManager em, NativeList<Entity> pool,
        NativeParallelHashMap<int, Entity> idMap, NativeParallelHashMap<int, double> recentDespawns,
        ZombieSpawnConfig spawnConfig, double now)
    {
        NativeQueue<ZombieSyncEntry> syncQueue = SystemAPI.GetComponent<ZombieSyncQueue>(singletonEntity).Queue;
        if (syncQueue.IsEmpty())
            return;

        while (syncQueue.TryDequeue(out ZombieSyncEntry entry))
        {
            if (idMap.TryGetValue(entry.NetId, out Entity existing) && em.Exists(existing))
            {
                UpdateInterpolationTarget(em, existing, entry, now);
                continue;
            }

            if (recentDespawns.TryGetValue(entry.NetId, out double despawnTime))
            {
                if (now - despawnTime < DespawnSuppressionWindow)
                    continue;

                recentDespawns.Remove(entry.NetId);
            }

            Entity spawned = ZombiePool.Acquire(em, pool, spawnConfig.Prefab);
            em.SetComponentData(spawned, new ZombieNetId { Value = entry.NetId });

            LocalTransform transform = em.GetComponentData<LocalTransform>(spawned);
            em.SetComponentData(spawned, transform
                .WithPosition(entry.Position)
                .WithRotation(quaternion.RotateY(math.radians(entry.Yaw))));

            em.SetComponentData(spawned, new ZombieInterpolation
            {
                PrevPosition = entry.Position,
                TargetPosition = entry.Position,
                PrevYaw = entry.Yaw,
                TargetYaw = entry.Yaw,
                Elapsed = 0f,
                Duration = 0f,
                LastUpdateTime = now
            });

            idMap[entry.NetId] = spawned;
        }
    }

    void UpdateInterpolationTarget(EntityManager em, Entity entity, ZombieSyncEntry entry, double now)
    {
        ZombieInterpolation interp = em.GetComponentData<ZombieInterpolation>(entity);
        LocalTransform transform = em.GetComponentData<LocalTransform>(entity);

        float duration = (float)(now - interp.LastUpdateTime);
        duration = math.clamp(duration, 0.02f, 0.5f);

        float3 currentForward = math.mul(transform.Rotation, new float3(0f, 0f, 1f));
        float currentYaw = math.degrees(math.atan2(currentForward.x, currentForward.z));

        em.SetComponentData(entity, new ZombieInterpolation
        {
            PrevPosition = transform.Position,
            TargetPosition = entry.Position,
            PrevYaw = currentYaw,
            TargetYaw = entry.Yaw,
            Elapsed = 0f,
            Duration = duration,
            LastUpdateTime = now
        });
    }

    void RemoveStale(ref SystemState state, EntityManager em, NativeList<Entity> pool, double now)
    {
        NativeList<Entity> stale = new NativeList<Entity>(16, Allocator.Temp);

        foreach (var (interp, entity) in SystemAPI.Query<RefRO<ZombieInterpolation>>().WithAll<ZombieTag>().WithEntityAccess())
        {
            if (now - interp.ValueRO.LastUpdateTime > StaleTimeout)
                stale.Add(entity);
        }

        for (int i = 0; i < stale.Length; i++)
            ZombiePool.Release(em, pool, stale[i]);

        stale.Dispose();
    }

    void Interpolate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, interp) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<ZombieInterpolation>>().WithAll<ZombieTag>())
        {
            if (interp.ValueRO.Duration <= 0f)
                continue;

            interp.ValueRW.Elapsed += deltaTime;
            float t = math.saturate(interp.ValueRO.Elapsed / interp.ValueRO.Duration);

            transform.ValueRW.Position = math.lerp(interp.ValueRO.PrevPosition, interp.ValueRO.TargetPosition, t);

            float delta = math.fmod(interp.ValueRO.TargetYaw - interp.ValueRO.PrevYaw + 540f, 360f) - 180f;
            float yaw = interp.ValueRO.PrevYaw + delta * t;
            transform.ValueRW.Rotation = quaternion.RotateY(math.radians(yaw));
        }
    }
}