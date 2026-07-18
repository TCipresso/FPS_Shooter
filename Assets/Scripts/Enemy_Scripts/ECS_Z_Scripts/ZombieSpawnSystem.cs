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

        PlayerPosition playerPosition = default;
        bool foundPlayer = false;
        foreach (var p in SystemAPI.Query<RefRO<PlayerPosition>>())
        {
            playerPosition = p.ValueRO;
            foundPlayer = true;
        }
        if (!foundPlayer || !playerPosition.IsValid)
            return;

        EntityManager em = state.EntityManager;
        NativeArray<Entity> spawned = new NativeArray<Entity>(config.SpawnCount, Allocator.Temp);
        em.Instantiate(config.Prefab, spawned);

        Random random = new Random((uint)System.DateTime.Now.Ticks | 1);
        for (int i = 0; i < spawned.Length; i++)
        {
            float2 offset = random.NextFloat2Direction() * random.NextFloat(0f, config.SpawnRadius);
            float3 pos = playerPosition.Value + new float3(offset.x, 0f, offset.y);
            em.SetComponentData(spawned[i], LocalTransform.FromPosition(pos));
        }
        spawned.Dispose();

        config.HasSpawned = true;
        em.SetComponentData(configEntity, config);
    }
}
