using Unity.Entities;
using Unity.Collections;

[UpdateAfter(typeof(ZombieDamageSystem))]
public partial struct ZombieDeathSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieSingletonTag>(out Entity singletonEntity))
            return;

        ZombieSimAuthority authority = SystemAPI.GetComponent<ZombieSimAuthority>(singletonEntity);

        if (authority.IsNetworked && !authority.IsServer)
        {
            ApplyClientDeaths(ref state, singletonEntity);
        }

        TickDeaths(ref state, singletonEntity, authority);
    }

    void ApplyClientDeaths(ref SystemState state, Entity singletonEntity)
    {
        NativeQueue<ushort> deaths = SystemAPI.GetComponent<ZombieClientDeathQueue>(singletonEntity).Queue;
        if (deaths.IsEmpty())
            return;

        state.Dependency.Complete();

        EntityManager em = state.EntityManager;
        NativeParallelHashMap<int, Entity> idMap = SystemAPI.GetComponent<ZombieNetIdMap>(singletonEntity).Map;

        while (deaths.TryDequeue(out ushort netId))
        {
            if (idMap.TryGetValue(netId, out Entity entity) && em.Exists(entity) && !em.HasComponent<ZombieDead>(entity))
                em.AddComponentData(entity, new ZombieDead { Timer = 0.35f });
        }
    }

    void TickDeaths(ref SystemState state, Entity singletonEntity, ZombieSimAuthority authority)
    {
        bool hostReleases = authority.ShouldSimulate;

        int deadCount = SystemAPI.QueryBuilder().WithAll<ZombieDead>().Build().CalculateEntityCount();
        if (deadCount == 0)
            return;

        state.Dependency.Complete();

        EntityManager em = state.EntityManager;
        NativeList<Entity> pool = SystemAPI.GetComponent<ZombiePoolSingleton>(singletonEntity).Inactive;

        NativeQueue<ushort> despawnQueue = default;
        bool hasDespawnQueue = false;
        if (hostReleases && authority.IsNetworked)
        {
            despawnQueue = SystemAPI.GetComponent<ZombieServerDespawnQueue>(singletonEntity).Queue;
            hasDespawnQueue = true;
        }

        float deltaTime = SystemAPI.Time.DeltaTime;
        NativeList<Entity> toRelease = new NativeList<Entity>(64, Allocator.Temp);

        foreach (var (dead, entity) in SystemAPI.Query<RefRW<ZombieDead>>().WithEntityAccess())
        {
            dead.ValueRW.Timer -= deltaTime;
            if (dead.ValueRO.Timer <= 0f)
                toRelease.Add(entity);
        }

        for (int i = 0; i < toRelease.Length; i++)
        {
            Entity entity = toRelease[i];

            if (hasDespawnQueue && em.HasComponent<ZombieNetId>(entity))
            {
                ushort netId = em.GetComponentData<ZombieNetId>(entity).Value;
                if (netId != 0)
                    despawnQueue.Enqueue(netId);
            }

            em.RemoveComponent<ZombieDead>(entity);
            ZombiePool.Release(em, pool, entity);
        }

        toRelease.Dispose();
    }
}
