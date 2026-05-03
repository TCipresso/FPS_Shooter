using UnityEngine;

public class PlayerFpsLook : MonoBehaviour
{
    [SerializeField] private FPSInput input;
    [SerializeField] private Transform playerBody;
    [SerializeField] private Camera cam;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Tilt")]
    [SerializeField] private float tiltAngle = 3f;
    [SerializeField] private float tiltSpeed = 8f;

    [Header("Sprint FOV")]
    [SerializeField] private float sprintFovMultiplier = 1.15f;
    [SerializeField] private float fovLerpSpeed = 8f;

    private const float SensitivityScale = 0.01f;
    private float xRotation;
    private float currentTilt;
    private float baseFov;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (input == null && playerBody != null)
            input = playerBody.GetComponent<FPSInput>();

        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam != null)
            baseFov = cam.fieldOfView;
    }

    void Update()
    {
        if (input == null || playerBody == null) return;

        Vector2 delta = input.Look * mouseSensitivity * SensitivityScale;

        xRotation -= delta.y;
        xRotation = Mathf.Clamp(xRotation, -maxPitch, maxPitch);

        float targetTilt = -input.Move.x * tiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * Time.deltaTime);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        playerBody.Rotate(Vector3.up * delta.x);

        // Sprint FOV — driven by the controller's Sprinting flag
        if (cam != null)
        {
            float targetFov = (input.IsSprinting) ? baseFov * sprintFovMultiplier : baseFov;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovLerpSpeed * Time.deltaTime);
        }
    }
}