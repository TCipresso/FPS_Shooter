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
        NativeQueue<ZombieCreditEvent> creditQueue = SystemAPI.GetComponent<ZombieCreditQueue>(singletonEntity).Queue;

        EntityManager em = state.EntityManager;
        int killsThisFrame = 0;

        while (queue.TryDequeue(out ZombieDamageEvent damageEvent))
        {
            if (!em.Exists(damageEvent.Target))
                continue;
            if (!em.HasComponent<ZombieHealth>(damageEvent.Target))
                continue;

            ZombieHealth health = em.GetComponentData<ZombieHealth>(damageEvent.Target);
            health.Current -= damageEvent.Amount;

            UnityEngine.Debug.Log($"[ZombieDamageSystem] damage event amount={damageEvent.Amount} playerIndex={damageEvent.PlayerIndex}");

            if (damageEvent.PlayerIndex >= 0)
                creditQueue.Enqueue(new ZombieCreditEvent { PlayerIndex = damageEvent.PlayerIndex, IsKill = false });

            if (health.Current <= 0)
            {
                if (em.HasComponent<ZombieNetId>(damageEvent.Target))
                {
                    ushort netId = em.GetComponentData<ZombieNetId>(damageEvent.Target).Value;
                    if (netId != 0)
                        despawnQueue.Enqueue(netId);
                }

                if (damageEvent.PlayerIndex >= 0)
                    creditQueue.Enqueue(new ZombieCreditEvent { PlayerIndex = damageEvent.PlayerIndex, IsKill = true });

                ZombiePool.Release(em, pool, damageEvent.Target);
                killsThisFrame++;
            }
            else
            {
                em.SetComponentData(damageEvent.Target, health);
            }
        }

        if (killsThisFrame > 0 && SystemAPI.TryGetSingletonEntity<ZombieRoundState>(out Entity roundEntity))
        {
            ZombieRoundState round = SystemAPI.GetComponent<ZombieRoundState>(roundEntity);
            round.KilledThisRound += killsThisFrame;
            SystemAPI.SetComponent(roundEntity, round);
        }
    }
}