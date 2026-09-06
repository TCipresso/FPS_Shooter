using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ZombieAuthoring : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public int maxHealth = 100;
    public int attackDamage = 25;
    public float xpBounty = 20f;
    public float hitboxHeight = 1.8f;
    public float hitboxRadius = 0.5f;
    public float groundOffset = 0.9f;

    [Header("Spawn Emergence")]
    [Tooltip("How far below the ground the zombie starts when it spawns. 0 = no emergence, spawns on the surface.")]
    public float emergeRiseDistance = 2f;
    [Tooltip("How fast it claws up out of the ground, m/s.")]
    public float emergeRiseSpeed = 3f;

    class Baker : Baker<ZombieAuthoring>
    {
        public override void Bake(ZombieAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ZombieTag>(entity);
            AddComponent(entity, new ZombieMoveSpeed { Value = authoring.moveSpeed });
            AddComponent(entity, new ZombieBaseStats
            {
                BaseMoveSpeed = authoring.moveSpeed,
                BaseMaxHealth = authoring.maxHealth,
                BaseContactDamage = authoring.attackDamage
            });
            AddComponent(entity, new ZombieHealth { Current = authoring.maxHealth, Max = authoring.maxHealth });
            AddComponent(entity, new ZombieContactDamage { Value = authoring.attackDamage });
            AddComponent(entity, new ZombieXpBounty { Value = authoring.xpBounty });
            AddComponent(entity, new ZombieContactCooldown { Value = 0f });
            AddComponent(entity, new ZombieHitboxHeight { Value = authoring.hitboxHeight });
            AddComponent(entity, new ZombieHitboxRadius { Value = authoring.hitboxRadius });
            AddComponent(entity, new ZombieVerticalVelocity { Value = 0f });
            AddComponent(entity, new ZombieGroundOffset { Value = authoring.groundOffset });
            AddComponent(entity, new ZombieClimbState { WasBlocked = false, WasWallBlocked = false });
            AddComponent(entity, new ZombieTarget { Index = -1, Position = float3.zero, HasTarget = false, RecheckTimer = 0f });
            // Assigned by the spawner per weighted entry; baked default is entry 0.
            AddComponent(entity, new ZombiePrefabIndex { Value = 0 });
            AddComponent(entity, new ZombieEmerge
            {
                RemainingRise = 0f,
                RiseDistance = authoring.emergeRiseDistance,
                Speed = authoring.emergeRiseSpeed
            });
        }
    }
}
