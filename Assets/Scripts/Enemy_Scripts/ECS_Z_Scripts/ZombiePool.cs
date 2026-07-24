using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

public static class ZombiePool
{
    public static Entity Acquire(EntityManager em, NativeList<Entity> pool, Entity prefab)
    {
        while (pool.Length > 0)
        {
            Entity candidate = pool[pool.Length - 1];
            pool.RemoveAt(pool.Length - 1);

            if (!em.Exists(candidate))
                continue;

            em.SetEnabled(candidate, true);
            ResetState(em, candidate);
            return candidate;
        }

        return em.Instantiate(prefab);
    }

    public static void Release(EntityManager em, NativeList<Entity> pool, Entity entity)
    {
        if (!em.Exists(entity))
            return;

        if (em.HasComponent<ZombieNetId>(entity))
            em.SetComponentData(entity, new ZombieNetId { Value = 0 });

        em.SetEnabled(entity, false);
        pool.Add(entity);
    }

    static void ResetState(EntityManager em, Entity entity)
    {
        if (em.HasComponent<ZombieHealth>(entity))
        {
            ZombieHealth health = em.GetComponentData<ZombieHealth>(entity);
            health.Current = health.Max;
            em.SetComponentData(entity, health);
        }

        if (em.HasComponent<ZombieVerticalVelocity>(entity))
            em.SetComponentData(entity, new ZombieVerticalVelocity { Value = 0f });

        if (em.HasComponent<ZombieContactCooldown>(entity))
            em.SetComponentData(entity, new ZombieContactCooldown { Value = 0f });

        if (em.HasComponent<ZombieClimbState>(entity))
            em.SetComponentData(entity, new ZombieClimbState { WasBlocked = false, WasWallBlocked = false });

        if (em.HasComponent<ZombieTarget>(entity))
            em.SetComponentData(entity, new ZombieTarget { Index = -1, Position = float3.zero, HasTarget = false, RecheckTimer = 0f });

        if (em.HasComponent<ZombieNetId>(entity))
            em.SetComponentData(entity, new ZombieNetId { Value = 0 });

        if (em.HasComponent<ZombieInterpolation>(entity))
        {
            em.SetComponentData(entity, new ZombieInterpolation
            {
                PrevPosition = float3.zero,
                TargetPosition = float3.zero,
                PrevYaw = 0f,
                TargetYaw = 0f,
                Elapsed = 0f,
                Duration = 0f,
                LastUpdateTime = 0d
            });
        }
    }
}
