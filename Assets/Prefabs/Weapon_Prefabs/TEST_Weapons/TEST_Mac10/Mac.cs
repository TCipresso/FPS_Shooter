using UnityEngine;

public class Mac : WeaponBase
{
    [Header("Mac Settings")]
    public float range = 50f;
    public int damagePerBullet = 35;

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

            HitBox hitBox = hit.collider.GetComponent<HitBox>();
            if (hitBox != null)
            {
                hitBox.TakeDamageWithHitPoint(damagePerBullet, playerStats, hit.point, playerStats != null ? playerStats.goldGainMultiplier : 1f);
            }
            else
            {
                ZombieBase zombie = hit.collider.GetComponent<ZombieBase>();
                if (zombie != null)
                {
                    zombie.TakeDamage(ApplyCrit(damagePerBullet), playerStats, playerStats != null ? playerStats.goldGainMultiplier : 1f);
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
        TriggerReloadAnimation();
        Debug.Log("[Mac] Reloading...");
    }
}