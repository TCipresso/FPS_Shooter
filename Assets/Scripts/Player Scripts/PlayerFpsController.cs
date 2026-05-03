using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerFpsController : MonoBehaviour
{
    [SerializeField] private FPSInput input;
    [SerializeField] private Transform orientation;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float airAcceleration = 6f;
    [SerializeField] private bool omniDirectionalSprint = false;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -28f;
    [SerializeField] private float groundedStickForce = -4f;
    [SerializeField] private float coyoteTime = 0.12f;

    public bool IsSprinting { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsSlideJumping { get; private set; }

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float coyoteCounter;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (input == null)
            input = GetComponent<FPSInput>();
        if (orientation == null)
            orientation = transform;

        verticalVelocity = groundedStickForce;
    }

    void Update()
    {
        if (input == null) return;

        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;

        if (grounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        bool moving = input.Move.sqrMagnitude > 0.01f;
        bool forwardEnough = input.Move.y > 0f;
        bool sprintAllowed = omniDirectionalSprint ? moving : forwardEnough;
        IsSprinting = input.SprintHeld && sprintAllowed;

        float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;

        Vector3 wishDirection = orientation.right * input.Move.x + orientation.forward * input.Move.y;
        wishDirection = Vector3.ClampMagnitude(wishDirection, 1f);
        Vector3 targetVelocity = wishDirection * targetSpeed;

        float accel = grounded ? acceleration : airAcceleration;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            accel * Time.deltaTime
        );

        if (input.JumpBuffered && coyoteCounter > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteCounter = 0f;
            input.ConsumeJump();
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        CollisionFlags flags = controller.Move(finalVelocity * Time.deltaTime);
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;
    }
}