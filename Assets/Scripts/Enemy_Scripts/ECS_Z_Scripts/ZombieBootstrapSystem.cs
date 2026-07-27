using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public partial struct ZombieBootstrapSystem : ISystem
{
    public const int MaxPlayers = 4;

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

        state.EntityManager.AddComponentData(singleton, new ZombieSimAuthority { IsNetworked = false, IsServer = true });
        state.EntityManager.AddComponentData(singleton, new ZombieNetIdCounter { Next = 1 });
        state.EntityManager.AddComponentData(singleton, new ZombieFreeNetIds { Queue = new NativeQueue<ushort>(Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombieNetIdMap { Map = new NativeParallelHashMap<int, Entity>(1024, Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombieSnapshotBuffer { Entries = new NativeList<ZombieSnapshotEntry>(1024, Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombieSnapshotState { Timer = 0f, Interval = 0.05f, HasNewSnapshot = false });
        state.EntityManager.AddComponentData(singleton, new ZombieSyncQueue { Queue = new NativeQueue<ZombieSyncEntry>(Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombieClientDespawnQueue { Queue = new NativeQueue<ushort>(Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombieServerDespawnQueue { Queue = new NativeQueue<ushort>(Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombieDamageRequestQueue { Queue = new NativeQueue<ZombieDamageRequest>(Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombiePoolSingleton { Inactive = new NativeList<Entity>(512, Allocator.Persistent) });
        state.EntityManager.AddComponentData(singleton, new ZombieRecentDespawns { Map = new NativeParallelHashMap<int, double>(512, Allocator.Persistent) });
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
        foreach (var q in SystemAPI.Query<RefRO<ZombieFreeNetIds>>())
            q.ValueRO.Queue.Dispose();
        foreach (var m in SystemAPI.Query<RefRO<ZombieNetIdMap>>())
            m.ValueRO.Map.Dispose();
        foreach (var b in SystemAPI.Query<RefRO<ZombieSnapshotBuffer>>())
            b.ValueRO.Entries.Dispose();
        foreach (var q in SystemAPI.Query<RefRO<ZombieSyncQueue>>())
            q.ValueRO.Queue.Dispose();
        foreach (var q in SystemAPI.Query<RefRO<ZombieClientDespawnQueue>>())
            q.ValueRO.Queue.Dispose();
        foreach (var q in SystemAPI.Query<RefRO<ZombieServerDespawnQueue>>())
            q.ValueRO.Queue.Dispose();
        foreach (var q in SystemAPI.Query<RefRO<ZombieDamageRequestQueue>>())
            q.ValueRO.Queue.Dispose();
        foreach (var p in SystemAPI.Query<RefRO<ZombiePoolSingleton>>())
            p.ValueRO.Inactive.Dispose();
        foreach (var d in SystemAPI.Query<RefRO<ZombieRecentDespawns>>())
            d.ValueRO.Map.Dispose();
    }
}