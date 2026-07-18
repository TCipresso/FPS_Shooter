using Unity.Entities;
using Unity.Collections;

public partial struct ZombieDamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieDamageQueue>(out Entity singletonEntity))
            return;

        NativeQueue<ZombieDamageEvent> queue = SystemAPI.GetComponent<ZombieDamageQueue>(singletonEntity).Queue;
        if (queue.IsEmpty())
            return;

        EntityManager em = state.EntityManager;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        while (queue.TryDequeue(out ZombieDamageEvent damageEvent))
        {
            if (!em.Exists(damageEvent.Target))
                continue;
            if (!em.HasComponent<ZombieHealth>(damageEvent.Target))
                continue;

            ZombieHealth health = em.GetComponentData<ZombieHealth>(damageEvent.Target);
            health.Current -= damageEvent.Amount;

            if (health.Current <= 0)
                ecb.DestroyEntity(damageEvent.Target);
            else
                em.SetComponentData(damageEvent.Target, health);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
