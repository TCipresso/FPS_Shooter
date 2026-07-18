using Unity.Entities;
using UnityEngine;

public class ZombieAuthoring : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public int maxHealth = 100;
    public float hitboxHeight = 1.8f;
    public float hitboxRadius = 0.5f;
    public float groundOffset = 0.9f;

    class Baker : Baker<ZombieAuthoring>
    {
        public override void Bake(ZombieAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ZombieTag>(entity);
            AddComponent(entity, new ZombieMoveSpeed { Value = authoring.moveSpeed });
            AddComponent(entity, new ZombieHealth { Current = authoring.maxHealth, Max = authoring.maxHealth });
            AddComponent(entity, new ZombieContactCooldown { Value = 0f });
            AddComponent(entity, new ZombieHitboxHeight { Value = authoring.hitboxHeight });
            AddComponent(entity, new ZombieHitboxRadius { Value = authoring.hitboxRadius });
            AddComponent(entity, new ZombieVerticalVelocity { Value = 0f });
            AddComponent(entity, new ZombieGroundOffset { Value = authoring.groundOffset });
            AddComponent(entity, new ZombieClimbState { WasBlocked = false, WasWallBlocked = false });
        }
    }
}