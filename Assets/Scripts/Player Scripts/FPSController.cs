using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FPSController : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public FPSInput input;
    public FPSLook look;

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

    [Header("Slide")]
    public float slideBoostSpeed = 16f;
    public float slideFriction = 6f;
    public float slideMinSpeed = 2f;
    public float slideCapsuleHeight = 1f;
    public float slideCapsuleCenter = -0.25f;
    public float slideJumpBoost = 20f;
    public float slideJumpForce = 550f;
    [Range(0f, 1f)] public float slideJumpVertical = 0.6f;
    [Range(0f, 1f)] public float slideJumpHorizontal = 0.6f;

    public bool IsSprinting { get; private set; }
    public bool IsSliding { get; private set; }

    Rigidbody rb;
    CapsuleCollider col;

    bool grounded;
    bool readyToJump = true;
    bool slideJumped = false;
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
        if (input.CrouchPressed && IsSprinting && grounded && !IsSliding)
            StartSlide();

        if (IsSliding)
        {
            Vector3 horiz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (!input.CrouchHeld || horiz.magnitude < slideMinSpeed)
                EndSlide();
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void StartSlide()
    {
        IsSliding = true;

        col.height = slideCapsuleHeight;
        col.center = new Vector3(col.center.x, slideCapsuleCenter, col.center.z);

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

        col.height = defaultCapsuleHeight;
        col.center = new Vector3(col.center.x, defaultCapsuleCenterY, col.center.z);
    }

    void ApplyMovement()
    {
        if (input == null) return;

        Vector3 fwd = orientation ? orientation.forward : transform.forward;
        Vector3 side = orientation ? orientation.right : transform.right;

        bool canLook = look == null || look.CanLook;

        if (grounded) slideJumped = false;

        IsSprinting = input.SprintHeld && input.Move.sqrMagnitude > 0f && input.Move.y >= 0f && !IsSliding;

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
                rb.AddForce((Vector3.up * slideJumpForce * slideJumpVertical) + (slideDir * slideJumpForce * slideJumpHorizontal));
            }
            else
            {
                Vector3 v = rb.linearVelocity;
                v.y = 0f;
                rb.linearVelocity = v;
                rb.AddForce(Vector3.up * jumpForce);
            }
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
            if (Vector3.Angle(Vector3.up, normal) < maxSlopeAngle)
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
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