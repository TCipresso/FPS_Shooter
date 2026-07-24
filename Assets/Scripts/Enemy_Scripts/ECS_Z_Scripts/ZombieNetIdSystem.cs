using Unity.Entities;
using Unity.Collections;
using Unity.Burst;

[UpdateBefore(typeof(ZombieMovementSystem))]
public partial struct ZombieNetIdSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieSingletonTag>(out Entity singletonEntity))
            return;

        ZombieSimAuthority authority = SystemAPI.GetComponent<ZombieSimAuthority>(singletonEntity);

        if (authority.ShouldSimulate)
            AssignIds(ref state, singletonEntity);

        RebuildMap(ref state, singletonEntity);
    }

    void AssignIds(ref SystemState state, Entity singletonEntity)
    {
        ZombieNetIdCounter counter = SystemAPI.GetComponent<ZombieNetIdCounter>(singletonEntity);
        NativeQueue<ushort> freeIds = SystemAPI.GetComponent<ZombieFreeNetIds>(singletonEntity).Queue;

        bool changed = false;

        foreach (var netId in SystemAPI.Query<RefRW<ZombieNetId>>().WithAll<ZombieTag>())
        {
            if (netId.ValueRO.Value != 0)
                continue;

            ushort assigned;
            if (!freeIds.TryDequeue(out assigned))
            {
                if (counter.Next == 0)
                    counter.Next = 1;

                assigned = counter.Next;
                counter.Next++;
                if (counter.Next == 0)
                    counter.Next = 1;

                changed = true;
            }

            netId.ValueRW.Value = assigned;
        }

        if (changed)
            SystemAPI.SetComponent(singletonEntity, counter);
    }

    void RebuildMap(ref SystemState state, Entity singletonEntity)
    {
        ZombieNetIdMap idMap = SystemAPI.GetComponent<ZombieNetIdMap>(singletonEntity);
        idMap.Map.Clear();

        int zombieCount = SystemAPI.QueryBuilder().WithAll<ZombieTag, ZombieNetId>().Build().CalculateEntityCount();
        if (zombieCount > idMap.Map.Capacity)
            idMap.Map.Capacity = zombieCount;

        state.Dependency = new BuildNetIdMapJob
        {
            MapWriter = idMap.Map.AsParallelWriter()
        }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(ZombieTag))]
partial struct BuildNetIdMapJob : IJobEntity
{
    public NativeParallelHashMap<int, Entity>.ParallelWriter MapWriter;

    void Execute(Entity entity, in ZombieNetId netId)
    {
        if (netId.Value == 0)
            return;

        MapWriter.TryAdd(netId.Value, entity);
    }
}
