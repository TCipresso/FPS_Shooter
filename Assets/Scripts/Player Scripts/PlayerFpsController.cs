using UnityEngine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerFpsController : NetworkBehaviour
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
    public float VerticalVelocity => verticalVelocity;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -28f;
    [SerializeField] private float groundedStickForce = -4f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private int jumpCount = 1;

    [Header("Slide")]
    [SerializeField] private float slideBoostSpeed = 14f;
    [SerializeField] private float slideFriction = 5f;
    [SerializeField] private float slideMinSpeed = 3f;
    [SerializeField] private float slideCooldown = 0.5f;
    [SerializeField] private float slideHeight = 1f;
    [SerializeField] private float slideHeightLerpSpeed = 12f;
    [SerializeField] private float slideJumpUpSpeed = 8f;
    [SerializeField] private float slideJumpForwardBoost = 4f;
    [SerializeField] private float slideAirgraceTime = 0.2f;
    [SerializeField] private float slideDropForce = -14f;
    [SerializeField] private float slideGravityScale = 18f;
    [SerializeField] private float slideSlopeThreshold = 10f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 32f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashSteerStrength = 8f;
    [SerializeField] private int dashCharges = 1;

    [Header("Camera Slide Dip")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float slideCameraDrop = 0.8f;
    [SerializeField] private float cameraLerpSpeed = 12f;

    [Header("Downed")]
    [SerializeField] private float downedCameraDrop = 1.2f;
    public bool IsDowned { get; set; }

    [Header("Audio")]
    [SerializeField] private PlayerMovementAudio movementAudio;

    private Vector3 cameraDefaultLocalPos;

    public bool IsSprinting { get; set; }
    public bool IsSliding { get; private set; }
    public bool IsSlideJumping { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsSprintingSuppressed => sprintSuppressTimer > 0f || (IsSlideJumping && !IsGrounded);
    public bool IsGrounded => controller != null && controller.isGrounded;

    public int JumpCount
    {
        get => jumpCount;
        set
        {
            jumpCount = Mathf.Max(1, value);
            jumpsRemaining = jumpCount;
        }
    }

    public float DashSpeedMultiplier { get; set; } = 1f;
    public float DashCooldownMultiplier { get; set; } = 1f;
    public int DashCharges { get; set; } = 1;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float coyoteCounter;
    private float sprintSuppressTimer;

    private float slideCooldownTimer;
    private float slideAirgraceTimer;
    private float slideGroundedBuffer;
    private float defaultHeight;
    private Vector3 defaultCenter;

    private Vector3 groundNormal = Vector3.up;
    private bool hasGroundNormal = false;

    private int jumpsRemaining;

    private float dashTimer;
    private float dashCooldownTimer;
    private int dashChargesRemaining;
    private Vector3 dashDirection;
    private Vector3 preDashHorizontalVelocity;
    private float preDashVerticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (input == null)
            input = GetComponent<FPSInput>();

        if (orientation == null)
            orientation = transform;

        if (cameraHolder != null)
            cameraDefaultLocalPos = cameraHolder.localPosition;

        verticalVelocity = groundedStickForce;
        defaultHeight = controller.height;
        defaultCenter = controller.center;

        jumpsRemaining = jumpCount;
        DashCharges = dashCharges;
        dashChargesRemaining = DashCharges;
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
            controller.enabled = false;
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (input == null) return;

        if (IsDowned)
        {
            horizontalVelocity = Vector3.zero;

            if (controller.isGrounded)
                verticalVelocity = groundedStickForce;
            else
                verticalVelocity += gravity * Time.deltaTime;

            controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            UpdateCameraSlideDip();
            return;
        }

        TickTimers();

        bool grounded = controller.isGrounded;

        if (grounded && verticalVelocity < 0f && !IsSlideJumping)
            verticalVelocity = groundedStickForce;

        if (grounded)
        {
            coyoteCounter = coyoteTime;
            IsSlideJumping = false;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        if (!grounded)
        {
            groundNormal = Vector3.up;
            hasGroundNormal = false;
        }

        UpdateSprintState();
        HandleDash();

        if (!IsDashing)
            UpdateSlideState(grounded);

        UpdateCapsuleHeight();
        ApplyHorizontalMovement(grounded);
        HandleJump(grounded);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity;

        if (IsSliding && hasGroundNormal)
        {
            Vector3 slopeMoveDir = Vector3.ProjectOnPlane(horizontalVelocity, groundNormal);
            finalVelocity = slopeMoveDir;
            finalVelocity += -groundNormal * 8f;
        }
        else
        {
            finalVelocity = horizontalVelocity;
            finalVelocity.y = verticalVelocity;
        }

        UpdateCameraSlideDip();

        CollisionFlags flags = controller.Move(finalVelocity * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;
    }

    void TickTimers()
    {
        if (sprintSuppressTimer > 0f) sprintSuppressTimer -= Time.deltaTime;
        if (slideCooldownTimer > 0f) slideCooldownTimer -= Time.deltaTime;
        if (slideGroundedBuffer > 0f) slideGroundedBuffer -= Time.deltaTime;

        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                IsDashing = false;
                horizontalVelocity = preDashHorizontalVelocity;
            }
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
            {
                dashChargesRemaining = Mathf.Min(dashChargesRemaining + 1, DashCharges);
                if (dashChargesRemaining < DashCharges)
                    dashCooldownTimer = dashCooldown * DashCooldownMultiplier;
            }
        }
    }

    void HandleDash()
    {
        if (input.ManeuverPressed && dashChargesRemaining > 0)
        {
            if (IsSliding) EndSlide();

            preDashHorizontalVelocity = horizontalVelocity;
            preDashVerticalVelocity = verticalVelocity;

            Vector2 moveInput = input.Move;
            if (moveInput.sqrMagnitude > 0.01f)
            {
                dashDirection = orientation.right * moveInput.x + orientation.forward * moveInput.y;
                dashDirection.y = 0f;
                dashDirection.Normalize();
            }
            else
            {
                dashDirection = orientation.forward;
                dashDirection.y = 0f;
                dashDirection.Normalize();
            }

            horizontalVelocity = dashDirection * dashSpeed * DashSpeedMultiplier;
            verticalVelocity = 0f;

            movementAudio?.PlayDash();
            IsDashing = true;
            dashTimer = dashDuration;
            dashChargesRemaining--;
            dashCooldownTimer = dashCooldown * DashCooldownMultiplier;
        }
    }

    void UpdateSprintState()
    {
        bool moving = input.Move.sqrMagnitude > 0.01f;
        bool forwardEnough = input.Move.y > 0f;
        bool sprintAllowed = omniDirectionalSprint ? moving : forwardEnough;

        IsSprinting = input.SprintHeld &&
                      sprintAllowed &&
                      !input.AimHeld &&
                      !IsSprintingSuppressed &&
                      !IsSliding;
    }

    void UpdateSlideState(bool grounded)
    {
        if (grounded)
            slideGroundedBuffer = 0.15f;

        bool canInitiate = slideGroundedBuffer > 0f;

        if (!IsSliding && input.CrouchPressed && IsSprinting && canInitiate && slideCooldownTimer <= 0f)
            StartSlide();

        if (IsSliding)
        {
            if (grounded)
                slideAirgraceTimer = slideAirgraceTime;
            else
                slideAirgraceTimer -= Time.deltaTime;

            float horizSpeed = horizontalVelocity.magnitude;
            bool goingDownhill = hasGroundNormal && IsDownhill();
            bool tooSlow = horizSpeed < slideMinSpeed && !goingDownhill;

            if (!input.CrouchHeld)
                EndSlide();
            else if (tooSlow)
                EndSlide();
            else if (slideAirgraceTimer <= 0f)
                EndSlide();
        }
    }

    bool IsDownhill()
    {
        if (!hasGroundNormal) return false;

        float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
        if (slopeAngle < slideSlopeThreshold) return false;

        Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
        return Vector3.Dot(horizontalVelocity.normalized, slopeDir) > 0f;
    }

    void StartSlide()
    {
        IsSliding = true;
        slideAirgraceTimer = slideAirgraceTime;
        verticalVelocity = slideDropForce;

        Vector3 fwd = orientation.forward;
        Vector3 side = orientation.right;
        Vector2 m = input.Move;

        Vector3 slideDir = fwd * m.y + side * m.x;

        if (slideDir.sqrMagnitude < 0.01f)
            slideDir = fwd;

        slideDir.Normalize();

        float currentSpeed = horizontalVelocity.magnitude;
        float finalSlideSpeed = Mathf.Max(slideBoostSpeed, currentSpeed);

        horizontalVelocity = slideDir * finalSlideSpeed;

        movementAudio?.PlaySlide();
    }

    void EndSlide()
    {
        IsSliding = false;
        slideCooldownTimer = slideCooldown;
        movementAudio?.StopSlide();
    }

    void UpdateCapsuleHeight()
    {
        float targetHeight = IsSliding ? slideHeight : defaultHeight;

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            slideHeightLerpSpeed * Time.deltaTime
        );

        float heightDelta = (defaultHeight - controller.height) * 0.5f;

        controller.center = new Vector3(
            defaultCenter.x,
            defaultCenter.y - heightDelta,
            defaultCenter.z
        );
    }

    void UpdateCameraSlideDip()
    {
        if (cameraHolder == null) return;

        Vector3 targetPos = cameraDefaultLocalPos;

        if (IsDowned)
            targetPos.y -= downedCameraDrop;
        else if (IsSliding)
            targetPos.y -= slideCameraDrop;

        cameraHolder.localPosition = Vector3.Lerp(
            cameraHolder.localPosition,
            targetPos,
            cameraLerpSpeed * Time.deltaTime
        );
    }

    void ApplyHorizontalMovement(bool grounded)
    {
        if (IsDashing)
        {
            verticalVelocity = 0f;

            Vector2 moveInput = input.Move;
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 wishDir = orientation.right * moveInput.x + orientation.forward * moveInput.y;
                wishDir.y = 0f;
                wishDir.Normalize();
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    wishDir * dashSpeed * DashSpeedMultiplier,
                    dashSteerStrength * Time.deltaTime
                );
            }
            return;
        }

        if (IsSliding)
        {
            if (hasGroundNormal)
                verticalVelocity = groundedStickForce;

            if (hasGroundNormal)
            {
                float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
                if (slopeAngle >= slideSlopeThreshold)
                {
                    Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                    horizontalVelocity += slopeDir * slideGravityScale * Time.deltaTime;
                }
            }

            Vector3 frictionDelta = horizontalVelocity.normalized * slideFriction * Time.deltaTime;
            if (frictionDelta.magnitude < horizontalVelocity.magnitude)
                horizontalVelocity -= frictionDelta;
            else
                horizontalVelocity = Vector3.zero;

            return;
        }

        Vector3 wishDirection = orientation.right * input.Move.x + orientation.forward * input.Move.y;
        wishDirection = Vector3.ClampMagnitude(wishDirection, 1f);

        float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;

        if (grounded)
        {
            Vector3 targetVelocity = wishDirection * targetSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            if (wishDirection.sqrMagnitude > 0.01f)
            {
                Vector3 targetVelocity = wishDirection * Mathf.Max(targetSpeed, horizontalVelocity.magnitude);
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, airAcceleration * Time.deltaTime);

                if (horizontalVelocity.magnitude > maxAirSpeed && !IsSlideJumping)
                    horizontalVelocity = horizontalVelocity.normalized * maxAirSpeed;
            }
            else if (!conserveAirMomentum)
            {
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, airAcceleration * 0.5f * Time.deltaTime);
            }
        }
    }

    void HandleJump(bool grounded)
    {
        if (!input.JumpBuffered) return;

        if (IsSliding && grounded)
        {
            Vector3 slideDir = horizontalVelocity.normalized;
            horizontalVelocity += slideDir * slideJumpForwardBoost;
            verticalVelocity = slideJumpUpSpeed;

            IsSlideJumping = true;
            EndSlide();
            movementAudio?.PlayJump();
            input.ConsumeJump();
            return;
        }

        if (coyoteCounter > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteCounter = 0f;
            jumpsRemaining = Mathf.Max(0, jumpsRemaining - 1);
            movementAudio?.PlayJump();
            input.ConsumeJump();
            return;
        }

        if (jumpsRemaining > 0)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpsRemaining--;
            movementAudio?.PlayJump();
            input.ConsumeJump();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!isLocalPlayer) return;

        float angle = Vector3.Angle(Vector3.up, hit.normal);

        if (angle < 60f)
        {
            groundNormal = hit.normal;
            hasGroundNormal = true;
            jumpsRemaining = jumpCount;
        }
    }

    public void SuppressSprintOnShoot(float duration)
    {
        if (duration > sprintSuppressTimer)
            sprintSuppressTimer = duration;
    }

    public void SetHorizontalVelocity(Vector3 velocity)
    {
        horizontalVelocity = velocity;
    }
}