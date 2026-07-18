using UnityEngine;

public class M4 : WeaponBase
{
    float nextFireTime = 0f;

    public override void Shoot()
    {
        if (!CanShoot()) return;
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + FireInterval;

        TriggerFireAnimation();
        PlayFireSound();
        PlayMuzzleFlash();
        ApplyRecoil();
        AddBloom();
        //AddBloom();
        Fire(damage);
    }
}