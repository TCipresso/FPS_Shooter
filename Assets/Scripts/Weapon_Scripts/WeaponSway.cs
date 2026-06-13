using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("References")]
    public FPSInput input;

    [Header("Position Sway")]
    public float positionSwayAmount = 0.02f;
    public float maxPositionSway = 0.06f;

    [Header("Rotation Sway")]
    public float rotationSwayAmount = 4f;
    public float maxRotationSway = 6f;

    [Header("Smoothing")]
    public float positionSmooth = 10f;
    public float rotationSmooth = 12f;

    Vector3 initialLocalPosition;
    Quaternion initialLocalRotation;

    void Start()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        if (input == null) return;

        Vector2 look = input.Look;

        float swayX = Mathf.Clamp(-look.x * positionSwayAmount, -maxPositionSway, maxPositionSway);
        float swayY = Mathf.Clamp(-look.y * positionSwayAmount, -maxPositionSway, maxPositionSway);
        Vector3 targetPosition = initialLocalPosition + new Vector3(swayX, swayY, 0f);

        float rotX = Mathf.Clamp(-look.y * rotationSwayAmount, -maxRotationSway, maxRotationSway);
        float rotY = Mathf.Clamp(look.x * rotationSwayAmount, -maxRotationSway, maxRotationSway);
        float rotZ = Mathf.Clamp(look.x * rotationSwayAmount, -maxRotationSway, maxRotationSway);
        Quaternion targetRotation = initialLocalRotation * Quaternion.Euler(rotX, rotY, rotZ);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, positionSmooth * Time.deltaTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, rotationSmooth * Time.deltaTime);
    }
}