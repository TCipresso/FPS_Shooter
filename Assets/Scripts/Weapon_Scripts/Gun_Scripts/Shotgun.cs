using UnityEngine;

public class Shotgun : WeaponBase
{
    [Header("Shotgun Settings")]
    public int pelletsPerShot = 8;
    public float spreadAngle = 10f;
    public float range = 50f;

    void Awake()
    {
        currentMag = maxMag;
    }

    public override void Shoot()
    {
        if (!CanShoot()) return;

        currentMag--;

        PlayFireSound();
        TriggerCockAnimation();

        Debug.Log($"[Shotgun] Fired! Ammo: {currentMag}/{maxMag} | Reserve: {reserveAmmo}");

        for (int i = 0; i < pelletsPerShot; i++)
            FirePellet();

        if (currentMag <= 0 && reserveAmmo > 0)
            Reload();
    }

    void FirePellet()
    {
        float x = Random.Range(-spreadAngle, spreadAngle);
        float y = Random.Range(-spreadAngle, spreadAngle);
        Vector3 direction = Quaternion.Euler(x, y, 0f) * muzzlePoint.forward;

        Ray ray = new Ray(muzzlePoint.position, direction);
        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            endPoint = hit.point;
            Debug.Log($"[Shotgun] Pellet hit: {hit.collider.gameObject.name}");
        }
        else
        {
            endPoint = muzzlePoint.position + direction * range;
        }

        SpawnTrail(muzzlePoint.position, endPoint);
    }

    public override void Reload()
    {
        if (isReloading || currentMag == maxMag || reserveAmmo <= 0) return;
        isCocking = false;
        TriggerReloadAnimation();
        Debug.Log($"[Shotgun] Reloading...");
    }
}