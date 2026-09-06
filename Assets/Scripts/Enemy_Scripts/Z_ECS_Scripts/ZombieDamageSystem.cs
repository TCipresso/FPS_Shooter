using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ZombieDamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieSingletonTag>(out Entity singletonEntity))
            return;

        NativeQueue<ZombieDamageEvent> queue = SystemAPI.GetComponent<ZombieDamageQueue>(singletonEntity).Queue;
        if (queue.IsEmpty())
            return;

        state.Dependency.Complete();

        NativeList<Entity> pool = SystemAPI.GetComponent<ZombiePoolSingleton>(singletonEntity).Inactive;
        NativeQueue<ZombieCreditEvent> creditQueue = SystemAPI.GetComponent<ZombieCreditQueue>(singletonEntity).Queue;

        EntityManager em = state.EntityManager;
        int killsThisFrame = 0;

        while (queue.TryDequeue(out ZombieDamageEvent damageEvent))
        {
            if (!em.Exists(damageEvent.Target) || !em.HasComponent<ZombieHealth>(damageEvent.Target))
                continue; // target already gone (e.g. two shots the same frame)

            ZombieHealth health = em.GetComponentData<ZombieHealth>(damageEvent.Target);
            health.Current -= damageEvent.Amount;

            bool killed = health.Current <= 0;

            float xpBounty = 0f;
            float3 deathPos = float3.zero;
            if (killed)
            {
                if (em.HasComponent<ZombieXpBounty>(damageEvent.Target))
                    xpBounty = em.GetComponentData<ZombieXpBounty>(damageEvent.Target).Value;
                if (em.HasComponent<LocalTransform>(damageEvent.Target))
                    deathPos = em.GetComponentData<LocalTransform>(damageEvent.Target).Position;
            }

            creditQueue.Enqueue(new ZombieCreditEvent
            {
                PlayerIndex = damageEvent.PlayerIndex,
                IsKill = killed,
                WeaponTicket = damageEvent.WeaponTicket,
                XpAmount = xpBounty,
                Position = deathPos
            });

            if (killed)
            {
                ZombiePool.Release(em, pool, damageEvent.Target);
                killsThisFrame++;
            }
            else
            {
                em.SetComponentData(damageEvent.Target, health);
            }
        }

        if (killsThisFrame > 0 && SystemAPI.TryGetSingletonEntity<ZombieSpawnStats>(out Entity statsEntity))
        {
            ZombieSpawnStats stats = SystemAPI.GetComponent<ZombieSpawnStats>(statsEntity);
            stats.TotalKilled += killsThisFrame;
            SystemAPI.SetComponent(statsEntity, stats);
        }
    }
}
