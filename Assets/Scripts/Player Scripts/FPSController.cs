using System;
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

    Rigidbody rb;

    bool grounded;
    bool readyToJump = true;
    float jumpCooldown = 0.25f;
    Vector3 normalVector = Vector3.up;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void ApplyMovement()
    {
        if (input == null) return;

        Vector3 fwd = orientation ? orientation.forward : transform.forward;
        Vector3 side = orientation ? orientation.right : transform.right;

        bool canLook = look == null || look.CanLook;

        float currentMaxSpeed = canLook ? walkSpeed : 0f;

        Vector3 wishDir = (fwd * input.Move.y) + (side * input.Move.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        Vector3 v0 = rb.linearVelocity;
        Vector3 horiz0 = new Vector3(v0.x, 0f, v0.z);

        if (grounded)
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
            if (wishDir.sqrMagnitude > 0f)
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

        if (readyToJump && input.JumpPressed && grounded)
        {
            readyToJump = false;

            Vector3 v = rb.linearVelocity;
            if (v.y < 0.5f) v.y = 0f;
            rb.linearVelocity = v;

            rb.AddForce(Vector3.up * jumpForce);

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        input.ConsumeJump();
    }

    void ResetJump() => readyToJump = true;

    bool cancellingGrounded;

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