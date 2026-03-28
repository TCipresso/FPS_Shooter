using UnityEngine;

public class SMG : WeaponBase
{
    [Header("SMG Settings")]
    public float range = 60f;
    public int damagePerBullet = 20;

    float nextFireTime = 0f;

    protected override void Awake()
    {
        base.Awake();
        currentMag = maxMag;
    }

    public override void Shoot()
    {
        if (!CanShoot()) return;
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + FireInterval;
        currentMag--;

        PlayFireSound();
        ApplyRecoil();
        AddBloom();
        FireBullet();

        if (currentMag <= 0 && reserveAmmo > 0)
            Reload();
    }

    void FireBullet()
    {
        Vector3 direction = GetAimDirection(0f, 0f);
        Vector3 origin = GetAimOrigin();

        Ray ray = new Ray(origin, direction);
        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            endPoint = hit.point;

            ZombieBase zombie = hit.collider.GetComponent<ZombieBase>();
            if (zombie != null)
                zombie.TakeDamage(damagePerBullet);

            SpawnImpactEffect(hit);
        }
        else
        {
            endPoint = origin + direction * range;
        }

        SpawnTrail(muzzlePoint.position, endPoint);
    }

    public override void Reload()
    {
        if (isReloading || currentMag == maxMag || reserveAmmo <= 0) return;
        TriggerReloadAnimation();
        Debug.Log("[SMG] Reloading...");
    }
}