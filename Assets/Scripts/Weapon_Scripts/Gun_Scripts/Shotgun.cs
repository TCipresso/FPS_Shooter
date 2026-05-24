using UnityEngine;

public class Shotgun : WeaponBase
{
    [Header("Shotgun Settings")]
    public int pelletsPerShot = 8;
    public float spreadAngle = 10f;

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
        PlayMuzzleFlash();
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

        bool didHit = bulletData != null && bulletData.hitMask != 0
            ? Physics.Raycast(ray, out RaycastHit hit, range, bulletData.hitMask)
            : Physics.Raycast(ray, out hit, range);

        if (didHit)
        {
            endPoint = hit.point;

            HitBox hitBox = hit.collider.GetComponent<HitBox>();
            if (hitBox != null)
            {
                hitBox.TakeDamageWithHitPoint(damage, playerStats, hit.point,
                    playerStats != null ? playerStats.goldGainMultiplier : 1f);
            }
            else
            {
                ZombieBase zombie = hit.collider.GetComponent<ZombieBase>();
                if (zombie != null)
                {
                    zombie.TakeDamage(ApplyCrit(damage), playerStats,
                        playerStats != null ? playerStats.goldGainMultiplier : 1f);
                    if (HitMarkerPool.Instance != null)
                        HitMarkerPool.Instance.Spawn(hit.point, false);
                }
            }

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