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

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem casingEject;

    [Header("Trail")]
    public BulletTrail trailPrefab;
    public string trailPoolKey = "BulletTrail";
    public int trailPoolSize = 10;

    [Header("Impact Effects")]
    public ParticleSystem impactEffect;
    public ParticleSystem zombieImpactEffect;

    [Header("Hit Marker")]
    public ParticleSystem hitMarkerPrefab;
    public string hitMarkerPoolKey = "HitMarker";
    public int hitMarkerPoolSize = 10;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireSound;
    public List<WeaponSound> sounds = new List<WeaponSound>();

    [Header("Camera Recoil")]
    public float recoilUp = 2f;
    public float recoilSideRange = 0.5f;

    [Header("Weapon Recoil")]
    public float kickRotationZ = 5f;
    public float kickPositionZ = -0.1f;
    public float kickPositionY = 0.05f;
    public float kickPositionX = 0.02f;

    [Header("Fire Mode")]
    public bool isAutomatic = false;
    public float rpm = 300f;
    public float FireInterval => 60f / rpm;

    [Header("Gold")]
    public float goldMultiplier = 1f;

    [Header("Accuracy")]
    public float baseAccuracy = 1f;
    public float bloomPerShot = 0.5f;
    public float bloomDecaySpeed = 3f;
    public float maxBloom = 4f;
    [HideInInspector] public float currentBloom = 0f;

    [Header("ADS")]
    public float adsFOVReduction = 15f;
    public float adsBloomMultiplier = 0.25f;
    public float adsTransitionSpeed = 10f;
    public bool adsFadeCrosshair = false;
    [HideInInspector] public bool isAiming = false;

    [Header("Animation")]
    public Animator animator;

    [HideInInspector] public bool isReloading = false;
    [HideInInspector] public bool isCocking = false;
    [HideInInspector] public bool isFiring = false;

    protected FPSLook fpsLook;
    protected Camera mainCamera;
    protected WeaponRecoil weaponRecoil;
    protected PlayerStats playerStats;
    protected FPSController fpsController;

    protected virtual void Awake()
    {
        fpsLook = FindFirstObjectByType<FPSLook>();
        mainCamera = Camera.main;
        playerStats = FindFirstObjectByType<PlayerStats>();
        fpsController = FindFirstObjectByType<FPSController>();

        if (fpsLook == null)
            Debug.LogWarning($"[{gameObject.name}] FPSLook not found in scene.");
        if (mainCamera == null)
            Debug.LogWarning($"[{gameObject.name}] Main Camera not found in scene.");
    }

    protected virtual void OnEnable()
    {
        // Register hit marker pool
        if (BulletPool.Instance != null && hitMarkerPrefab != null)
            BulletPool.Instance.EnsurePoolSize(hitMarkerPoolKey, hitMarkerPrefab.gameObject, hitMarkerPoolSize);

        if (animator != null)
        {
            animator.SetBool("IsReloading", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsSprinting", false);
            animator.SetBool("IsAiming", false);
            animator.ResetTrigger("Cock");
            animator.Play("Idle", 0, 0f);
        }
    }

    protected virtual void Update()
    {
        if (currentBloom > 0f)
            currentBloom = Mathf.Max(0f, currentBloom - bloomDecaySpeed * Time.deltaTime);

        if (fpsController != null && animator != null)
        {
            bool isWalking = !isCocking
                          && !isReloading
                          && fpsController.input.Move.sqrMagnitude > 0.01f;

            animator.SetBool("IsWalking", isWalking);
            animator.SetBool("IsSprinting", fpsController.IsSprinting && !fpsController.IsSprintingSuppressed);

            isAiming = fpsController.input.AimHeld && !isReloading;

            if (isAiming)
                fpsController.IsSprinting = false;

            animator.SetBool("IsAiming", isAiming);
        }
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

    public void ResetState()
    {
        StopAllCoroutines();

        isReloading = false;
        isCocking = false;
        isAiming = false;
        currentBloom = 0f;

        if (animator != null)
        {
            animator.SetBool("IsReloading", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsSprinting", false);
            animator.SetBool("IsAiming", false);
            animator.ResetTrigger("Cock");
            animator.Play("Idle", 0, 0f);
        }
    }

    public virtual void OnCockComplete()
    {
        if (!gameObject.activeSelf) return;
        isCocking = false;
    }

    public void OnReloadComplete()
    {
        if (!gameObject.activeSelf) return;
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

    protected void PlayMuzzleFlash()
    {
        if (muzzleFlash == null) return;
        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleFlash.Play();
    }

    public void EjectCasing()
    {
        if (casingEject == null) return;
        casingEject.Play();
    }

    public void LoadRecoilValues()
    {
        if (weaponRecoil == null)
            weaponRecoil = FindFirstObjectByType<WeaponRecoil>();

        if (weaponRecoil != null)
            weaponRecoil.LoadValues(kickRotationZ, kickPositionZ, kickPositionY, kickPositionX);
    }

    protected void ApplyRecoil()
    {
        if (fpsLook != null)
            fpsLook.ApplyRecoil(recoilUp, recoilSideRange);

        if (weaponRecoil == null)
            weaponRecoil = FindFirstObjectByType<WeaponRecoil>();

        if (weaponRecoil == null)
        {
            Debug.LogError("[WeaponBase] WeaponRecoil not found in scene!");
            return;
        }

        weaponRecoil.Kick();
    }

    protected void SpawnImpactEffect(RaycastHit hit)
    {
        bool isZombie = hit.collider.GetComponent<ZombieBase>() != null;
        ParticleSystem effectPrefab = isZombie ? zombieImpactEffect : impactEffect;

        if (effectPrefab == null) return;

        ParticleSystem effect = Instantiate(
            effectPrefab,
            hit.point,
            Quaternion.LookRotation(hit.normal)
        );

        Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
    }

    protected void SpawnHitMarker(RaycastHit hit)
    {
        if (BulletPool.Instance == null || hitMarkerPrefab == null) return;

        GameObject obj = BulletPool.Instance.Get(
            hitMarkerPoolKey,
            hit.point,
            Quaternion.LookRotation(hit.normal)
        );

        if (obj == null) return;

        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps == null) return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();

        // Return to pool after the particle finishes
        StartCoroutine(ReturnHitMarkerToPool(obj, ps.main.duration + ps.main.startLifetime.constantMax));
    }

    System.Collections.IEnumerator ReturnHitMarkerToPool(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
            obj.SetActive(false);
    }

    protected Vector3 GetAimDirection(float spreadX, float spreadY)
    {
        if (mainCamera == null) return muzzlePoint.forward;

        float bloomScale = isAiming ? adsBloomMultiplier : 1f;
        float totalX = spreadX + Random.Range(-currentBloom, currentBloom) * bloomScale;
        float totalY = spreadY + Random.Range(-currentBloom, currentBloom) * bloomScale;

        Quaternion spreadRotation = Quaternion.AngleAxis(totalY, mainCamera.transform.up)
                                  * Quaternion.AngleAxis(totalX, mainCamera.transform.right);

        return spreadRotation * mainCamera.transform.forward;
    }

    protected void AddBloom()
    {
        currentBloom = Mathf.Min(currentBloom + bloomPerShot, maxBloom);
    }

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

    protected void TriggerFireAnimation()
    {
        if (animator == null) return;
        isFiring = true;
        if (fpsController != null)
        {
            fpsController.IsSprinting = false;
            fpsController.SuppressSprintOnShoot(0.3f);
        }
        animator.Play("Pistol_Fire", 2, 0f);
        StartCoroutine(ResetFiring());
    }

    System.Collections.IEnumerator ResetFiring()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(info.length);
        isFiring = false;
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
        if (BulletPool.Instance == null) return;
        GameObject obj = BulletPool.Instance.Get(trailPoolKey, start, Quaternion.identity);
        if (obj == null) return;
        BulletTrail trail = obj.GetComponent<BulletTrail>();
        if (trail != null) trail.Fire(start, end);
    }
}

[System.Serializable]
public class WeaponSound
{
    public string name;
    public AudioClip clip;
}