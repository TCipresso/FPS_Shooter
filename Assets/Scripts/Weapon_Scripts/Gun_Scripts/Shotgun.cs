using UnityEngine;

public class Shotgun : WeaponBase
{
    [Header("Shotgun Settings")]
    public int pelletsPerShot = 8;
    public float spreadAngle = 10f;
    public float range = 50f;
    public int damagePerPellet = 15;

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

        TriggerFireAnimation();
        PlayFireSound();
        TriggerCockAnimation();
        ApplyRecoil();
        AddBloom();

        for (int i = 0; i < pelletsPerShot; i++)
            FirePellet();

        if (currentMag <= 0 && reserveAmmo > 0)
            Reload();
    }

    void FirePellet()
    {
        float x = Random.Range(-spreadAngle, spreadAngle);
        float y = Random.Range(-spreadAngle, spreadAngle);

        Vector3 direction = GetAimDirection(x, y);
        Vector3 origin = GetAimOrigin();

        Ray ray = new Ray(origin, direction);
        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            endPoint = hit.point;

            ZombieBase zombie = hit.collider.GetComponent<ZombieBase>();
            if (zombie != null)
                zombie.TakeDamage(damagePerPellet, playerStats, goldMultiplier);

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
        isCocking = false;
        TriggerReloadAnimation();
        Debug.Log("[Shotgun] Reloading...");
    }
}