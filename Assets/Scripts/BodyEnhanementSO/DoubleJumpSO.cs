using UnityEngine;

[CreateAssetMenu(fileName = "DoubleJump", menuName = "Bloodsport/Enhancements/Double Jump")]
public class DoubleJumpEnhancementSO : BodyEnhancementSO
{
    [Header("Double Jump")]
    public int extraJumps = 1;

    public override bool IsPassive => true;

    public override void ApplyPassive(BodyEnhancementContext ctx)
    {
        ctx.FpsController.JumpCount = 1 + extraJumps;
    }

    public override void OnUnequip(BodyEnhancementContext ctx)
    {
        ctx.FpsController.JumpCount = 1;
    }
}