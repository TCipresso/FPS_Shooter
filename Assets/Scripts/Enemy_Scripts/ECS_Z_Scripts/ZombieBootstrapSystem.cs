using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public partial struct ZombieBootstrapSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        Entity singleton = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(singleton, new PlayerPosition { Value = float3.zero, IsValid = false });
        state.EntityManager.AddComponentData(singleton, new ZombieDamageQueue { Queue = new NativeQueue<ZombieDamageEvent>(Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new PlayerDamageQueue { Queue = new NativeQueue<PlayerDamageEvent>(Allocator.Persistent) });
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
        foreach (var g in SystemAPI.Query<RefRO<ZombieGridSingleton>>())
            g.ValueRO.Grid.Dispose();
    }
}