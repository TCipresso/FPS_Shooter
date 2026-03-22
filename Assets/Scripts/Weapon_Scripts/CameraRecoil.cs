using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    public float recoilUp = 2f;
    public float recoilSideRange = 0.5f;
    public float snapSpeed = 20f;
    public float returnSpeed = 6f;

    Vector3 currentRecoil = Vector3.zero;
    Vector3 targetRecoil = Vector3.zero;
    Quaternion originalRotation;

    void Start()
    {
        originalRotation = transform.localRotation;
    }

    void Update()
    {
        // Snap toward target kick
        currentRecoil = Vector3.Lerp(currentRecoil, targetRecoil, snapSpeed * Time.deltaTime);

        // Decay target back to zero
        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, returnSpeed * Time.deltaTime);

        // Apply recoil offset on top of original rotation cleanly
        transform.localRotation = originalRotation * Quaternion.Euler(currentRecoil);
    }

    public void Kick(float upOverride = -1f, float sideRangeOverride = -1f)
    {
        float up = upOverride >= 0f ? upOverride : recoilUp;
        float side = sideRangeOverride >= 0f ? sideRangeOverride : recoilSideRange;

        targetRecoil += new Vector3(-up, Random.Range(-side, side), 0f);
    }
}