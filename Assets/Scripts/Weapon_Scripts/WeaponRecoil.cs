using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("Feel")]
    public float snapSpeed = 20f;
    public float returnSpeed = 8f;

    [HideInInspector] public float kickRotationZ;
    [HideInInspector] public float kickPositionZ;
    [HideInInspector] public float kickPositionY;
    [HideInInspector] public float kickPositionX;

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
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, snapSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, snapSpeed * Time.deltaTime);
        targetPosition = Vector3.Lerp(targetPosition, originalLocalPosition, returnSpeed * Time.deltaTime);
        targetRotation = Quaternion.Lerp(targetRotation, originalLocalRotation, returnSpeed * Time.deltaTime);
    }

    public void LoadValues(float rotZ, float posZ, float posY, float posX)
    {
        kickRotationZ = rotZ;
        kickPositionZ = posZ;
        kickPositionY = posY;
        kickPositionX = posX;
    }

    public void Kick()
    {
        targetPosition += new Vector3(Random.Range(-kickPositionX, kickPositionX), kickPositionY, kickPositionZ);
        targetRotation *= Quaternion.Euler(0f, 0f, kickRotationZ);
    }
}