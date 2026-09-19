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

    [Header("Movement FOV")]
    public PlayerFpsController fpsController;
    [Range(0f, 50f)] public float slideFOVPercent = 15f;
    public float fovTransitionSpeed = 6f;

    [Header("Dash FOV")]
    [Range(0f, 50f)] public float dashFOVPercent = 15f;
    public float dashFOVInSpeed = 20f;
    public float dashFOVOutSpeed = 8f;

    [Header("Shot FOV")]
    [Min(0f)] public float maxShotFOV = 8f;
    [Min(0.01f)] public float shotFOVReturnSpeed = 18f;
    [Range(0f, 1f)] public float weaponShotFOVMultiplier = 1f;

    float rotationX = 0f;
    float currentTiltZ = 0f;
    float baseFOV;
    float currentDashFOV;
    float shotFOV;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.enabled = true;

            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = true;

            baseFOV = playerCamera.fieldOfView;
            currentDashFOV = baseFOV;
        }

        if (overlayCamera != null)
        {
            overlayCamera.gameObject.SetActive(true);
            overlayCamera.enabled = true;
        }

        SyncOverlayFOV();
    }

    void LateUpdate()
    {
        HandleRotation();
        HandleStrafeTilt();
        HandleFOV();
        SyncOverlayFOV();
        shotFOV *= Mathf.Exp(-Mathf.Max(0.01f, shotFOVReturnSpeed) * Time.deltaTime);
        if (Mathf.Abs(shotFOV) < 0.001f)
            shotFOV = 0f;
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
        if (playerCamera == null) return;

        float targetFOV;

        if (fpsController != null && (fpsController.IsSliding || fpsController.IsSlideJumping))
            targetFOV = baseFOV * (1f + slideFOVPercent / 100f);
        else
            targetFOV = baseFOV;

        bool isDashing = fpsController != null && fpsController.IsDashing;
        float dashTargetFOV = isDashing
            ? baseFOV * (1f + dashFOVPercent / 100f)
            : targetFOV;

        float dashFOVSpeed = isDashing ? dashFOVInSpeed : dashFOVOutSpeed;
        currentDashFOV = Mathf.Lerp(currentDashFOV, dashTargetFOV, dashFOVSpeed * Time.deltaTime);

        playerCamera.fieldOfView = Mathf.Clamp(currentDashFOV + shotFOV, 1f, 179f);
    }

    public void AddShotFOV(float impulse)
    {
        float limit = Mathf.Max(0f, maxShotFOV);
        shotFOV = Mathf.Clamp(shotFOV + impulse, -limit, limit);
    }

    public void ApplyRecoil(float pitchDegrees, float yawDegrees, bool aiming, float weaponTiltAmount, float weaponTiltFrequency, float weaponTiltFade, float hipFireTiltMultiplier) { }
    public void StopRecoil() { }

    void SyncOverlayFOV()
    {
        if (!overlayCamera || !playerCamera) return;
        if (overlayCamera.orthographic) return;
        overlayCamera.fieldOfView = Mathf.Clamp(weaponCameraFOV + shotFOV * weaponShotFOVMultiplier, 1f, 179f);
    }
}
