using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct ZombieTag : IComponentData { }


public struct ZombieMoveSpeed : IComponentData
{
    public float Value;
}

public struct ZombieBaseStats : IComponentData
{
    public float BaseMoveSpeed;
    public int BaseMaxHealth;
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

public struct ZombieSingletonTag : IComponentData { }

[InternalBufferCapacity(4)]
public struct PlayerTargetElement : IBufferElementData
{
    public float3 Position;
    public bool IsRegistered;
    public bool IsTargetable;
}

public struct ZombieTarget : IComponentData
{
    public int Index;
    public float3 Position;
    public bool HasTarget;
    public float RecheckTimer;
}

public struct ZombieSpawnConfig : IComponentData
{
    public Entity Prefab;
}

public struct ZombieDamageEvent
{
    public Entity Target;
    public int Amount;
    public int PlayerIndex;
}

public struct PlayerDamageEvent
{
    public int PlayerIndex;
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

public struct ZombieCreditEvent
{
    public int PlayerIndex;
    public bool IsKill;
}

public struct ZombieCreditQueue : IComponentData
{
    public NativeQueue<ZombieCreditEvent> Queue;
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

public struct ZombieTargetConfig : IComponentData
{
    public float RecheckInterval;
    public float SwitchDistanceRatio;
}

public struct ZombieWallConfig : IComponentData
{
    public int WallLayerMask;
    public float CheckDistance;
    public float CheckRadius;
    public float ClimbSpeed;
    public float LedgeLaunchSpeed;
    public float ZombieClimbDistance;
    public float MaxStackHeight;
    public int GroundLayerMask;
    public float GroundCheckDistance;
}