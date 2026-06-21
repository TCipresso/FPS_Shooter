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

    [Header("FOV")]
    public PlayerFpsController fpsController;
    [Range(0f, 50f)] public float slideFOVPercent = 15f;
    public float fovTransitionSpeed = 6f;

    [Header("Dash FOV")]
    [Range(0f, 50f)] public float dashFOVPercent = 15f;
    public float dashFOVInSpeed = 20f;
    public float dashFOVOutSpeed = 8f;

    float rotationX = 0f;
    float currentTiltZ = 0f;
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
        HandleFOV();
        SyncOverlayFOV();
    }

    void HandleRotation()
    {
        if (!CanLook || input == null) return;

        float mouseX = input.Look.x * lookSpeed;
        float mouseY = input.Look.y * lookSpeed;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        transform.Rotate(0f, mouseX, 0f);

        if (orientation)
            orientation.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void HandleStrafeTilt()
    {
        if (playerCamera == null || input == null) return;

        float targetTiltZ = -input.Move.x * maxTiltZ;
        currentTiltZ = Mathf.Lerp(currentTiltZ, targetTiltZ, tiltSpeed * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(rotationX, 0f, currentTiltZ);

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

    public void StopRecoil() { }

    void SyncOverlayFOV()
    {
        if (!overlayCamera || !playerCamera) return;
        if (overlayCamera.orthographic) return;
        overlayCamera.fieldOfView = weaponCameraFOV;
    }
}