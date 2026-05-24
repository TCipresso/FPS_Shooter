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
        FireHitscan(damagePerBullet, range);

        if (currentMag <= 0 && reserveAmmo > 0)
            Reload();
    }

    public override void Reload()
    {
        if (isReloading || currentMag == maxMag || reserveAmmo <= 0) return;
        TriggerReloadAnimation();
        Debug.Log("[Mac] Reloading...");
    }
}