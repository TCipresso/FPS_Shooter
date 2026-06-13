using UnityEngine;
using UnityEngine.InputSystem;

public class FPSInput : MonoBehaviour
{
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;
    public InputActionReference crouchAction;
    public InputActionReference maneuverAction;

    public float jumpBufferTime = 0.15f;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool CrouchHeld { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool ManeuverPressed { get; private set; }

    float jumpBufferCounter;
    public bool JumpBuffered => jumpBufferCounter > 0f;

    void OnEnable()
    {
        if (moveAction) moveAction.action.Enable();
        if (lookAction) lookAction.action.Enable();
        if (jumpAction) jumpAction.action.Enable();
        if (crouchAction) crouchAction.action.Enable();
        if (maneuverAction) maneuverAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction) moveAction.action.Disable();
        if (lookAction) lookAction.action.Disable();
        if (jumpAction) jumpAction.action.Disable();
        if (crouchAction) crouchAction.action.Disable();
        if (maneuverAction) maneuverAction.action.Disable();
    }

    void Update()
    {
        Move = moveAction ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Look = lookAction ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
        Move = Vector2.ClampMagnitude(Move, 1f);

        JumpHeld = jumpAction && jumpAction.action.IsPressed();
        CrouchHeld = crouchAction && crouchAction.action.IsPressed();
        CrouchPressed = crouchAction && crouchAction.action.WasPressedThisFrame();
        ManeuverPressed = maneuverAction && maneuverAction.action.WasPressedThisFrame();

        if (jumpAction && jumpAction.action.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter < 0f)
            jumpBufferCounter = 0f;
    }

    public void ConsumeJump()
    {
        jumpBufferCounter = 0f;
    }
}