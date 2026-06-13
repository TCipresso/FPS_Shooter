using UnityEngine;

public class SpeedLinesVFX : MonoBehaviour
{
    public PlayerFpsController fpsController;

    [Header("Direction Points")]
    public ParticleSystem vfx_N;
    public ParticleSystem vfx_NE;
    public ParticleSystem vfx_E;
    public ParticleSystem vfx_SE;
    public ParticleSystem vfx_S;
    public ParticleSystem vfx_SW;
    public ParticleSystem vfx_W;
    public ParticleSystem vfx_NW;

    [Header("Slide Repeat")]
    [SerializeField] private float slideRepeatRate = 0.3f;

    private ParticleSystem activeSystem;
    private ParticleSystem slideLockedSystem;
    private float slideRepeatTimer;

    void Start()
    {
        StopAll();
    }

    void Update()
    {
        if (fpsController == null) return;

        if (!fpsController.IsDashing && !fpsController.IsSliding)
        {
            if (activeSystem != null)
                StopAll();
            slideLockedSystem = null;
            return;
        }

        Vector3 moveDir = fpsController.GetMoveDirection();
        if (moveDir.sqrMagnitude < 0.01f)
        {
            if (activeSystem != null)
                StopAll();
            slideLockedSystem = null;
            return;
        }

        if (fpsController.IsSliding)
        {
            // Lock direction on slide start
            if (slideLockedSystem == null)
            {
                Vector3 flatDir = new Vector3(moveDir.x, 0f, moveDir.z).normalized;
                Vector3 localDir = transform.InverseTransformDirection(flatDir);
                float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;
                slideLockedSystem = GetSystemForAngle(angle);
                activeSystem = slideLockedSystem;
                StopAll();
                activeSystem = slideLockedSystem;
                activeSystem?.Play();
                slideRepeatTimer = slideRepeatRate;
            }
            else
            {
                slideRepeatTimer -= Time.deltaTime;
                if (slideRepeatTimer <= 0f)
                {
                    slideLockedSystem?.Play();
                    slideRepeatTimer = slideRepeatRate;
                }
            }
            return;
        }

        // Dash logic unchanged
        slideLockedSystem = null;

        Vector3 flatDirDash = new Vector3(moveDir.x, 0f, moveDir.z).normalized;
        Vector3 localDirDash = transform.InverseTransformDirection(flatDirDash);
        float angleDash = Mathf.Atan2(localDirDash.x, localDirDash.z) * Mathf.Rad2Deg;
        if (angleDash < 0f) angleDash += 360f;

        ParticleSystem target = GetSystemForAngle(angleDash);

        if (target == activeSystem) return;

        StopAll();
        activeSystem = target;
        if (activeSystem != null)
            activeSystem.Play();
    }

    ParticleSystem GetSystemForAngle(float angle)
    {
        if (angle < 22.5f || angle >= 337.5f) return vfx_N;
        if (angle < 67.5f) return vfx_NE;
        if (angle < 112.5f) return vfx_E;
        if (angle < 157.5f) return vfx_SE;
        if (angle < 202.5f) return vfx_S;
        if (angle < 247.5f) return vfx_SW;
        if (angle < 292.5f) return vfx_W;
        return vfx_NW;
    }

    void StopAll()
    {
        vfx_N?.Stop(false);
        vfx_NE?.Stop(false);
        vfx_E?.Stop(false);
        vfx_SE?.Stop(false);
        vfx_S?.Stop(false);
        vfx_SW?.Stop(false);
        vfx_W?.Stop(false);
        vfx_NW?.Stop(false);
        activeSystem = null;
    }
}