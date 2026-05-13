using UnityEngine;

public class AirHolder : MonoBehaviour
{
    [Header("References")]
    public PlayerFpsController fpsController;

    [Header("Air Raise")]
    public float maxRaise = 0.08f;
    public float airRaiseSpeed = 4f;
    public float maxAirTime = 1.5f;

    [Header("Landing Impact")]
    public float impactDip = 0.12f;
    public float impactSpeed = 20f;
    public float returnSpeed = 8f;
    public float velocityNormalizer = 20f;

    float airTime = 0f;
    float currentY = 0f;
    float targetY = 0f;
    bool wasGrounded = true;
    bool isRecovering = false;

    void Update()
    {
        bool grounded = fpsController.IsGrounded;

        if (!grounded)
        {
            airTime += Time.deltaTime;
            isRecovering = false;
        }

        if (!wasGrounded && grounded)
        {
            float impactForce = Mathf.Clamp01(-fpsController.VerticalVelocity / velocityNormalizer);
            targetY = -impactDip * impactForce;
            isRecovering = true;
            airTime = 0f;
        }

        if (!grounded && !isRecovering)
        {
            float t = Mathf.Clamp01(airTime / maxAirTime);
            targetY = Mathf.Lerp(0f, maxRaise, t);
        }

        if (grounded && !isRecovering)
        {
            targetY = 0f;
            airTime = 0f;
        }

        if (isRecovering)
        {
            currentY = Mathf.Lerp(currentY, targetY, impactSpeed * Time.deltaTime);
            if (Mathf.Abs(currentY - targetY) < 0.001f)
            {
                targetY = 0f;
                isRecovering = false;
            }
        }
        else
        {
            currentY = Mathf.Lerp(currentY, targetY, airRaiseSpeed * Time.deltaTime);
        }

        transform.localPosition = new Vector3(
            transform.localPosition.x,
            currentY,
            transform.localPosition.z
        );

        wasGrounded = grounded;
    }
}