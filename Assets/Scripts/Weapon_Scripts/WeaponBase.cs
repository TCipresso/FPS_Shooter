using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Ammo")]
    public int currentMag;
    public int maxMag;
    public int reserveAmmo;
    public int maxReserve;

    [Header("Timing")]
    public float fireRate = 0.5f;
    public float reloadTime = 2f;

    [Header("Muzzle")]
    public Transform muzzlePoint;

    [Header("Trail")]
    public BulletTrail trailPrefab;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireSound;
    public AudioClip reloadSound;

    protected bool isReloading = false;
    protected float nextFireTime = 0f;

    public abstract void Shoot();
    public abstract void Reload();

    public void Refill()
    {
        reserveAmmo = maxReserve;
        Debug.Log($"[{gameObject.name}] Ammo refilled. Reserve: {reserveAmmo}");
    }

    public bool CanShoot()
    {
        return !isReloading && currentMag > 0 && Time.time >= nextFireTime;
    }

    protected void SpawnTrail(Vector3 start, Vector3 end)
    {
        if (trailPrefab == null) return;
        BulletTrail trail = Instantiate(trailPrefab, start, Quaternion.identity);
        trail.Fire(start, end);
    }

    protected void PlayFireSound()
    {
        if (audioSource == null || fireSound == null) return;
        audioSource.PlayOneShot(fireSound);
    }

    protected void PlayReloadSound()
    {
        if (audioSource == null || reloadSound == null) return;
        audioSource.PlayOneShot(reloadSound);
    }
}