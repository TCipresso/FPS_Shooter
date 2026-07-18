using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct ZombieTag : IComponentData { }

public struct ZombieMoveSpeed : IComponentData
{
    public float Value;
}

public struct ZombieHealth : IComponentData
{
    public int Current;
    public int Max;
}

public struct ZombieContactCooldown : IComponentData
{
    public float Value;
}

public struct ZombieHitboxHeight : IComponentData
{
    public float Value;
}

public struct ZombieHitboxRadius : IComponentData
{
    public float Value;
}

public struct ZombieVerticalVelocity : IComponentData
{
    public float Value;
}

public struct ZombieClimbState : IComponentData
{
    public bool WasBlocked;
    public bool WasWallBlocked;
}

public struct ZombieGroundOffset : IComponentData
{
    public float Value;
}

public struct PlayerPosition : IComponentData
{
    public float3 Value;
    public bool IsValid;
}

public struct ZombieSpawnConfig : IComponentData
{
    public Entity Prefab;
    public int SpawnCount;
    public float SpawnRadius;
    public bool HasSpawned;
}

public struct ZombieDamageEvent
{
    public Entity Target;
    public int Amount;
}

public struct PlayerDamageEvent
{
    public int Amount;
}

public struct ZombieDamageQueue : IComponentData
{
    public NativeQueue<ZombieDamageEvent> Queue;
}

public struct PlayerDamageQueue : IComponentData
{
    public NativeQueue<PlayerDamageEvent> Queue;
}

public struct ZombieGridEntry
{
    public Entity Entity;
    public float3 Position;
    public float Height;
    public float Radius;
    public float GroundOffset;
}

public struct ZombieGridSingleton : IComponentData
{
    public NativeParallelMultiHashMap<int3, ZombieGridEntry> Grid;
    public float CellSize;
}

public struct ZombieWallConfig : IComponentData
{
    public int WallLayerMask;
    public float CheckDistance;
    public float CheckRadius;
    public float ClimbSpeed;
    public float LedgeLaunchSpeed;
    public float MaxStackHeight;
    public int GroundLayerMask;
    public float GroundCheckDistance;
}