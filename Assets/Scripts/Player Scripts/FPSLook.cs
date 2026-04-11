using UnityEngine;

public class FPSLook : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Camera overlayCamera;
    public Transform orientation;
    public FPSInput input;

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

    [Header("Sprint FOV")]
    public FPSController fpsController;
    [Range(0f, 50f)] public float sprintFOVPercent = 10f;
    public float fovTransitionSpeed = 6f;

    float rotationX = 0f;
    float currentTiltZ = 0f;

    Vector3 currentRecoil = Vector3.zero;
    Vector3 targetRecoil = Vector3.zero;

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

        playerCamera.transform.localRotation = Quaternion.Euler(
            rotationX + currentRecoil.x,
            currentRecoil.y,
            currentTiltZ
        );
    }

    void HandleRecoil()
    {
        currentRecoil = Vector3.Lerp(currentRecoil, targetRecoil, recoilSnapSpeed * Time.deltaTime);
        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
    }

    void HandleSprintFOV()
    {
        if (playerCamera == null || fpsController == null) return;

        float targetFOV = (fpsController.IsSprinting || fpsController.IsSliding)
            ? baseFOV * (1f + sprintFOVPercent / 100f)
            : baseFOV;

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            fovTransitionSpeed * Time.deltaTime
        );
    }

    public void ApplyRecoil(float up, float side)
    {
        targetRecoil += new Vector3(-up, Random.Range(-side, side), 0f);
    }

    void SyncOverlayFOV()
    {
        if (!overlayCamera || !playerCamera) return;
        if (overlayCamera.orthographic) return;

        overlayCamera.fieldOfView = baseFOV;
    }
}