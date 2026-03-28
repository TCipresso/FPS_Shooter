using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("Rotation Recoil")]
    public float kickRotationZ = 5f;

    [Header("Position Recoil")]
    public float kickPositionZ = -0.1f;
    public float kickPositionY = 0.05f;
    public float kickPositionX = 0.02f;

    [Header("Feel")]
    public float snapSpeed = 20f;
    public float returnSpeed = 8f;

    Vector3 originalLocalPosition;
    Quaternion originalLocalRotation;

    Vector3 targetPosition;
    Quaternion targetRotation;

    void Start()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        targetPosition = originalLocalPosition;
        targetRotation = originalLocalRotation;
    }

    void Update()
    {
        // Snap toward target
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, snapSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, snapSpeed * Time.deltaTime);

        // Decay target back to original
        targetPosition = Vector3.Lerp(targetPosition, originalLocalPosition, returnSpeed * Time.deltaTime);
        targetRotation = Quaternion.Lerp(targetRotation, originalLocalRotation, returnSpeed * Time.deltaTime);
    }

    public void Kick()
    {
        // Add kick to target position and rotation
        targetPosition += new Vector3(Random.Range(-kickPositionX, kickPositionX), kickPositionY, kickPositionZ);
        targetRotation *= Quaternion.Euler(0f, 0f, kickRotationZ);
    }
}