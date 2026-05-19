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
    public float recoilRiseSpeed = 14f;
    public float recoilRecoverySpeed = 6f;

    [Header("ADS Sensitivity")]
    public WeaponInventory weaponInventory;
    [Range(0f, 1f)] public float adsSensitivityMultiplier = 0.6f;
    public PlayerFpsController fpsController;
    [Range(0f, 50f)] public float sprintFOVPercent = 10f;
    [Range(0f, 50f)] public float slideFOVPercent = 15f;
    public float fovTransitionSpeed = 6f;

    float rotationX = 0f;
    float currentTiltZ = 0f;

    // recoil offset that gets added on top of rotationX
    float recoilPitch = 0f;     // current smoothed pitch offset
    float targetPitch = 0f;     // what we're lerping toward
    float recoilYaw = 0f;
    float targetYaw = 0f;

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
        HandleStrafeTilt();
        HandleRecoil();
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

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        transform.Rotate(0f, mouseX, 0f);

        if (orientation)
            orientation.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void HandleRecoil()
    {
        recoilPitch = Mathf.Lerp(recoilPitch, targetPitch, recoilRiseSpeed * Time.deltaTime);
        recoilYaw = Mathf.Lerp(recoilYaw, targetYaw, recoilRiseSpeed * Time.deltaTime);

        if (!isFiring)
        {
            // bleed recoil offset into rotationX so camera stays put, then zero it
            rotationX += recoilPitch;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            recoilPitch = 0f;
            targetPitch = 0f;

            targetYaw = Mathf.Lerp(targetYaw, 0f, recoilRecoverySpeed * Time.deltaTime);
            recoilYaw = Mathf.Lerp(recoilYaw, 0f, recoilRecoverySpeed * Time.deltaTime);
            if (Mathf.Abs(targetYaw) < 0.001f) targetYaw = 0f;
            if (Mathf.Abs(recoilYaw) < 0.001f) recoilYaw = 0f;
        }
    }

    void HandleStrafeTilt()
    {
        if (playerCamera == null || input == null) return;

        float targetTiltZ = -input.Move.x * maxTiltZ;
        currentTiltZ = Mathf.Lerp(currentTiltZ, targetTiltZ, tiltSpeed * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(
            rotationX + recoilPitch,
            recoilYaw,
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
        else if (fpsController.IsSliding || fpsController.IsSlideJumping)
            targetFOV = baseFOV * (1f + slideFOVPercent / 100f);
        else if (fpsController.IsSprinting)
            targetFOV = baseFOV * (1f + sprintFOVPercent / 100f);
        else
            targetFOV = baseFOV;

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            fovTransitionSpeed * Time.deltaTime
        );
    }

    // called per shot from WeaponBase
    public void ApplyRecoil(float pitchDegrees, float yawDegrees)
    {
        targetPitch -= pitchDegrees;
        targetYaw += yawDegrees;
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