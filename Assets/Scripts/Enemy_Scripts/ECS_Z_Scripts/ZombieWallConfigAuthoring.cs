using Unity.Entities;
using UnityEngine;

public class ZombieWallConfigAuthoring : MonoBehaviour
{
    public LayerMask wallLayerMask;
    public float checkDistance = 0.6f;
    public float climbSpeed = 4f;
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
                ClimbSpeed = authoring.climbSpeed,
                GroundLayerMask = authoring.groundLayerMask.value,
                GroundCheckDistance = authoring.groundCheckDistance
            });
        }
    }
}