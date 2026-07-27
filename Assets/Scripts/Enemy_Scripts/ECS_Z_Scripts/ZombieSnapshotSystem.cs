using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[UpdateAfter(typeof(ZombieMovementSystem))]
[UpdateBefore(typeof(ZombieDamageSystem))]
public partial struct ZombieSnapshotSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieSingletonTag>(out Entity singletonEntity))
            return;

        ZombieSimAuthority authority = SystemAPI.GetComponent<ZombieSimAuthority>(singletonEntity);
        if (!authority.IsNetworked || !authority.IsServer)
            return;

        ApplyDamageRequests(ref state, singletonEntity);

        ZombieSnapshotState snapshotState = SystemAPI.GetComponent<ZombieSnapshotState>(singletonEntity);

        snapshotState.Timer -= SystemAPI.Time.DeltaTime;
        if (snapshotState.Timer > 0f)
        {
            SystemAPI.SetComponent(singletonEntity, snapshotState);
            return;
        }

        snapshotState.Timer = snapshotState.Interval;

        state.Dependency.Complete();

        NativeList<ZombieSnapshotEntry> entries = SystemAPI.GetComponent<ZombieSnapshotBuffer>(singletonEntity).Entries;
        entries.Clear();

        foreach (var (transform, netId) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<ZombieNetId>>().WithAll<ZombieTag>())
        {
            if (netId.ValueRO.Value == 0)
                continue;

            float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
            float yaw = math.degrees(math.atan2(forward.x, forward.z));

            entries.Add(new ZombieSnapshotEntry
            {
                NetId = netId.ValueRO.Value,
                Position = transform.ValueRO.Position,
                Yaw = yaw
            });
        }

        snapshotState.HasNewSnapshot = true;
        SystemAPI.SetComponent(singletonEntity, snapshotState);
    }

    void ApplyDamageRequests(ref SystemState state, Entity singletonEntity)
    {
        NativeQueue<ZombieDamageRequest> requests = SystemAPI.GetComponent<ZombieDamageRequestQueue>(singletonEntity).Queue;
        if (requests.IsEmpty())
            return;

        state.Dependency.Complete();

        NativeParallelHashMap<int, Entity> idMap = SystemAPI.GetComponent<ZombieNetIdMap>(singletonEntity).Map;
        NativeQueue<ZombieDamageEvent> damageQueue = SystemAPI.GetComponent<ZombieDamageQueue>(singletonEntity).Queue;

        while (requests.TryDequeue(out ZombieDamageRequest request))
        {
            if (!idMap.TryGetValue(request.NetId, out Entity target))
                continue;

            damageQueue.Enqueue(new ZombieDamageEvent { Target = target, Amount = request.Amount, PlayerIndex = request.PlayerIndex });
        }
    }
}