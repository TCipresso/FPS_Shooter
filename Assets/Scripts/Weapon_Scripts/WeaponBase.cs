using UnityEngine;
using System.Collections.Generic;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Ammo")]
    public int currentMag;
    public int maxMag;
    [HideInInspector] public int baseMaxMag;
    public int reserveAmmo;
    public int maxReserve;

    [Header("Muzzle")]
    public Transform muzzlePoint;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem casingEject;

    [Header("Bullet Data")]
    public BulletDataSO bulletData;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireSound;
    public List<WeaponSound> sounds = new List<WeaponSound>();

    [Header("Damage / Range")]
    public int damage = 25;
    public float range = 50f;

    [Header("Camera Recoil")]
    public float maxRecoilUp = 4f;
    public float maxRecoilSide = 1.5f;
    [Tooltip("X = shot index 0-1, Y = 0-1 recoil strength. Controls pitch per shot.")]
    public AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f);
    [Tooltip("X = shot index 0-1, Y = 0-1 side kick strength.")]
    public AnimationCurve recoilSideCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
    [Tooltip("How many shots to reach the end of the curve.")]
    public int recoilMaxShots = 10;

    [Header("Camera Recoil - ADS Multipliers")]
    [Range(0f, 1f)] public float adsRecoilUpMultiplier = 0.4f;
    [Range(0f, 1f)] public float adsRecoilSideMultiplier = 0.3f;

    [Header("Camera Tilt")]
    public float tiltAmount = 0.3f;
    public float tiltFrequency = 20f;
    public float tiltFadeSpeed = 3f;
    public float adsTiltMultiplier = 0.3f;
    [Range(0f, 1f)] public float hipFireTiltMultiplier = 0.4f;

    [Header("Weapon Recoil - Hip Fire")]
    public float kickRotationX = 2f;
    public float kickRotationY = 2f;
    public float kickRotationZ = 5f;
    public float kickPositionZ = -0.1f;
    public float kickPositionY = 0.05f;
    public float kickPositionX = 0.02f;

    [Header("Weapon Recoil - ADS")]
    public float adsKickRotationX = 1f;
    public float adsKickRotationY = 1f;
    public float adsKickRotationZ = 2f;
    public float adsKickPositionZ = -0.05f;
    public float adsKickPositionY = 0.02f;
    public float adsKickPositionX = 0.01f;

    [Header("Fire Mode")]
    public bool isAutomatic = false;
    public float rpm = 300f;
    [HideInInspector] public float baseRpm;
    public float FireInterval => 60f / rpm;

    [Header("Critical Hit")]
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critMultiplier = 2f;

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

    [Header("Reload")]
    public bool canReloadWhileSprinting = false;

    [Header("Animation")]
    public Animator animator;
    public float walkStopDelay = 0.1f;
    public string FireClipName = "Enter Clip name Here";

    [HideInInspector] public bool isReloading = false;
    [HideInInspector] public bool isCocking = false;
    [HideInInspector] public bool isFiring = false;

    int shotsFired = 0;
    float walkStopTimer = 0f;

    protected FPSLook fpsLook;
    protected Camera mainCamera;
    protected WeaponRecoil weaponRecoil;
    protected PlayerStats playerStats;
    protected PlayerFpsController fpsController;

    protected virtual void Awake()
    {
        baseRpm = rpm;
        baseMaxMag = maxMag;
        fpsLook = FindFirstObjectByType<FPSLook>();
        mainCamera = Camera.main;
        playerStats = FindFirstObjectByType<PlayerStats>();
        fpsController = FindFirstObjectByType<PlayerFpsController>();

        if (fpsLook == null)
            Debug.LogWarning($"[{gameObject.name}] FPSLook not found in scene.");
        if (mainCamera == null)
            Debug.LogWarning($"[{gameObject.name}] Main Camera not found in scene.");
        if (bulletData == null)
            Debug.LogWarning($"[{gameObject.name}] No BulletDataSO assigned.");
    }

    protected virtual void OnEnable()
    {
        shotsFired = 0;

        if (animator != null)
        {
            animator.SetBool("IsReloading", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsSprinting", false);
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsIdle", false);
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
            if (isReloading && fpsController.IsSprinting && !canReloadWhileSprinting)
                CancelReload();

            bool isWalking = !isCocking
                          && !isReloading
                          && fpsController.input.Move.sqrMagnitude > 0.01f;

            if (isWalking)
                walkStopTimer = walkStopDelay;
            else if (walkStopTimer > 0f)
                walkStopTimer -= Time.deltaTime;

            bool showWalking = !isReloading && (isWalking || walkStopTimer > 0f);

            bool showSprinting = fpsController.IsSprinting && !fpsController.IsSprintingSuppressed
                                 && !(isReloading && canReloadWhileSprinting);

            animator.SetBool("IsWalking", showWalking);
            animator.SetBool("IsSprinting", !isReloading && showSprinting);
            animator.SetBool("IsIdle", isReloading);

            isAiming = fpsController.input.AimHeld && !isReloading;

            if (isAiming)
                fpsController.IsSprinting = false;

            animator.SetBool("IsAiming", isAiming);
        }
    }

    public abstract void Shoot();
    public abstract void Reload();

   

    private void FireHitscan(int damage, float range)
    {
        if (bulletData == null) return;

        Vector3 direction = GetAimDirection(0f, 0f);
        Vector3 origin = GetAimOrigin();
        Ray ray = new Ray(origin, direction);
        Vector3 endPoint;

        bool didHit = bulletData.hitMask != 0
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

   

    public void ApplyExtraMagazine(int extra)
    {
        maxMag = baseMaxMag + extra;
        currentMag = Mathf.Min(currentMag, maxMag);
    }

    public void ApplyAttackSpeed(float attackSpeed)
    {
        rpm = baseRpm * attackSpeed;
    }

    public void Refill()
    {
        reserveAmmo = maxReserve;
        Debug.Log($"[{gameObject.name}] Ammo refilled. Reserve: {reserveAmmo}");
    }

    public bool CanShoot()
    {
        if (isCocking) return false;
        if (currentMag <= 0) return false;
        if (isReloading)
        {
            CancelReload();
            return false;
        }
        return true;
    }

    public void ResetState()
    {
        StopAllCoroutines();

        isReloading = false;
        isCocking = false;
        isAiming = false;
        currentBloom = 0f;
        shotsFired = 0;

        if (animator != null)
        {
            animator.SetBool("IsReloading", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsSprinting", false);
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsIdle", false);
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

    protected void Fire(int damage)
    {
        if (bulletData == null) return;

        switch (bulletData.bulletType)
        {
            case BulletType.Hitscan:
                FireHitscan(damage, range);
                break;
            case BulletType.Projectile:
                FireProjectile(damage);
                break;
        }
    }


    private void FireProjectile(int damage)  // stub for later
    {
        Debug.LogWarning("[WeaponBase] Projectile firing not yet implemented.");
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

    protected void SpawnImpactEffect(RaycastHit hit)
    {
        if (ImpactEffectPool.Instance == null) return;
        bool isZombie = hit.collider.GetComponent<ZombieBase>() != null;
        if (isZombie)
            ImpactEffectPool.Instance.SpawnZombie(hit.point, hit.normal);
        else
            ImpactEffectPool.Instance.SpawnWorld(hit.point, hit.normal);
    }

    protected void SpawnTrail(Vector3 start, Vector3 end)
    {
        if (bulletData == null || BulletPool.Instance == null) return;
        GameObject obj = BulletPool.Instance.Get(bulletData.trailPoolKey, start, Quaternion.identity);
        if (obj == null) return;
        BulletTrail trail = obj.GetComponent<BulletTrail>();
        if (trail != null) trail.Fire(start, end);
    }

   

    public void LoadRecoilValues()
    {
        if (weaponRecoil == null)
            weaponRecoil = FindFirstObjectByType<WeaponRecoil>();

        if (weaponRecoil != null)
            weaponRecoil.LoadValues(kickRotationX, kickRotationY, kickRotationZ,
                kickPositionZ, kickPositionY, kickPositionX);
    }

    protected void ApplyRecoil()
    {
        float t = recoilMaxShots > 0
            ? Mathf.Clamp01((float)shotsFired / recoilMaxShots)
            : 1f;

        float pitchStrength = recoilCurve.Evaluate(t);
        float sideStrength = recoilSideCurve.Evaluate(t);

        float pitch = pitchStrength * maxRecoilUp;
        float yaw = sideStrength * maxRecoilSide * (Random.value > 0.5f ? 1f : -1f);

        if (isAiming)
        {
            pitch *= adsRecoilUpMultiplier;
            yaw *= adsRecoilSideMultiplier;
        }

        float finalTilt = tiltAmount * (isAiming ? adsTiltMultiplier : 1f);

        if (fpsLook != null)
            fpsLook.ApplyRecoil(pitch, yaw, isAiming, finalTilt, tiltFrequency, tiltFadeSpeed, hipFireTiltMultiplier);

        if (weaponRecoil == null)
            weaponRecoil = FindFirstObjectByType<WeaponRecoil>();

        if (weaponRecoil != null)
        {
            if (isAiming)
                weaponRecoil.LoadValues(adsKickRotationX, adsKickRotationY, adsKickRotationZ,
                    adsKickPositionZ, adsKickPositionY, adsKickPositionX);
            else
                weaponRecoil.LoadValues(kickRotationX, kickRotationY, kickRotationZ,
                    kickPositionZ, kickPositionY, kickPositionX);

            weaponRecoil.Kick();
        }

        shotsFired++;
    }

    public void StopRecoil()
    {
        shotsFired = 0;
        if (fpsLook != null)
            fpsLook.StopRecoil();
    }

    public void Equip(WeaponInstance instance)
    {
        if (instance == null || instance.definition == null) return;

        WeaponDefinitionSO def = instance.definition;

        bulletData = def.bulletData;
        damage = instance.finalDamage;
        range = instance.finalRange;
        rpm = instance.finalRpm;
        baseRpm = instance.finalRpm;
        maxMag = instance.finalMagSize;
        baseMaxMag = instance.finalMagSize;
        currentMag = instance.finalMagSize;
        reserveAmmo = instance.finalReserveAmmo;
        maxReserve = instance.finalReserveAmmo;

        WeaponAttachmentVisuals visuals = GetComponentInParent<WeaponAttachmentVisuals>();
        if (visuals == null)
            visuals = GetComponentInChildren<WeaponAttachmentVisuals>();

        Debug.Log($"[WeaponBase] visuals found: {visuals != null} on {gameObject.name}");

        if (visuals != null)
            visuals.ApplyAttachments(instance.rolledAttachments);

        Debug.Log($"[WeaponBase] Equipped {def.weaponName} | Rarity: {instance.rarity} | Damage: {damage} | Range: {range} | RPM: {rpm} | Mag: {maxMag}");
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

    protected Vector3 GetAimOrigin()
    {
        if (mainCamera == null) return muzzlePoint.position;
        return mainCamera.transform.position;
    }

  

    public int ApplyCrit(int damage)
    {
        if (Random.value <= critChance)
            return Mathf.RoundToInt(damage * critMultiplier);
        return damage;
    }

    protected void AddBloom()
    {
        currentBloom = Mathf.Min(currentBloom + bloomPerShot, maxBloom);
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
        if (isReloading) return;
        isFiring = true;
        if (fpsController != null)
        {
            fpsController.IsSprinting = false;
            fpsController.SuppressSprintOnShoot(0.3f);
        }
        animator.Play(FireClipName, 2, 0f);
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
        isAiming = false;
        isReloading = true;
        isCocking = false;
        StopRecoil();
        animator.SetBool("IsAiming", false);
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsSprinting", false);
        animator.SetFloat("ReloadSpeed", playerStats != null ? playerStats.reloadSpeed : 1f);
        animator.SetBool("IsReloading", true);
    }

    public void CancelReload()
    {
        if (!isReloading) return;
        isReloading = false;
        if (animator != null)
        {
            animator.SetBool("IsReloading", false);
            animator.SetBool("IsIdle", false);
            if (fpsController != null)
                animator.SetBool("IsSprinting", fpsController.IsSprinting && !fpsController.IsSprintingSuppressed);
        }
    }
}

[System.Serializable]
public class WeaponSound
{
    public string name;
    public AudioClip clip;
}