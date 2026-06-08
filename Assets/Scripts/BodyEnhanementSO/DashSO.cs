using UnityEngine;

[CreateAssetMenu(fileName = "Dash", menuName = "Bloodsport/Enhancements/Dash")]
public class DashSO : BodyEnhancementSO
{
    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.15f;

    public override bool IsPassive => false;

    private float dashTimer;

    public override void OnEquip(BodyEnhancementContext ctx)
    {
        dashTimer = 0f;
    }

    public override void OnUpdate(BodyEnhancementContext ctx, ref float cooldownTimer)
    {
        // tick dash duration
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            return;
        }

        if (cooldownTimer > 0f) return;
        if (!ctx.Input.ManeuverPressed) return;

        // get move input in world space
        Vector2 input = ctx.Input.Move;
        Vector3 dashDir;

        if (input.sqrMagnitude > 0.01f)
        {
            dashDir = ctx.Orientation.right * input.x + ctx.Orientation.forward * input.y;
            dashDir.y = 0f;
            dashDir.Normalize();
        }
        else
        {
            // no input — dash forward
            dashDir = ctx.Orientation.forward;
            dashDir.y = 0f;
            dashDir.Normalize();
        }

        // directly move via CharacterController for the dash duration
        ctx.CharacterController.Move(dashDir * dashSpeed * Time.deltaTime);

        dashTimer = dashDuration;
        cooldownTimer = cooldown;
    }
}