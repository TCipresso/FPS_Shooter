using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[UpdateAfter(typeof(ZombieBootstrapSystem))]
public partial struct ZombieSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieSpawnConfig>(out Entity configEntity))
            return;

        ZombieSpawnConfig config = SystemAPI.GetComponent<ZombieSpawnConfig>(configEntity);
        if (config.HasSpawned)
            return;

        if (!SystemAPI.TryGetSingletonEntity<ZombieSingletonTag>(out Entity singletonEntity))
            return;

        ZombieSimAuthority authority = SystemAPI.GetComponent<ZombieSimAuthority>(singletonEntity);
        if (!authority.ShouldSimulate)
            return;

        DynamicBuffer<PlayerTargetElement> players = SystemAPI.GetBuffer<PlayerTargetElement>(singletonEntity);

        float3 anchor = float3.zero;
        bool foundAnchor = false;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].IsRegistered)
            {
                anchor = players[i].Position;
                foundAnchor = true;
                break;
            }
        }
        if (!foundAnchor)
            return;

        ZombieTargetConfig targetConfig = SystemAPI.GetComponent<ZombieTargetConfig>(singletonEntity);

        EntityManager em = state.EntityManager;
        NativeList<Entity> pool = SystemAPI.GetComponent<ZombiePoolSingleton>(singletonEntity).Inactive;

        NativeArray<Entity> spawned = new NativeArray<Entity>(config.SpawnCount, Allocator.Temp);

        int pooledTaken = 0;
        for (int i = 0; i < spawned.Length && pool.Length > 0; i++)
        {
            spawned[i] = ZombiePool.Acquire(em, pool, config.Prefab);
            pooledTaken++;
        }

        int remaining = spawned.Length - pooledTaken;
        if (remaining > 0)
        {
            NativeArray<Entity> fresh = new NativeArray<Entity>(remaining, Allocator.Temp);
            em.Instantiate(config.Prefab, fresh);
            for (int i = 0; i < remaining; i++)
                spawned[pooledTaken + i] = fresh[i];
            fresh.Dispose();
        }

        Random random = new Random((uint)System.DateTime.Now.Ticks | 1);
        for (int i = 0; i < spawned.Length; i++)
        {
            float2 offset = random.NextFloat2Direction() * random.NextFloat(0f, config.SpawnRadius);
            float3 pos = anchor + new float3(offset.x, 0f, offset.y);
            LocalTransform current = em.GetComponentData<LocalTransform>(spawned[i]);
            em.SetComponentData(spawned[i], current.WithPosition(pos));

            em.SetComponentData(spawned[i], new ZombieTarget
            {
                Index = -1,
                Position = float3.zero,
                HasTarget = false,
                RecheckTimer = random.NextFloat(0f, targetConfig.RecheckInterval)
            });
        }
        spawned.Dispose();

        config.HasSpawned = true;
        em.SetComponentData(configEntity, config);
    }
}