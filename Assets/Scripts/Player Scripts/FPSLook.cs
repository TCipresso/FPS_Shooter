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

    [Header("FOV")]
    public PlayerFpsController fpsController;
    [Range(0f, 50f)] public float slideFOVPercent = 15f;
    public float fovTransitionSpeed = 6f;

    [Header("Dash FOV")]
    [Range(0f, 50f)] public float dashFOVPercent = 15f;
    public float dashFOVInSpeed = 20f;
    public float dashFOVOutSpeed = 8f;

    float rotationX = 0f;
    float targetRotationX = 0f;
    float currentTiltZ = 0f;

    float recoilYaw = 0f;
    float targetRecoilYaw = 0f;
    float recoilYawVelocity = 0f;

    float tiltAmount = 0f;
    float tiltFrequency = 0f;
    float tiltFadeSpeed = 0f;
    float tiltScale = 1f;
    float currentTiltRecoil = 0f;
    float targetTiltRecoil = 0f;
    float tiltRecoilVelocity = 0f;
    float perlinTime = 0f;

    bool isFiring = false;

    float baseFOV;
    float currentDashFOV;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera != null)
        {
            baseFOV = playerCamera.fieldOfView;
            currentDashFOV = baseFOV;
        }

        SyncOverlayFOV();
    }

    void LateUpdate()
    {
        HandleRotation();
        HandleStrafeTilt();
        HandleRecoil();
        HandleFOV();
        SyncOverlayFOV();
    }

    void HandleRotation()
    {
        if (!CanLook || input == null) return;

        float mouseX = input.Look.x * lookSpeed;
        float mouseY = input.Look.y * lookSpeed;

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

        if (!isFiring)
        {
            targetRecoilYaw = Mathf.SmoothDamp(targetRecoilYaw, 0f, ref recoilYawVelocity, 1f / recoilRecoverySpeed);
            if (Mathf.Abs(targetRecoilYaw) < 0.001f) targetRecoilYaw = 0f;

            tiltAmount = Mathf.MoveTowards(tiltAmount, 0f, tiltFadeSpeed * Time.deltaTime);
        }

        if (tiltAmount > 0f)
        {
            perlinTime += Time.deltaTime * tiltFrequency;
            float perlin = (Mathf.PerlinNoise(perlinTime, 0.5f) - 0.5f) * 2f;
            targetTiltRecoil = perlin * tiltAmount * tiltScale;
        }
        else
        {
            targetTiltRecoil = 0f;
        }

        currentTiltRecoil = Mathf.SmoothDamp(currentTiltRecoil, targetTiltRecoil, ref tiltRecoilVelocity, 0.05f);
    }

    void HandleStrafeTilt()
    {
        if (playerCamera == null || input == null) return;

        float targetTiltZ = -input.Move.x * maxTiltZ;
        currentTiltZ = Mathf.Lerp(currentTiltZ, targetTiltZ, tiltSpeed * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(
            rotationX,
            recoilYaw,
            currentTiltZ + currentTiltRecoil
        );

        playerCamera.transform.localRotation = rot;

        if (overlayCamera != null)
            overlayCamera.transform.localRotation = rot;
    }

    void HandleFOV()
    {
        if (playerCamera == null || fpsController == null) return;

        float targetFOV = (fpsController.IsSliding || fpsController.IsSlideJumping)
            ? baseFOV * (1f + slideFOVPercent / 100f)
            : baseFOV;

        float dashTargetFOV = fpsController.IsDashing
            ? baseFOV * (1f + dashFOVPercent / 100f)
            : targetFOV;

        float dashFOVSpeed = fpsController.IsDashing ? dashFOVInSpeed : dashFOVOutSpeed;
        currentDashFOV = Mathf.Lerp(currentDashFOV, dashTargetFOV, dashFOVSpeed * Time.deltaTime);

        playerCamera.fieldOfView = currentDashFOV;
    }

    public void ApplyRecoil(float pitchDegrees, float yawDegrees, bool aiming, float weaponTiltAmount, float weaponTiltFrequency, float weaponTiltFade, float hipFireTiltMultiplier)
    {
        targetRotationX -= pitchDegrees;
        targetRotationX = Mathf.Clamp(targetRotationX, -lookXLimit, lookXLimit);
        targetRecoilYaw += yawDegrees;
        tiltAmount = weaponTiltAmount;
        tiltFrequency = weaponTiltFrequency;
        tiltFadeSpeed = weaponTiltFade;
        tiltScale = hipFireTiltMultiplier;
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