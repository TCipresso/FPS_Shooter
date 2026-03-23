using UnityEngine;
using System.Collections.Generic;

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
    public List<WeaponSound> sounds = new List<WeaponSound>();

    [Header("Recoil")]
    public float recoilUp = 2f;
    public float recoilSideRange = 0.5f;

    [Header("Animation")]
    public Animator animator;

    protected bool isReloading = false;
    protected bool isCocking = false;
    protected FPSLook fpsLook;
    protected Camera mainCamera;

    protected virtual void Awake()
    {
        fpsLook = FindFirstObjectByType<FPSLook>();
        mainCamera = Camera.main;

        if (fpsLook == null)
            Debug.LogWarning($"[{gameObject.name}] FPSLook not found in scene.");
        if (mainCamera == null)
            Debug.LogWarning($"[{gameObject.name}] Main Camera not found in scene.");
    }

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

        if (animator != null)
            animator.SetBool("IsReloading", false);

        Debug.Log($"[{gameObject.name}] Reloaded. Ammo: {currentMag}/{maxMag} | Reserve: {reserveAmmo}");
    }

    public void PlaySoundByName(string soundName)
    {
        if (audioSource == null) return;

        WeaponSound ws = sounds.Find(s => s.name == soundName);
        if (ws != null && ws.clip != null)
            audioSource.PlayOneShot(ws.clip);
        else
            Debug.LogWarning($"[{gameObject.name}] Sound not found: {soundName}");
    }

    protected void PlayFireSound()
    {
        if (audioSource == null || fireSound == null) return;
        audioSource.PlayOneShot(fireSound);
    }

    protected void ApplyRecoil()
    {
        if (fpsLook == null) return;
        fpsLook.ApplyRecoil(recoilUp, recoilSideRange);
    }

    // Returns the correct aim direction from the main camera
    protected Vector3 GetAimDirection(Vector3 spreadEuler)
    {
        if (mainCamera == null) return muzzlePoint.forward;
        return Quaternion.Euler(spreadEuler) * mainCamera.transform.forward;
    }

    // Returns the correct aim origin from the main camera
    protected Vector3 GetAimOrigin()
    {
        if (mainCamera == null) return muzzlePoint.position;
        return mainCamera.transform.position;
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

[System.Serializable]
public class WeaponSound
{
    public string name;
    public AudioClip clip;
}