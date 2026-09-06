using Unity.Entities;
using UnityEngine;

public class ZombieWallConfigAuthoring : MonoBehaviour
{
    [Header("Wall Climbing")]
    public LayerMask wallLayerMask;
    public float checkDistance = 0.6f;
    public float checkRadius = 0.4f;
    public float climbSpeed = 4f;
    public float ledgeLaunchSpeed = 6f;
    public float zombieClimbDistance = 2f;
    public float maxStackHeight = 8f;

    [Header("Ground")]
    public LayerMask groundLayerMask;
    public float groundCheckDistance = 15f;

    [Header("Crowd / Stacking")]
    [Tooltip("How far a zombie pushes away from other zombies (metres, horizontal).")]
    public float separationRadius = 3f;
    [Tooltip("How hard that push fights the pull toward the player. 2 = spread out, ~0.5 = crowd, 0 = full overlap / max stacking.")]
    public float separationStrength = 2f;
    [Tooltip("How close a zombie must be to a neighbour to stand on top of it.")]
    public float standFootprintRadius = 0.6f;
    [Tooltip("A neighbour's head must be this far above my chest before I bother climbing it.")]
    public float climbHeightThreshold = 0.3f;
    [Tooltip("1 = only climb neighbours directly ahead. Lower (~0.6-0.7) = climb neighbours off to the side too -> more piling.")]
    [Range(0f, 1f)] public float forwardDotThreshold = 0.95f;
    [Tooltip("How far above my feet a neighbour's top can be and still count as steppable footing.")]
    public float standTolerance = 0.3f;

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
                ZombieClimbDistance = authoring.zombieClimbDistance,
                MaxStackHeight = authoring.maxStackHeight,
                GroundLayerMask = authoring.groundLayerMask.value,
                GroundCheckDistance = authoring.groundCheckDistance,
                SeparationRadius = authoring.separationRadius,
                SeparationStrength = authoring.separationStrength,
                StandFootprintRadius = authoring.standFootprintRadius,
                ClimbHeightThreshold = authoring.climbHeightThreshold,
                ForwardDotThreshold = authoring.forwardDotThreshold,
                StandTolerance = authoring.standTolerance
            });
        }
    }
}
