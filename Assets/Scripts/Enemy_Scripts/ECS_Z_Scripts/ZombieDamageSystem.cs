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

        NativeQueue<ushort> deathQueue = SystemAPI.GetComponent<ZombieServerDeathQueue>(singletonEntity).Queue;

        float deathDuration = 0.35f;
        if (SystemAPI.TryGetSingleton<ZombieSpawnTuning>(out ZombieSpawnTuning tuning))
            deathDuration = tuning.DeathDuration;

        EntityManager em = state.EntityManager;

        while (queue.TryDequeue(out ZombieDamageEvent damageEvent))
        {
            if (!em.Exists(damageEvent.Target))
                continue;
            if (!em.HasComponent<ZombieHealth>(damageEvent.Target))
                continue;
            if (em.HasComponent<ZombieDead>(damageEvent.Target))
                continue;

            ZombieHealth health = em.GetComponentData<ZombieHealth>(damageEvent.Target);
            health.Current -= damageEvent.Amount;

            if (health.Current <= 0)
            {
                if (em.HasComponent<ZombieNetId>(damageEvent.Target))
                {
                    ushort netId = em.GetComponentData<ZombieNetId>(damageEvent.Target).Value;
                    if (netId != 0)
                        deathQueue.Enqueue(netId);
                }

                em.AddComponentData(damageEvent.Target, new ZombieDead { Timer = deathDuration });
            }
            else
            {
                em.SetComponentData(damageEvent.Target, health);
            }
        }
    }
}