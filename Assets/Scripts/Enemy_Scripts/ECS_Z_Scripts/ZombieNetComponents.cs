using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct ZombieNetId : IComponentData
{
    public ushort Value;
}

public struct ZombieInterpolation : IComponentData
{
    public float3 PrevPosition;
    public float3 TargetPosition;
    public float PrevYaw;
    public float TargetYaw;
    public float Elapsed;
    public float Duration;
    public double LastUpdateTime;
}

public struct ZombieSimAuthority : IComponentData
{
    public bool IsNetworked;
    public bool IsServer;

    public bool ShouldSimulate => !IsNetworked || IsServer;
}

public struct ZombieNetIdCounter : IComponentData
{
    public ushort Next;
}

public struct ZombieFreeNetIds : IComponentData
{
    public NativeQueue<ushort> Queue;
}

public struct ZombieNetIdMap : IComponentData
{
    public NativeParallelHashMap<int, Entity> Map;
}

public struct ZombieSnapshotEntry
{
    public ushort NetId;
    public float3 Position;
    public float Yaw;
}

public struct ZombieSnapshotBuffer : IComponentData
{
    public NativeList<ZombieSnapshotEntry> Entries;
}

public struct ZombieSnapshotState : IComponentData
{
    public float Timer;
    public float Interval;
    public bool HasNewSnapshot;
}

public struct ZombieSyncEntry
{
    public ushort NetId;
    public float3 Position;
    public float Yaw;
}

public struct ZombieSyncQueue : IComponentData
{
    public NativeQueue<ZombieSyncEntry> Queue;
}

public struct ZombieClientDespawnQueue : IComponentData
{
    public NativeQueue<ushort> Queue;
}

public struct ZombieServerDespawnQueue : IComponentData
{
    public NativeQueue<ushort> Queue;
}

public struct ZombieDamageRequest
{
    public ushort NetId;
    public int Amount;
}

public struct ZombieDamageRequestQueue : IComponentData
{
    public NativeQueue<ZombieDamageRequest> Queue;
}

public struct ZombiePoolSingleton : IComponentData
{
    public NativeList<Entity> Inactive;
}

public struct ZombieRecentDespawns : IComponentData
{
    public NativeParallelHashMap<int, double> Map;
}