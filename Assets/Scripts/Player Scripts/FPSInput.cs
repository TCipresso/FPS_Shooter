using UnityEngine;
using UnityEngine.InputSystem;

public class FPSInput : MonoBehaviour
{
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;
    public InputActionReference crouchAction;
    public InputActionReference aimAction;

    public float jumpBufferTime = 0.15f;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool CrouchHeld { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool AimHeld { get; private set; }
    public bool IsSprinting { get; set; }

    float jumpBufferCounter;
    public bool JumpBuffered => jumpBufferCounter > 0f;

    void OnEnable()
    {
        if (moveAction) moveAction.action.Enable();
        if (lookAction) lookAction.action.Enable();
        if (jumpAction) jumpAction.action.Enable();
        if (sprintAction) sprintAction.action.Enable();
        if (crouchAction) crouchAction.action.Enable();
        if (aimAction) aimAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction) moveAction.action.Disable();
        if (lookAction) lookAction.action.Disable();
        if (jumpAction) jumpAction.action.Disable();
        if (sprintAction) sprintAction.action.Disable();
        if (crouchAction) crouchAction.action.Disable();
        if (aimAction) aimAction.action.Disable();
    }

    void Update()
    {
        Move = moveAction ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Look = lookAction ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;

        Move = Vector2.ClampMagnitude(Move, 1f);

        JumpHeld = jumpAction && jumpAction.action.IsPressed();
        SprintHeld = sprintAction && sprintAction.action.IsPressed();
        CrouchHeld = crouchAction && crouchAction.action.IsPressed();
        CrouchPressed = crouchAction && crouchAction.action.WasPressedThisFrame();
        AimHeld = aimAction && aimAction.action.IsPressed();

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