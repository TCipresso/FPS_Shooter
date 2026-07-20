using UnityEngine;
using Mirror;

public class FPSLook : NetworkBehaviour
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

    [Header("ADS Sensitivity")]
    public WeaponInventory weaponInventory;
    [Range(0f, 1f)] public float adsSensitivityMultiplier = 0.6f;
    public PlayerFpsController fpsController;
    [Range(0f, 50f)] public float sprintFOVPercent = 10f;
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

    [SyncVar] float syncedPitch;
    [SyncVar] float syncedYaw;
    float lookSendTimer;
    float lastSentPitch;
    float lastSentYaw;
    public float remoteLookLerpSpeed = 12f;

    public override void OnStartClient()
    {
        if (isLocalPlayer) return;

        if (playerCamera != null)
        {
            playerCamera.enabled = false;

            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;

            playerCamera.gameObject.SetActive(true);
        }

        if (overlayCamera != null)
        {
            overlayCamera.enabled = false;
            overlayCamera.gameObject.SetActive(true);
        }
    }

    public override void OnStartLocalPlayer()
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
        if (!isLocalPlayer)
        {
            ApplyRemoteLook();
            return;
        }

        HandleRotation();
        HandleStrafeTilt();
        HandleFOV();
        SyncOverlayFOV();
        SendLook();
    }

    void SendLook()
    {
        if (!NetworkClient.active) return;

        if (lookSendTimer > 0f)
        {
            lookSendTimer -= Time.deltaTime;
            return;
        }

        float yaw = transform.eulerAngles.y;

        bool pitchChanged = Mathf.Abs(rotationX - lastSentPitch) > 0.25f;
        bool yawChanged = Mathf.Abs(Mathf.DeltaAngle(yaw, lastSentYaw)) > 0.25f;

        if (!pitchChanged && !yawChanged) return;

        lastSentPitch = rotationX;
        lastSentYaw = yaw;
        lookSendTimer = 0.05f;
        CmdSetLook(rotationX, yaw);
    }

    [Command]
    void CmdSetLook(float pitch, float yaw)
    {
        syncedPitch = pitch;
        syncedYaw = yaw;
    }

    void ApplyRemoteLook()
    {
        Quaternion yawTarget = Quaternion.Euler(0f, syncedYaw, 0f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            yawTarget,
            remoteLookLerpSpeed * Time.deltaTime
        );

        if (playerCamera == null) return;

        Quaternion pitchTarget = Quaternion.Euler(syncedPitch, 0f, 0f);
        Quaternion smoothed = Quaternion.Slerp(
            playerCamera.transform.localRotation,
            pitchTarget,
            remoteLookLerpSpeed * Time.deltaTime
        );

        playerCamera.transform.localRotation = smoothed;

        if (overlayCamera != null)
            overlayCamera.transform.localRotation = smoothed;
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

        WeaponBase weapon = weaponInventory != null ? weaponInventory.GetActiveWeaponBase() : null;
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

        float dashTargetFOV = fpsController.IsDashing
            ? baseFOV * (1f + dashFOVPercent / 100f)
            : targetFOV;

        float dashFOVSpeed = fpsController.IsDashing ? dashFOVInSpeed : dashFOVOutSpeed;
        currentDashFOV = Mathf.Lerp(currentDashFOV, dashTargetFOV, dashFOVSpeed * Time.deltaTime);

        playerCamera.fieldOfView = currentDashFOV;
    }

    public void ApplyRecoil(float pitchDegrees, float yawDegrees, bool aiming, float weaponTiltAmount, float weaponTiltFrequency, float weaponTiltFade, float hipFireTiltMultiplier) { }
    public void StopRecoil() { }

    void SyncOverlayFOV()
    {
        if (!overlayCamera || !playerCamera) return;
        if (overlayCamera.orthographic) return;
        overlayCamera.fieldOfView = weaponCameraFOV;
    }
}