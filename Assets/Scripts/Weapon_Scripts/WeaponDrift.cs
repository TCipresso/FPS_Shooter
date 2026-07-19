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
    [Header("Drift Limits")]
    public float maxLateral = 0.15f;
    public float maxBackward = 0.2f;
    private Vector3 _targetPosition;
    void Update()
    {
        if (fpsInput.AimHeld)
        {
            _targetPosition = Vector3.zero;
        }
        else
        {
            Vector3 localVelocity = transform.parent.InverseTransformDirection(characterController.velocity);
            float lateral = Mathf.Clamp(-localVelocity.x * lateralStrength, -maxLateral, maxLateral);
            float backward = localVelocity.z < 0 ? Mathf.Max(localVelocity.z * backwardStrength, -maxBackward) : 0f;
            _targetPosition = new Vector3(lateral, 0f, backward);
        }
        transform.localPosition = Vector3.Lerp(transform.localPosition, _targetPosition, Time.deltaTime * smoothSpeed);
    }
}