using Unity.Entities;
using UnityEngine;

public class ZombieWallConfigAuthoring : MonoBehaviour
{
    public LayerMask wallLayerMask;
    public float checkDistance = 0.6f;
    public float checkRadius = 0.4f;
    public float climbSpeed = 4f;
    public float ledgeLaunchSpeed = 6f;
    public float maxStackHeight = 8f;
    public LayerMask groundLayerMask;
    public float groundCheckDistance = 15f;

    class Baker : Baker<ZombieWallConfigAuthoring>
    {
        public override void Bake(ZombieWallConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ZombieWallConfig
            {
                WallLayerMask = authoring.wallLayerMask.value,
                CheckDistance = authoring.checkDistance,
                CheckRadius = authoring.checkRadius,
                ClimbSpeed = authoring.climbSpeed,
                LedgeLaunchSpeed = authoring.ledgeLaunchSpeed,
                MaxStackHeight = authoring.maxStackHeight,
                GroundLayerMask = authoring.groundLayerMask.value,
                GroundCheckDistance = authoring.groundCheckDistance
            });
        }
    }
}