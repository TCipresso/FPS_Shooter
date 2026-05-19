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
    public float maxShakeTilt = 2f;
    public float shakeReturnSpeed = 8f;
    [Range(0f, 1f)] public float hipFireTiltMultiplier = 0.4f;

    [Header("ADS Sensitivity")]
    public WeaponInventory weaponInventory;
    [Range(0f, 1f)] public float adsSensitivityMultiplier = 0.6f;
    public PlayerFpsController fpsController;
    [Range(0f, 50f)] public float sprintFOVPercent = 10f;
    [Range(0f, 50f)] public float slideFOVPercent = 15f;
    public float fovTransitionSpeed = 6f;

    float rotationX = 0f;
    float targetRotationX = 0f;
    float currentTiltZ = 0f;

    float recoilYaw = 0f;
    float targetRecoilYaw = 0f;
    float recoilYawVelocity = 0f;

    float recoilTiltZ = 0f;
    float targetTiltZ_Recoil = 0f;
    float recoilTiltVelocity = 0f;

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

        targetRotationX -= mouseY;
        targetRotationX = Mathf.Clamp(targetRotationX, -lookXLimit, lookXLimit);

        if (isFiring)
            rotationX = Mathf.Lerp(rotationX, targetRotationX, recoilRiseSpeed * Time.deltaTime);
        else
            rotationX = targetRotationX;

        transform.Rotate(0f, mouseX, 0f);

        if (orientation)
            orientation.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void HandleRecoil()
    {
        recoilYaw = Mathf.Lerp(recoilYaw, targetRecoilYaw, recoilRiseSpeed * Time.deltaTime);

        recoilTiltZ = Mathf.SmoothDamp(recoilTiltZ, targetTiltZ_Recoil, ref recoilTiltVelocity, 1f / shakeReturnSpeed);

        if (!isFiring)
        {
            targetRecoilYaw = Mathf.SmoothDamp(targetRecoilYaw, 0f, ref recoilYawVelocity, 1f / recoilRecoverySpeed);
            if (Mathf.Abs(targetRecoilYaw) < 0.001f) targetRecoilYaw = 0f;

            targetTiltZ_Recoil = Mathf.SmoothDamp(targetTiltZ_Recoil, 0f, ref recoilTiltVelocity, 1f / shakeReturnSpeed);
            if (Mathf.Abs(targetTiltZ_Recoil) < 0.001f) targetTiltZ_Recoil = 0f;
        }
    }

    void HandleStrafeTilt()
    {
        if (playerCamera == null || input == null) return;

        float targetTiltZ = -input.Move.x * maxTiltZ;
        currentTiltZ = Mathf.Lerp(currentTiltZ, targetTiltZ, tiltSpeed * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(
            rotationX,
            recoilYaw,
            currentTiltZ + recoilTiltZ
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

    public void ApplyRecoil(float pitchDegrees, float yawDegrees, bool aiming)
    {
        targetRotationX -= pitchDegrees;
        targetRotationX = Mathf.Clamp(targetRotationX, -lookXLimit, lookXLimit);
        targetRecoilYaw += yawDegrees;
        float tiltScale = aiming ? 1f : hipFireTiltMultiplier;
        targetTiltZ_Recoil = Random.Range(-maxShakeTilt, maxShakeTilt) * tiltScale;
        isFiring = true;
    }

    public void StopRecoil()
    {
        isFiring = false;
        recoilYawVelocity = 0f;
    }

    void SyncOverlayFOV()
    {
        if (!overlayCamera || !playerCamera) return;
        if (overlayCamera.orthographic) return;

        overlayCamera.fieldOfView = weaponCameraFOV;
    }
}