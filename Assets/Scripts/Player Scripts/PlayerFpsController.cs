using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerFpsController : MonoBehaviour
{
    [SerializeField] public FPSInput input;
    [SerializeField] private Transform orientation;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float airAcceleration = 6f;
    [SerializeField] private bool omniDirectionalSprint = false;

    [Header("Air Momentum")]
    [SerializeField] private float maxAirSpeed = 12f;
    [SerializeField] private bool conserveAirMomentum = true;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -28f;
    [SerializeField] private float groundedStickForce = -4f;
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpUpSpeed = 9f;
    [SerializeField] private float wallJumpAwaySpeed = 8f;
    [SerializeField] private float wallContactBuffer = 0.15f;
    [SerializeField] private float wallJumpCooldown = 0.25f;
    [SerializeField] private float wallMinAngle = 60f;
    [SerializeField] private float wallMaxAngle = 120f;

    public bool IsSprinting { get; set; }
    public bool IsSliding { get; private set; }
    public bool IsSlideJumping { get; private set; }
    public bool IsSprintingSuppressed => sprintSuppressTimer > 0f;
    public bool IsGrounded => controller != null && controller.isGrounded;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float coyoteCounter;
    private float sprintSuppressTimer;

    private bool onWall;
    private Vector3 wallNormal;
    private float wallContactTimer;
    private float wallJumpCooldownTimer;

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

        if (sprintSuppressTimer > 0f)
            sprintSuppressTimer -= Time.deltaTime;

        if (wallJumpCooldownTimer > 0f)
            wallJumpCooldownTimer -= Time.deltaTime;

        if (wallContactTimer > 0f)
            wallContactTimer -= Time.deltaTime;
        else
            onWall = false;

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
        IsSprinting = input.SprintHeld && sprintAllowed && !input.AimHeld && !IsSprintingSuppressed;

        float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;

        Vector3 wishDirection = orientation.right * input.Move.x + orientation.forward * input.Move.y;
        wishDirection = Vector3.ClampMagnitude(wishDirection, 1f);

        if (grounded)
        {
            // Ground: full control, accelerate to target velocity (including zero)
            Vector3 targetVelocity = wishDirection * targetSpeed;
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            // Air: preserve momentum. Only nudge toward wishDirection if input is held.
            if (wishDirection.sqrMagnitude > 0.01f)
            {
                // Player is steering — accelerate toward wish direction, but cap at maxAirSpeed
                Vector3 targetVelocity = wishDirection * Mathf.Max(targetSpeed, horizontalVelocity.magnitude);
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetVelocity,
                    airAcceleration * Time.deltaTime
                );

                // Clamp horizontal speed only if it exceeds the air ceiling
                if (horizontalVelocity.magnitude > maxAirSpeed)
                    horizontalVelocity = horizontalVelocity.normalized * maxAirSpeed;
            }
            else if (!conserveAirMomentum)
            {
                // Optional: slow drift to zero if momentum conservation is off
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, airAcceleration * 0.5f * Time.deltaTime);
            }
            // else: no input + conserveAirMomentum on = preserve velocity exactly
        }

        if (input.JumpBuffered)
        {
            if (!grounded && onWall && wallJumpCooldownTimer <= 0f)
            {
                horizontalVelocity = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized * wallJumpAwaySpeed;
                verticalVelocity = wallJumpUpSpeed;

                onWall = false;
                wallContactTimer = 0f;
                wallJumpCooldownTimer = wallJumpCooldown;
                input.ConsumeJump();
            }
            else if (coyoteCounter > 0f)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                coyoteCounter = 0f;
                input.ConsumeJump();
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        CollisionFlags flags = controller.Move(finalVelocity * Time.deltaTime);
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (controller.isGrounded) return;

        float angle = Vector3.Angle(Vector3.up, hit.normal);
        if (angle > wallMinAngle && angle < wallMaxAngle)
        {
            onWall = true;
            wallNormal = hit.normal;
            wallContactTimer = wallContactBuffer;
        }
    }

    public void SuppressSprintOnShoot(float duration)
    {
        if (duration > sprintSuppressTimer)
            sprintSuppressTimer = duration;
    }
}