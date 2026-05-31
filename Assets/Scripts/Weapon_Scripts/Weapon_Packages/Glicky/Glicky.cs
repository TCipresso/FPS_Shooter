using UnityEngine;
public class Glicky : WeaponBase
{
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
        AddBloom();
        Fire(damage);
        if (currentMag <= 0 && reserveAmmo > 0)
            Reload();
    }
    public override void Reload()
    {
        if (isReloading || currentMag == maxMag || reserveAmmo <= 0) return;
        TriggerReloadAnimation();
        Debug.Log("[Glicky] Reloading...");
    }
}