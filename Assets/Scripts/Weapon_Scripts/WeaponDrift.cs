using UnityEngine;

public class WeaponDrift : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController;
    public FPSInput fpsInput;

    [Header("Drift Strength")]
    public float lateralStrength = 0.1f;
    public float backwardStrength = 0.1f;
    public float smoothSpeed = 5f;

    private Vector3 _targetPosition;

    void Update()
    {
        Vector3 localVelocity = transform.parent.InverseTransformDirection(characterController.velocity);
        float lateral = -localVelocity.x * lateralStrength;
        float backward = localVelocity.z < 0 ? localVelocity.z * backwardStrength : 0f;
        _targetPosition = new Vector3(lateral, 0f, backward);

        transform.localPosition = Vector3.Lerp(transform.localPosition, _targetPosition, Time.deltaTime * smoothSpeed);
    }
}