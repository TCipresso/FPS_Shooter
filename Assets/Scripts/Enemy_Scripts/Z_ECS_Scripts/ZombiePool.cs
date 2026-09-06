using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

public static class ZombiePool
{
    // Acquire an inactive zombie of the given weighted-set index, or instantiate a fresh
    // one from the matching prefab. Pooled entities keep their ZombiePrefabIndex so a
    // released zombie is only ever reused as the same type.
    public static Entity Acquire(EntityManager em, NativeList<Entity> pool, int prefabIndex, Entity prefab)
    {
        for (int i = pool.Length - 1; i >= 0; i--)
        {
            Entity candidate = pool[i];

            if (!em.Exists(candidate))
            {
                pool.RemoveAtSwapBack(i);
                continue;
            }

            if (em.HasComponent<ZombiePrefabIndex>(candidate) &&
                em.GetComponentData<ZombiePrefabIndex>(candidate).Value != prefabIndex)
                continue;

            pool.RemoveAtSwapBack(i);
            em.SetEnabled(candidate, true);
            ResetState(em, candidate);
            if (em.HasComponent<ZombiePrefabIndex>(candidate))
                em.SetComponentData(candidate, new ZombiePrefabIndex { Value = prefabIndex });
            return candidate;
        }

        Entity spawned = em.Instantiate(prefab);
        if (em.HasComponent<ZombiePrefabIndex>(spawned))
            em.SetComponentData(spawned, new ZombiePrefabIndex { Value = prefabIndex });
        return spawned;
    }

    public static void Release(EntityManager em, NativeList<Entity> pool, Entity entity)
    {
        if (!em.Exists(entity))
            return;

        em.SetEnabled(entity, false);
        pool.Add(entity);
    }

    static void ResetState(EntityManager em, Entity entity)
    {
        float baseSpeed = 0f;
        int baseMaxHealth = 0;
        int baseContactDamage = 0;
        if (em.HasComponent<ZombieBaseStats>(entity))
        {
            ZombieBaseStats baseStats = em.GetComponentData<ZombieBaseStats>(entity);
            baseSpeed = baseStats.BaseMoveSpeed;
            baseMaxHealth = baseStats.BaseMaxHealth;
            baseContactDamage = baseStats.BaseContactDamage;
        }

        if (em.HasComponent<ZombieHealth>(entity) && baseMaxHealth > 0)
            em.SetComponentData(entity, new ZombieHealth { Current = baseMaxHealth, Max = baseMaxHealth });

        if (em.HasComponent<ZombieMoveSpeed>(entity) && baseSpeed > 0f)
            em.SetComponentData(entity, new ZombieMoveSpeed { Value = baseSpeed });

        if (em.HasComponent<ZombieContactDamage>(entity) && baseContactDamage > 0)
            em.SetComponentData(entity, new ZombieContactDamage { Value = baseContactDamage });

        if (em.HasComponent<ZombieVerticalVelocity>(entity))
            em.SetComponentData(entity, new ZombieVerticalVelocity { Value = 0f });

        if (em.HasComponent<ZombieContactCooldown>(entity))
            em.SetComponentData(entity, new ZombieContactCooldown { Value = 0f });

        if (em.HasComponent<ZombieClimbState>(entity))
            em.SetComponentData(entity, new ZombieClimbState { WasBlocked = false, WasWallBlocked = false });

        if (em.HasComponent<ZombieTarget>(entity))
            em.SetComponentData(entity, new ZombieTarget { Index = -1, Position = float3.zero, HasTarget = false, RecheckTimer = 0f });
    }
}
