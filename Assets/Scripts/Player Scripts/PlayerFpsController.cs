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

    [Header("Camera Slide Dip")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float slideCameraDrop = 0.8f;
    [SerializeField] private float cameraLerpSpeed = 12f;

    private Vector3 cameraDefaultLocalPos;

    public bool IsSprinting { get; set; }
    public bool IsSliding { get; private set; }
    public bool IsSlideJumping { get; private set; }
    public bool IsSprintingSuppressed => sprintSuppressTimer > 0f || (IsSlideJumping && !IsGrounded);
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

    private float slideCooldownTimer;
    private float slideAirgraceTimer;
    private float defaultHeight;
    private Vector3 defaultCenter;

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
    }

    void Update()
    {
        if (input == null) return;

        TickTimers();
        UpdateWallContact();

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

        UpdateSprintState();
        UpdateSlideState(grounded);
        UpdateCapsuleHeight();

        ApplyHorizontalMovement(grounded);
        HandleJump(grounded);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        UpdateCameraSlideDip();

        CollisionFlags flags = controller.Move(finalVelocity * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;
    }

    void TickTimers()
    {
        if (sprintSuppressTimer > 0f) sprintSuppressTimer -= Time.deltaTime;
        if (wallJumpCooldownTimer > 0f) wallJumpCooldownTimer -= Time.deltaTime;
        if (slideCooldownTimer > 0f) slideCooldownTimer -= Time.deltaTime;
    }

    void UpdateWallContact()
    {
        if (wallContactTimer > 0f)
            wallContactTimer -= Time.deltaTime;
        else
            onWall = false;
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
        if (!IsSliding && input.CrouchPressed && IsSprinting && grounded && slideCooldownTimer <= 0f)
        {
            StartSlide();
        }

        if (IsSliding)
        {
            if (grounded)
                slideAirgraceTimer = slideAirgraceTime;
            else
                slideAirgraceTimer -= Time.deltaTime;

            float horizSpeed = horizontalVelocity.magnitude;

            if (!input.CrouchHeld)
            {
                EndSlide();
            }
            else if (horizSpeed < slideMinSpeed)
            {
                EndSlide();
            }
            else if (slideAirgraceTimer <= 0f)
            {
                EndSlide();
            }
        }
    }

    void StartSlide()
    {
        IsSliding = true;
        slideAirgraceTimer = slideAirgraceTime;
        verticalVelocity = slideDropForce;

        Vector3 fwd = orientation.forward;
        Vector3 side = orientation.right;
        Vector2 m = input.Move;
        m.y = Mathf.Max(0f, m.y);

        Vector3 slideDir = fwd * m.y + side * m.x;

        if (slideDir.sqrMagnitude < 0.01f)
            slideDir = fwd;

        slideDir.Normalize();

        float currentSpeed = horizontalVelocity.magnitude;
        float finalSlideSpeed = Mathf.Max(slideBoostSpeed, currentSpeed);

        horizontalVelocity = slideDir * finalSlideSpeed;
    }

    void EndSlide()
    {
        IsSliding = false;
        slideCooldownTimer = slideCooldown;
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

        if (IsSliding)
            targetPos.y -= slideCameraDrop;

        cameraHolder.localPosition = Vector3.Lerp(
            cameraHolder.localPosition,
            targetPos,
            cameraLerpSpeed * Time.deltaTime
        );
    }

    void ApplyHorizontalMovement(bool grounded)
    {
        if (IsSliding)
        {
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                Vector3.zero,
                slideFriction * Time.deltaTime
            );
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
            input.ConsumeJump();
            return;
        }

        if (!grounded && onWall && wallJumpCooldownTimer <= 0f)
        {
            horizontalVelocity = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized * wallJumpAwaySpeed;
            verticalVelocity = wallJumpUpSpeed;

            onWall = false;
            wallContactTimer = 0f;
            wallJumpCooldownTimer = wallJumpCooldown;
            input.ConsumeJump();
            return;
        }

        if (coyoteCounter > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteCounter = 0f;
            input.ConsumeJump();
        }
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