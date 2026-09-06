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
    public int BaseContactDamage;
}

// Live contact damage (scaled by the difficulty ramp at spawn, mirrors ZombieBase.attackDamage).
public struct ZombieContactDamage : IComponentData
{
    public int Value;
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

// Weighted set of zombie prefabs the spawner draws from (ECS port of
// RadiusEnemySpawner.enemies + EnemySpawnManager's per-id pools).
[InternalBufferCapacity(8)]
public struct ZombieSpawnPrefabElement : IBufferElementData
{
    public Entity Prefab;
    public float Weight;
}

// Which entry of the weighted set this zombie was spawned from. Drives per-type pooling.
public struct ZombiePrefabIndex : IComponentData
{
    public int Value;
}

// Flat XP handed to the weapon that lands the killing blow (mirrors ZombieBase.xpBounty,
// minus the old proportional-by-damage split).
public struct ZombieXpBounty : IComponentData
{
    public float Value;
}

public struct ZombieDamageEvent
{
    public Entity Target;
    public int Amount;
    public int PlayerIndex;
    // Managed weapon reference is held in ZombieDamageBridge; 0 = no weapon.
    public int WeaponTicket;
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
    public int PlayerIndex;   // < 0 means "no credit, just release the weapon ticket"
    public bool IsKill;
    public int WeaponTicket;
    public float XpAmount;     // only meaningful when IsKill
    public float3 Position;    // zombie position at death, for kill-marker VFX
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

    // Crowd / stacking tuning (previously hardcoded in ZombieMovementSystem).
    public float SeparationRadius;      // how far a zombie pushes away from other zombies
    public float SeparationStrength;    // how hard that push fights the pull toward the player (0 = full overlap)
    public float StandFootprintRadius;  // how close to be "standing on" a neighbor
    public float ClimbHeightThreshold;  // neighbor's head must be this far above my chest to be worth climbing
    public float ForwardDotThreshold;   // 1 = only climb neighbors dead ahead; lower = climb off-angle neighbors too
    public float StandTolerance;        // how far above my feet a neighbor's top can be and still be steppable
}

// Inactive entity pool. Kept singleplayer-only (was previously alongside the net code).
public struct ZombiePoolSingleton : IComponentData
{
    public NativeList<Entity> Inactive;
}