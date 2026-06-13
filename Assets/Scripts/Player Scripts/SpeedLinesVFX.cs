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

    private ParticleSystem activeSystem;

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
            return;
        }

        Vector3 moveDir = fpsController.GetMoveDirection();
        if (moveDir.sqrMagnitude < 0.01f)
        {
            if (activeSystem != null)
                StopAll();
            return;
        }

        Vector3 flatDir = new Vector3(moveDir.x, 0f, moveDir.z).normalized;
        Vector3 localDir = transform.InverseTransformDirection(flatDir);
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        Debug.Log($"moveDir={moveDir} angle={angle} target={GetSystemForAngle(angle).name}");

        ParticleSystem target = GetSystemForAngle(angle);

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
        vfx_N?.Stop(true);
        vfx_NE?.Stop(true);
        vfx_E?.Stop(true);
        vfx_SE?.Stop(true);
        vfx_S?.Stop(true);
        vfx_SW?.Stop(true);
        vfx_W?.Stop(true);
        vfx_NW?.Stop(true);
        activeSystem = null;
    }
}