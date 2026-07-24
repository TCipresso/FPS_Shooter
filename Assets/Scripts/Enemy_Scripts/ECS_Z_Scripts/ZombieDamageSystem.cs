using Unity.Entities;
using Unity.Collections;

public partial struct ZombieDamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieSingletonTag>(out Entity singletonEntity))
            return;

        ZombieSimAuthority authority = SystemAPI.GetComponent<ZombieSimAuthority>(singletonEntity);
        if (!authority.ShouldSimulate)
            return;

        NativeQueue<ZombieDamageEvent> queue = SystemAPI.GetComponent<ZombieDamageQueue>(singletonEntity).Queue;
        if (queue.IsEmpty())
            return;

        state.Dependency.Complete();

        NativeQueue<ushort> despawnQueue = SystemAPI.GetComponent<ZombieServerDespawnQueue>(singletonEntity).Queue;
        NativeList<Entity> pool = SystemAPI.GetComponent<ZombiePoolSingleton>(singletonEntity).Inactive;

        EntityManager em = state.EntityManager;

        while (queue.TryDequeue(out ZombieDamageEvent damageEvent))
        {
            if (!em.Exists(damageEvent.Target))
                continue;
            if (!em.HasComponent<ZombieHealth>(damageEvent.Target))
                continue;

            ZombieHealth health = em.GetComponentData<ZombieHealth>(damageEvent.Target);
            health.Current -= damageEvent.Amount;

            if (health.Current <= 0)
            {
                if (em.HasComponent<ZombieNetId>(damageEvent.Target))
                {
                    ushort netId = em.GetComponentData<ZombieNetId>(damageEvent.Target).Value;
                    if (netId != 0)
                        despawnQueue.Enqueue(netId);
                }

                ZombiePool.Release(em, pool, damageEvent.Target);
            }
            else
            {
                em.SetComponentData(damageEvent.Target, health);
            }
        }
    }
}