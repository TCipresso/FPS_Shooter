using UnityEngine;
using UnityEngine.InputSystem;

public class FPSInput : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool JumpPressed { get; private set; }

    void OnEnable()
    {
        if (moveAction) moveAction.action.Enable();
        if (lookAction) lookAction.action.Enable();
        if (jumpAction) jumpAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction) moveAction.action.Disable();
        if (lookAction) lookAction.action.Disable();
        if (jumpAction) jumpAction.action.Disable();
    }

    void Update()
    {
        Move = moveAction ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Look = lookAction ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;

        if (jumpAction && jumpAction.action.WasPressedThisFrame())
            JumpPressed = true;
    }

    public void ConsumeJump()
    {
        JumpPressed = false;
    }
}