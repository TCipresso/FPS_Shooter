using UnityEngine;

public class FPSLook : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Camera overlayCamera;
    public Transform orientation;
    public FPSInput input;
    public float weaponCameraFOV = 90f;

    [Header("Look Settings")]
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public bool CanLook = true;

    [Header("Strafe Tilt")]
    public float maxTiltZ = 5f;
    public float tiltSpeed = 8f;

    [Header("Recoil")]
    public float recoilSnapSpeed = 20f;
    public float recoilReturnSpeed = 6f;
    public float maxRecoilRise = 12f;

    [Header("ADS Sensitivity")]
    public WeaponInventory weaponInventory;
    [Range(0f, 1f)] public float adsSensitivityMultiplier = 0.6f;
    public PlayerFpsController fpsController;
    [Range(0f, 50f)] public float sprintFOVPercent = 10f;
    public float fovTransitionSpeed = 6f;

    float rotationX = 0f;
    float currentTiltZ = 0f;

    Vector3 currentRecoil = Vector3.zero;
    Vector3 targetRecoil = Vector3.zero;
    bool isFiring = false;

    float baseFOV;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera != null)
            baseFOV = playerCamera.fieldOfView;

        SyncOverlayFOV();
    }

    void LateUpdate()
    {
        HandleRotation();
        HandleRecoil();
        HandleStrafeTilt();
        HandleSprintFOV();
        SyncOverlayFOV();
    }

    void HandleRotation()
    {
        if (!CanLook || input == null) return;

        float sensScale = 1f;

        if (weaponInventory != null)
        {
            WeaponBase weapon = weaponInventory.GetActiveWeaponBase();

            if (weapon != null && weapon.isAiming)
            {
                float aimFOV = baseFOV * (1f - weapon.adsFOVReduction / 100f);
                float fovRatio = aimFOV / baseFOV;
                sensScale = fovRatio * adsSensitivityMultiplier;
            }
        }

        float mouseX = input.Look.x * lookSpeed * sensScale;
        float mouseY = input.Look.y * lookSpeed * sensScale;

        float pitchDelta = -mouseY;

        if (currentRecoil.x < 0f && pitchDelta > 0f)
        {
            float consume = Mathf.Min(pitchDelta, -currentRecoil.x);

            currentRecoil.x += consume;
            targetRecoil.x += consume;

            pitchDelta -= consume;
        }

        rotationX += pitchDelta;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        transform.Rotate(0f, mouseX, 0f);

        if (orientation)
            orientation.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void HandleRecoil()
    {
        currentRecoil = Vector3.Lerp(
            currentRecoil,
            targetRecoil,
            recoilSnapSpeed * Time.deltaTime
        );

        if (!isFiring)
        {
            targetRecoil = Vector3.Lerp(
                targetRecoil,
                Vector3.zero,
                recoilReturnSpeed * Time.deltaTime
            );

            if (targetRecoil.sqrMagnitude < 0.0001f)
                targetRecoil = Vector3.zero;
        }
    }

    void HandleStrafeTilt()
    {
        if (playerCamera == null || input == null) return;

        float targetTiltZ = -input.Move.x * maxTiltZ;
        currentTiltZ = Mathf.Lerp(currentTiltZ, targetTiltZ, tiltSpeed * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(
            rotationX + currentRecoil.x,
            currentRecoil.y,
            currentTiltZ
        );

        playerCamera.transform.localRotation = rot;

        if (overlayCamera != null)
            overlayCamera.transform.localRotation = rot;
    }

    void HandleSprintFOV()
    {
        if (playerCamera == null || fpsController == null) return;

        WeaponBase weapon = FindFirstObjectByType<WeaponBase>();
        bool isAiming = weapon != null && weapon.isAiming;

        float targetFOV;

        if (isAiming)
            targetFOV = baseFOV * (1f - weapon.adsFOVReduction / 100f);
        else if (fpsController.IsSprinting || fpsController.IsSliding || fpsController.IsSlideJumping)
            targetFOV = baseFOV * (1f + sprintFOVPercent / 100f);
        else
            targetFOV = baseFOV;

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            fovTransitionSpeed * Time.deltaTime
        );
    }

    public void ApplyRecoil(float up, float side)
    {
        targetRecoil += new Vector3(-up, Random.Range(-side, side), 0f);

        targetRecoil.x = Mathf.Clamp(targetRecoil.x, -maxRecoilRise, 0f);

        isFiring = true;
    }

    public void StopRecoil()
    {
        isFiring = false;
    }

    void SyncOverlayFOV()
    {
        if (!overlayCamera || !playerCamera) return;
        if (overlayCamera.orthographic) return;

        overlayCamera.fieldOfView = weaponCameraFOV;
    }
}