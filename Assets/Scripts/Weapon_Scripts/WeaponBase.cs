using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Ammo")]
    public int currentMag;
    public int maxMag;
    public int reserveAmmo;
    public int maxReserve;

    [Header("Muzzle")]
    public Transform muzzlePoint;

    [Header("Trail")]
    public BulletTrail trailPrefab;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireSound;
    public AudioClip reloadSound;

    [Header("Animation")]
    public Animator animator;

    protected bool isReloading = false;
    protected bool isCocking = false;

    public abstract void Shoot();
    public abstract void Reload();

    public void Refill()
    {
        reserveAmmo = maxReserve;
        Debug.Log($"[{gameObject.name}] Ammo refilled. Reserve: {reserveAmmo}");
    }

    public bool CanShoot()
    {
        if (isReloading) return false;
        if (isCocking) return false;
        if (currentMag <= 0) return false;
        return true;
    }

    public void OnCockComplete()
    {
        isCocking = false;
    }

    public void OnReloadComplete()
    {
        int needed = maxMag - currentMag;
        int given = Mathf.Min(needed, reserveAmmo);
        currentMag += given;
        reserveAmmo -= given;
        isReloading = false;

        // Tell animator reload is done — unlocks the state
        if (animator != null)
            animator.SetBool("IsReloading", false);

        Debug.Log($"[{gameObject.name}] Reloaded. Ammo: {currentMag}/{maxMag} | Reserve: {reserveAmmo}");
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

    protected void TriggerCockAnimation()
    {
        if (animator == null) return;
        isCocking = true;
        animator.SetTrigger("Cock");
    }

    protected void TriggerReloadAnimation()
    {
        if (animator == null) return;
        isReloading = true;
        isCocking = false;
        animator.SetBool("IsReloading", true);
    }

    protected void SpawnTrail(Vector3 start, Vector3 end)
    {
        if (trailPrefab == null) return;
        BulletTrail trail = Instantiate(trailPrefab, start, Quaternion.identity);
        trail.Fire(start, end);
    }
}