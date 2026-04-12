using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FPSController : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public FPSInput input;
    public FPSLook look;
    public PlayerMovementAudio movementAudio;

    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float jumpForce = 550f;
    public float gravity = 20f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float maxSlopeAngle = 35f;

    [Header("Acceleration")]
    public float groundAcceleration = 40f;
    public float airAcceleration = 10f;
    [Range(0f, 1f)] public float airControl = 0.4f;
    public float maxAirSpeed = 12f;
    public bool conserveHorizontalMomentum = true;

    [Header("Jump")]
    public float jumpCooldown = 0.05f;
    public bool autoBHop = true;

    [Header("Wall Jump")]
    public float wallJumpUpForce = 400f;
    public float wallJumpAwayForce = 300f;
    public float wallJumpWindow = 0.3f;
    public float wallJumpCooldown = 0.5f;

    [Header("Slide")]
    public float slideBoostSpeed = 16f;
    public float slideFriction = 6f;
    public float slideMinSpeed = 2f;
    public float slideCapsuleHeight = 1f;
    public float slideCapsuleCenter = -0.25f;
    public float slideCrouchSpeed = 10f;
    public float slideCooldown = 1f;
    public float slideJumpBoost = 20f;
    public float slideJumpForce = 550f;
    [Range(0f, 1f)] public float slideJumpVertical = 0.6f;
    [Range(0f, 1f)] public float slideJumpHorizontal = 0.6f;

    public bool IsSprinting { get; set; }
    public bool IsSliding { get; private set; }
    public bool IsSprintingSuppressed { get; private set; }
    public bool IsGrounded => grounded;

    Rigidbody rb;
    CapsuleCollider col;

    bool grounded;
    bool suppressSprint = false;
    bool onWall;
    Vector3 wallNormal = Vector3.zero;
    float wallContactTimer = 0f;
    float wallJumpCooldownTimer = 0f;
    bool readyToJump = true;
    bool slideJumped = false;
    float slideCooldownTimer = 0f;
    float sprintSuppressTimer = 0f;
    Vector3 normalVector = Vector3.up;
    bool cancellingGrounded;

    float defaultCapsuleHeight;
    float defaultCapsuleCenterY;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        defaultCapsuleHeight = col.height;
        defaultCapsuleCenterY = col.center.y;
    }

    void Update()
    {
        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.deltaTime;

        if (sprintSuppressTimer > 0f)
            sprintSuppressTimer -= Time.deltaTime;

        IsSprintingSuppressed = sprintSuppressTimer > 0f || suppressSprint;

        if (wallJumpCooldownTimer > 0f)
            wallJumpCooldownTimer -= Time.deltaTime;

        // Decay wall contact window each frame; OnCollisionStay refreshes it
        if (wallContactTimer > 0f)
            wallContactTimer -= Time.deltaTime;
        else
            onWall = false;

        if (input.CrouchPressed && IsSprinting && grounded && !IsSliding && slideCooldownTimer <= 0f)
            StartSlide();

        if (IsSliding)
        {
            Vector3 horiz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (!input.CrouchHeld || horiz.magnitude < slideMinSpeed)
                EndSlide();
        }

        float targetCenterY = IsSliding ? slideCapsuleCenter : defaultCapsuleCenterY;
        col.center = new Vector3(
            col.center.x,
            Mathf.Lerp(col.center.y, targetCenterY, slideCrouchSpeed * Time.deltaTime),
            col.center.z
        );
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void StartSlide()
    {
        IsSliding = true;
        if (movementAudio != null) movementAudio.PlaySlide();

        col.height = slideCapsuleHeight;

        Vector3 fwd = orientation ? orientation.forward : transform.forward;
        Vector3 side = orientation ? orientation.right : transform.right;

        Vector2 move = input.Move;
        move.y = Mathf.Max(0f, move.y);

        Vector3 slideDir = (fwd * move.y + side * move.x).normalized;
        if (slideDir == Vector3.zero) slideDir = fwd;

        rb.linearVelocity = new Vector3(slideDir.x * slideBoostSpeed, rb.linearVelocity.y, slideDir.z * slideBoostSpeed);
    }

    void EndSlide()
    {
        IsSliding = false;
        slideCooldownTimer = slideCooldown;
        col.height = defaultCapsuleHeight;
        if (movementAudio != null) movementAudio.StopSlide();
    }

    void ApplyMovement()
    {
        if (input == null) return;

        Vector3 fwd = orientation ? orientation.forward : transform.forward;
        Vector3 side = orientation ? orientation.right : transform.right;

        bool canLook = look == null || look.CanLook;

        if (grounded && !slideJumped) suppressSprint = false;
        if (grounded) slideJumped = false;

        IsSprinting = !suppressSprint && input.SprintHeld && input.Move.sqrMagnitude > 0f && input.Move.y >= 0f && !IsSliding && !input.AimHeld;

        Vector3 v0 = rb.linearVelocity;
        Vector3 horiz0 = new Vector3(v0.x, 0f, v0.z);

        bool wantsJump = autoBHop ? input.JumpHeld || input.JumpBuffered : input.JumpBuffered;
        if (readyToJump && wantsJump && grounded)
        {
            bool wasSliding = IsSliding;
            Vector3 slideVelocity = rb.linearVelocity;
            EndSlide();
            readyToJump = false;

            if (wasSliding)
            {
                Vector3 slideDir = new Vector3(slideVelocity.x, 0f, slideVelocity.z).normalized;
                rb.linearVelocity = new Vector3(slideDir.x * slideJumpBoost, 0f, slideDir.z * slideJumpBoost);
                slideJumped = true;
                if (movementAudio != null) movementAudio.PlayJump();
                sprintSuppressTimer = 0.8f;
                rb.AddForce((Vector3.up * slideJumpForce * slideJumpVertical) + (slideDir * slideJumpForce * slideJumpHorizontal));
                suppressSprint = true;
            }
            else
            {
                Vector3 v = rb.linearVelocity;
                v.y = 0f;
                rb.linearVelocity = v;
                rb.AddForce(Vector3.up * jumpForce);
                if (movementAudio != null) movementAudio.PlayJump();
            }
            input.ConsumeJump();
            Invoke(nameof(ResetJump), jumpCooldown);
            return;
        }

        // Wall jump
        bool wantsWallJump = autoBHop ? input.JumpHeld || input.JumpBuffered : input.JumpBuffered;
        if (readyToJump && wantsWallJump && !grounded && onWall && wallJumpCooldownTimer <= 0f)
        {
            readyToJump = false;
            onWall = false;
            wallContactTimer = 0f;

            // Launch up + away from wall surface
            Vector3 jumpDir = (Vector3.up * wallJumpUpForce) + (wallNormal * wallJumpAwayForce);
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
            rb.AddForce(jumpDir, ForceMode.Impulse);

            wallJumpCooldownTimer = wallJumpCooldown;
            suppressSprint = true;
            if (movementAudio != null) movementAudio.PlayJump();
            input.ConsumeJump();
            Invoke(nameof(ResetJump), jumpCooldown);
            return;
        }

        if (IsSliding && !slideJumped)
        {
            horiz0 = Vector3.MoveTowards(horiz0, Vector3.zero, slideFriction * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(horiz0.x, v0.y, horiz0.z);
            return;
        }

        float currentMaxSpeed = canLook ? (IsSprinting ? sprintSpeed : walkSpeed) : 0f;
        Vector3 wishDir = (fwd * input.Move.y) + (side * input.Move.x);

        if (grounded && !slideJumped)
        {
            Vector3 target = wishDir * currentMaxSpeed;
            float ax = groundAcceleration * Time.fixedDeltaTime;
            horiz0 = new Vector3(
                Mathf.MoveTowards(horiz0.x, target.x, ax),
                0f,
                Mathf.MoveTowards(horiz0.z, target.z, ax)
            );
        }
        else
        {
            if (slideJumped)
            {
                // Don't touch horizontal velocity — preserve full slide jump speed
            }
            else if (wishDir.sqrMagnitude > 0f)
            {
                Vector3 target = wishDir * Mathf.Min(maxAirSpeed, currentMaxSpeed);
                float ax = airAcceleration * airControl * Time.fixedDeltaTime;
                horiz0 = Vector3.MoveTowards(horiz0, target, ax);
            }
            else if (!conserveHorizontalMomentum)
            {
                float ax = airAcceleration * 0.3f * Time.fixedDeltaTime;
                horiz0 = Vector3.MoveTowards(horiz0, Vector3.zero, ax);
            }
        }

        rb.linearVelocity = new Vector3(horiz0.x, v0.y, horiz0.z);
    }

    void ResetJump() => readyToJump = true;

    void OnCollisionStay(Collision other)
    {
        int layer = other.gameObject.layer;
        if ((groundLayer.value & (1 << layer)) == 0) return;

        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            float angle = Vector3.Angle(Vector3.up, normal);

            if (angle < maxSlopeAngle)
            {
                // Ground contact
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
            else if (angle > 60f && angle < 120f && !grounded)
            {
                // Wall contact — only register when airborne
                onWall = true;
                wallNormal = normal;
                wallContactTimer = wallJumpWindow;
            }
        }

        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    void StopGrounded() => grounded = false;
}