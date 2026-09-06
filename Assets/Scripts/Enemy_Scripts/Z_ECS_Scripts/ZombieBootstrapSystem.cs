using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public partial struct ZombieBootstrapSystem : ISystem
{
    // Singleplayer: exactly one player slot. Kept as a buffer so the movement/climb
    // targeting job is untouched.
    public const int MaxPlayers = 1;

    public void OnCreate(ref SystemState state)
    {
        Entity singleton = state.EntityManager.CreateEntity();

        state.EntityManager.AddComponent<ZombieSingletonTag>(singleton);

        DynamicBuffer<PlayerTargetElement> players = state.EntityManager.AddBuffer<PlayerTargetElement>(singleton);
        for (int i = 0; i < MaxPlayers; i++)
        {
            players.Add(new PlayerTargetElement
            {
                Position = float3.zero,
                IsRegistered = false,
                IsTargetable = false
            });
        }

        state.EntityManager.AddComponentData(singleton, new ZombieTargetConfig
        {
            RecheckInterval = 0.35f,
            SwitchDistanceRatio = 0.8f
        });

        state.EntityManager.AddComponentData(singleton, new ZombieDamageQueue { Queue = new NativeQueue<ZombieDamageEvent>(Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new PlayerDamageQueue { Queue = new NativeQueue<PlayerDamageEvent>(Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombieCreditQueue { Queue = new NativeQueue<ZombieCreditEvent>(Allocator.Persistent) });

        state.EntityManager.AddComponentData(singleton, new ZombiePoolSingleton { Inactive = new NativeList<Entity>(512, Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombieGridSingleton
        {
            Grid = new NativeParallelMultiHashMap<int3, ZombieGridEntry>(1024, Allocator.Persistent),
            CellSize = 3f
        });
    }

    public void OnDestroy(ref SystemState state)
    {
        foreach (var q in SystemAPI.Query<RefRO<ZombieDamageQueue>>())
            q.ValueRO.Queue.Dispose();
        foreach (var q in SystemAPI.Query<RefRO<PlayerDamageQueue>>())
            q.ValueRO.Queue.Dispose();
        foreach (var q in SystemAPI.Query<RefRO<ZombieCreditQueue>>())
            q.ValueRO.Queue.Dispose();
        foreach (var g in SystemAPI.Query<RefRO<ZombieGridSingleton>>())
            g.ValueRO.Grid.Dispose();
        foreach (var p in SystemAPI.Query<RefRO<ZombiePoolSingleton>>())
            p.ValueRO.Inactive.Dispose();
    }
}
