using UnityEngine;
public class WeaponRecoil : MonoBehaviour
{
    [Header("Feel")]
    public float snapSpeed = 20f;
    public float returnSpeed = 8f;
    public float perlinFrequency = 8f;
    [HideInInspector] public float kickRotationX;
    [HideInInspector] public float kickRotationY;
    [HideInInspector] public float kickRotationZ;
    [HideInInspector] public float kickPositionZ;
    [HideInInspector] public float kickPositionY;
    [HideInInspector] public float kickPositionX;
    public Vector3 originalLocalPosition;
    public Quaternion originalLocalRotation;
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    float perlinTime = 0f;
    bool isKicking = false;
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
        if (Vector3.Distance(targetPosition, originalLocalPosition) < 0.001f)
            isKicking = false;
    }
    public void LoadValues(float rotX, float rotY, float rotZ, float posZ, float posY, float posX)
    {
        kickRotationX = rotX;
        kickRotationY = rotY;
        kickRotationZ = rotZ;
        kickPositionZ = posZ;
        kickPositionY = posY;
        kickPositionX = posX;
    }
    public void Kick()
    {
        isKicking = true;
        perlinTime += Time.deltaTime * perlinFrequency;

        float perlinX = (Mathf.PerlinNoise(perlinTime, 0f) - 0.5f) * 2f;
        float perlinY = (Mathf.PerlinNoise(perlinTime, 1f) - 0.5f) * 2f;
        float perlinZ = (Mathf.PerlinNoise(0f, perlinTime) - 0.5f) * 2f;

        targetPosition += new Vector3(
            perlinX * kickPositionX,
            kickPositionY,
            kickPositionZ
        );

        targetRotation *= Quaternion.Euler(
            kickRotationX,
            perlinY * kickRotationY,
            perlinZ * kickRotationZ
        );
    }
}