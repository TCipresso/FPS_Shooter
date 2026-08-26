using UnityEngine;

public class LiquidSlosh : MonoBehaviour
{
    [Header("Wobble Settings")]
    public float maxWobble = 0.2f;
    public float wobbleSpeed = 4f;
    public float recoveryRate = 3f;
    public float velocityInfluence = 0.5f;

    private static readonly int WobbleXID = Shader.PropertyToID("_WobbleX");
    private static readonly int WobbleZID = Shader.PropertyToID("_WobbleZ");

    private MaterialPropertyBlock propertyBlock;
    private Renderer liquidRenderer;

    private Vector3 lastPosition;
    private Vector3 lastRotation;
    private Vector3 velocity;
    private Vector3 angularVelocity;
    private float wobbleX;
    private float wobbleZ;
    private float wobbleVelocityX;
    private float wobbleVelocityZ;

    void Awake()
    {
        liquidRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;
    }

    void Update()
    {
        // Calculate linear velocity
        Vector3 newVelocity = (transform.position - lastPosition) / Time.deltaTime;
        velocity = Vector3.Lerp(velocity, newVelocity, Time.deltaTime * 10f);
        lastPosition = transform.position;

        // Calculate angular velocity (rotation speed)
        Vector3 newAngularVelocity = (transform.eulerAngles - lastRotation) / Time.deltaTime;
        angularVelocity = Vector3.Lerp(angularVelocity, newAngularVelocity, Time.deltaTime * 10f);
        lastRotation = transform.eulerAngles;

        // Combine linear and angular velocity for wobble
        float targetWobbleX = (velocity.x + angularVelocity.z * 0.1f) * velocityInfluence;
        float targetWobbleZ = (velocity.z - angularVelocity.x * 0.1f) * velocityInfluence;

        // Spring-like wobble motion
        wobbleX = Mathf.SmoothDamp(wobbleX, targetWobbleX, ref wobbleVelocityX, 1f / wobbleSpeed);
        wobbleZ = Mathf.SmoothDamp(wobbleZ, targetWobbleZ, ref wobbleVelocityZ, 1f / wobbleSpeed);

        // Apply recovery (spring back to center)
        wobbleX = Mathf.Lerp(wobbleX, 0, Time.deltaTime * recoveryRate);
        wobbleZ = Mathf.Lerp(wobbleZ, 0, Time.deltaTime * recoveryRate);

        // Clamp wobble
        wobbleX = Mathf.Clamp(wobbleX, -maxWobble, maxWobble);
        wobbleZ = Mathf.Clamp(wobbleZ, -maxWobble, maxWobble);

        // Apply to shader
        liquidRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(WobbleXID, wobbleX);
        propertyBlock.SetFloat(WobbleZID, wobbleZ);
        liquidRenderer.SetPropertyBlock(propertyBlock);
    }
}