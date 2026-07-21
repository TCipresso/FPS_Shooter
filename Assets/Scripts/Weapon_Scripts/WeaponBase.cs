using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using Mirror;
using float3 = Unity.Mathematics.float3;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Muzzle")]
    public Transform muzzlePoint;

    [Header("Skin")]
    public Renderer skinRenderer;

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

    [Header("Swarm Hit Detection")]
    public float swarmHitRadius = 0.4f;

    [Header("Ragdoll")]
    public float ragdollForceMultiplier = 1f;

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

    [Header("Accuracy - Hip Fire")]
    public float baseAccuracy = 1f;
    public float bloomPerShot = 0.5f;
    public float maxBloom = 4f;
    public float bloomDecaySpeed = 3f;
    [HideInInspector] public float currentBloom = 0f;

    [Header("Accuracy - ADS")]
    public float adsBloomPerShot = 0.15f;
    public float adsMaxBloom = 1f;

    [Header("ADS")]
    public float adsFOVReduction = 15f;
    public float adsTransitionSpeed = 10f;
    public bool hideCrosshairAds = false;
    [HideInInspector] public bool isAiming = false;

    [Header("Animation")]
    public Animator animator;
    public float walkStopDelay = 0.1f;
    public string FireClipName = "Enter Clip name Here";
    public Animator universalAnimator;

    [HideInInspector] public bool isCocking = false;
    [HideInInspector] public bool isFiring = false;

    public System.Action<WeaponBase, List<Vector3>, List<byte>> onShotFired;

    readonly List<Vector3> shotEndPoints = new List<Vector3>(16);
    readonly List<byte> shotHitTypes = new List<byte>(16);

    float walkStopTimer = 0f;
    float fireResetTime = 0f;

    protected FPSLook fpsLook;
    protected Camera mainCamera;
    protected WeaponRecoil weaponRecoil;
    protected PlayerStats playerStats;
    protected PlayerFpsController fpsController;

    private int level1Damage;
    private float level1Range;
    private float level1Rpm;
    private bool level1Captured = false;

    protected virtual void Awake()
    {
        if (!level1Captured)
        {
            level1Damage = damage;
            level1Range = range;
            level1Rpm = rpm;
            level1Captured = true;
        }

        baseRpm = rpm;
        ResolveOwningPlayerReferences();

        if (fpsLook == null)
            Debug.LogWarning($"[{gameObject.name}] FPSLook not found on owning player.");
        if (mainCamera == null)
            Debug.LogWarning($"[{gameObject.name}] Main Camera not found.");
        if (bulletData == null)
            Debug.LogWarning($"[{gameObject.name}] No BulletDataSO assigned.");
        if (playerStats == null)
            Debug.LogWarning($"[{gameObject.name}] PlayerStats not found on owning player.");
        if (universalAnimator == null)
            universalAnimator = FindUniversalAnimatorInOwnHierarchy();
    }

    // Resolves references from this weapon's own player hierarchy instead of a scene-wide
    // search, so with multiple players in the scene each weapon always binds to its own
    // player's components rather than whichever instance Unity happens to find first.
    void ResolveOwningPlayerReferences()
    {
        if (fpsLook == null)
            fpsLook = GetComponentInParent<FPSLook>();
        if (playerStats == null)
            playerStats = GetComponentInParent<PlayerStats>();
        if (fpsController == null)
            fpsController = GetComponentInParent<PlayerFpsController>();
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    Animator FindUniversalAnimatorInOwnHierarchy()
    {
        Transform root = fpsController != null ? fpsController.transform : transform.root;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "WeaponAnims")
                return all[i].GetComponent<Animator>();
        }

        return null;
    }

    bool IsOwnerLocalPlayer()
    {
        if (fpsController == null) return true;
        if (!NetworkClient.active) return true;
        return fpsController.isLocalPlayer;
    }

    protected virtual void OnEnable()
    {
        isAiming = false;
        isCocking = false;
        isFiring = false;
        currentBloom = 0f;
        walkStopTimer = 0f;

        // Weapons can be enabled well after Awake (equip swap), and on the local player
        // the main camera may not have been activated yet at Awake time - refresh here.
        ResolveOwningPlayerReferences();

        if (universalAnimator == null)
            universalAnimator = FindUniversalAnimatorInOwnHierarchy();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

            animator.SetBool("IsWalking", false);
            animator.SetBool("IsSprinting", false);
            animator.SetBool("IsAiming", false);
            animator.ResetTrigger("Cock");
            animator.Play("Idle", 0, 0f);
        }
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        isAiming = false;
        isCocking = false;
        isFiring = false;
        currentBloom = 0f;
        walkStopTimer = 0f;
    }

    protected virtual void Update()
    {
        if (playerStats != null)
            rpm = baseRpm * playerStats.attackSpeed;

        if (isFiring && Time.time >= fireResetTime)
            isFiring = false;

        if (currentBloom > 0f)
            currentBloom = Mathf.Max(0f, currentBloom - bloomDecaySpeed * Time.deltaTime);

        if (!IsOwnerLocalPlayer()) return;

        if (fpsController != null && animator != null)
        {
            isAiming = fpsController.input.AimHeld;
            currentBloom = Mathf.Min(currentBloom, isAiming ? adsMaxBloom : maxBloom);

            if (isAiming)
                fpsController.IsSprinting = false;

            bool isWalking = !isCocking && fpsController.input.Move.sqrMagnitude > 0.01f;

            if (isWalking)
                walkStopTimer = walkStopDelay;
            else if (walkStopTimer > 0f)
                walkStopTimer -= Time.deltaTime;

            bool showWalking = !isAiming && (isWalking || walkStopTimer > 0f);
            bool showSprinting = !isAiming && fpsController.IsSprinting && !fpsController.IsSprintingSuppressed;

            animator.SetBool("IsWalking", showWalking);
            animator.SetBool("IsSprinting", showSprinting);
            animator.SetBool("IsAiming", isAiming);

            if (universalAnimator != null)
            {
                universalAnimator.SetBool("IsWalking", showWalking);
                universalAnimator.SetBool("IsSprinting", showSprinting);
            }
        }
    }

    public abstract void Shoot();

    public virtual void Reload() { }

    private void FireHitscan(int damage, float range)
    {
        if (bulletData == null) return;

        if (bulletData.isShotgun)
        {
            int pellets = Mathf.Max(1, bulletData.pelletCount);
            int pelletDamage = Mathf.Max(1, damage / pellets);

            for (int i = 0; i < pellets; i++)
            {
                float spreadX, spreadY;

                if (bulletData.flatSpread)
                {
                    float t = pellets > 1 ? (float)i / (pellets - 1) : 0.5f;
                    spreadX = 0f;
                    spreadY = Mathf.Lerp(-bulletData.pelletSpreadAngle, bulletData.pelletSpreadAngle, t);
                    FireHitscanPelletExact(pelletDamage, range, spreadX, spreadY);
                }
                else
                {
                    spreadX = Random.Range(-bulletData.pelletSpreadAngle, bulletData.pelletSpreadAngle);
                    spreadY = Random.Range(-bulletData.pelletSpreadAngle, bulletData.pelletSpreadAngle);
                    FireHitscanPellet(pelletDamage, range, spreadX, spreadY);
                }
            }
        }
        else
        {
            FireHitscanPellet(damage, range, 0f, 0f);
        }
    }

    private void FireHitscanPelletExact(int damage, float range, float spreadX, float spreadY)
    {
        Vector3 origin = GetAimOrigin();
        Quaternion spreadRotation = Quaternion.AngleAxis(spreadY, mainCamera.transform.up)
                                  * Quaternion.AngleAxis(spreadX, mainCamera.transform.right);
        Vector3 direction = spreadRotation * mainCamera.transform.forward;

        Ray ray = new Ray(origin, direction);
        Vector3 endPoint;

        bool didHitWorld = bulletData.hitMask != 0
            ? Physics.Raycast(ray, out RaycastHit hit, range, bulletData.hitMask)
            : Physics.Raycast(ray, out hit, range);

        float searchDistance = didHitWorld ? hit.distance : range;

        bool hitZombie = ZombieDamageBridge.TryFindNearestZombieAlongRay(
            (float3)origin, (float3)direction, searchDistance, swarmHitRadius,
            out Entity zombie, out float3 zombieHitPos);

        if (hitZombie)
        {
            endPoint = (Vector3)zombieHitPos;
            ZombieDamageBridge.DamageZombie(zombie, ApplyCrit(damage));

            if (HitMarkerPool.Instance != null)
                HitMarkerPool.Instance.Spawn(endPoint, false);

            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnZombie(endPoint, -direction);
        }
        else if (didHitWorld)
        {
            endPoint = hit.point;
            SpawnImpactEffect(hit, false);
        }
        else
        {
            endPoint = origin + direction * range;
        }
        SpawnTrail(muzzlePoint.position, endPoint);
        shotEndPoints.Add(endPoint);
        shotHitTypes.Add(hitZombie ? (byte)2 : didHitWorld ? (byte)1 : (byte)0);
    }

    private void FireHitscanPellet(int damage, float range, float spreadX, float spreadY)
    {
        Vector3 direction = GetAimDirection(spreadX, spreadY);
        Vector3 origin = GetAimOrigin();
        Ray ray = new Ray(origin, direction);
        Vector3 endPoint;

        bool didHitWorld = bulletData.hitMask != 0
            ? Physics.Raycast(ray, out RaycastHit hit, range, bulletData.hitMask)
            : Physics.Raycast(ray, out hit, range);

        float searchDistance = didHitWorld ? hit.distance : range;

        bool hitZombie = ZombieDamageBridge.TryFindNearestZombieAlongRay(
            (float3)origin, (float3)direction, searchDistance, swarmHitRadius,
            out Entity zombie, out float3 zombieHitPos);

        if (hitZombie)
        {
            endPoint = (Vector3)zombieHitPos;
            ZombieDamageBridge.DamageZombie(zombie, ApplyCrit(damage));

            if (HitMarkerPool.Instance != null)
                HitMarkerPool.Instance.Spawn(endPoint, false);

            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnZombie(endPoint, -direction);
        }
        else if (didHitWorld)
        {
            endPoint = hit.point;
            SpawnImpactEffect(hit, false);
        }
        else
        {
            endPoint = origin + direction * range;
        }
        SpawnTrail(muzzlePoint.position, endPoint);
        shotEndPoints.Add(endPoint);
        shotHitTypes.Add(hitZombie ? (byte)2 : didHitWorld ? (byte)1 : (byte)0);
    }

    public void ApplyAttackSpeed(float attackSpeed)
    {
        rpm = baseRpm * attackSpeed;
    }

    public bool CanShoot()
    {
        if (isCocking) return false;
        return true;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        isCocking = false;
        isAiming = false;
        isFiring = false;
        currentBloom = 0f;
        walkStopTimer = 0f;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

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

    public void PlaySoundByName(string soundName)
    {
        if (audioSource == null) return;

        for (int i = 0; i < sounds.Count; i++)
        {
            WeaponSound ws = sounds[i];
            if (ws != null && ws.name == soundName)
            {
                if (ws.clip != null)
                    audioSource.PlayOneShot(ws.clip);
                return;
            }
        }

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

        shotEndPoints.Clear();
        shotHitTypes.Clear();

        int scaledDamage = playerStats != null
            ? Mathf.RoundToInt(damage * playerStats.damageMultiplier)
            : damage;

        switch (bulletData.bulletType)
        {
            case BulletType.Hitscan:
                FireHitscan(scaledDamage, range);
                break;
            case BulletType.Projectile:
                FireProjectile(scaledDamage);
                break;
        }

        onShotFired?.Invoke(this, shotEndPoints, shotHitTypes);
    }

    private void FireProjectile(int damage)
    {
        Debug.LogWarning("[WeaponBase] Projectile firing not yet implemented.");
    }

    public void PlayRemoteFireEffects(Vector3[] endpoints, byte[] hitTypes)
    {
        if (!gameObject.activeInHierarchy) return;

        PlayMuzzleFlash();
        EjectCasing();
        PlayFireSound();

        if (animator != null)
            animator.Play(FireClipName, 2, 0f);

        if (endpoints == null) return;

        for (int i = 0; i < endpoints.Length; i++)
        {
            Vector3 end = endpoints[i];
            Vector3 start = muzzlePoint != null ? muzzlePoint.position : end;

            SpawnTrail(start, end);

            byte hitType = hitTypes != null && i < hitTypes.Length ? hitTypes[i] : (byte)0;
            if (hitType == 0) continue;
            if (ImpactEffectPool.Instance == null) continue;

            Vector3 dir = (end - start).sqrMagnitude > 0.0001f
                ? (end - start).normalized
                : transform.forward;

            if (hitType == 2)
                ImpactEffectPool.Instance.SpawnZombie(end, -dir);
            else
                ImpactEffectPool.Instance.SpawnWorld(end, -dir);
        }
    }

    protected void PlayMuzzleFlash()
    {
        if (muzzleFlash == null) return;
        muzzleFlash.transform.position = muzzlePoint.position;
        muzzleFlash.transform.rotation = muzzlePoint.rotation;
        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleFlash.Play();
    }

    public void EjectCasing()
    {
        if (casingEject == null) return;
        casingEject.Play();
    }

    protected void SpawnImpactEffect(RaycastHit hit, bool isZombie)
    {
        if (ImpactEffectPool.Instance == null) return;
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
            weaponRecoil = GetComponentInParent<WeaponRecoil>();
        if (weaponRecoil == null)
            weaponRecoil = FindFirstObjectByType<WeaponRecoil>();

        if (weaponRecoil != null)
            weaponRecoil.LoadValues(kickRotationX, kickRotationY, kickRotationZ,
                kickPositionZ, kickPositionY, kickPositionX);
    }

    protected void ApplyRecoil()
    {
        if (weaponRecoil == null)
            weaponRecoil = GetComponentInParent<WeaponRecoil>();
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
    }

    public void StopRecoil() { }

    public void ApplyLevel(WeaponDefinitionSO def, int level)
    {
        if (def == null) return;

        int clampedLevel = Mathf.Clamp(level, 1, def.maxLevel);

        damage = Mathf.RoundToInt(level1Damage * (1f + def.damageGrowthPerLevel * (clampedLevel - 1)));
        range = level1Range * (1f + def.rangeGrowthPerLevel * (clampedLevel - 1));
        baseRpm = level1Rpm * (1f + def.rpmGrowthPerLevel * (clampedLevel - 1));
        rpm = baseRpm;
    }

    protected Vector3 GetAimDirection(float spreadX, float spreadY)
    {
        if (mainCamera == null) return muzzlePoint.forward;

        float totalX = spreadX + Random.Range(-currentBloom, currentBloom);
        float totalY = spreadY + Random.Range(-currentBloom, currentBloom);

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
        float chance = playerStats != null ? playerStats.critChance : critChance;
        float multiplier = playerStats != null ? playerStats.critMultiplier : critMultiplier;

        if (Random.value <= chance)
            return Mathf.RoundToInt(damage * multiplier);
        return damage;
    }

    protected void AddBloom()
    {
        if (isAiming)
            currentBloom = Mathf.Min(currentBloom + adsBloomPerShot, adsMaxBloom);
        else
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
        isFiring = true;
        if (fpsController != null)
        {
            fpsController.IsSprinting = false;
            fpsController.SuppressSprintOnShoot(0.3f);
        }
        animator.Play(FireClipName, 2, 0f);
        fireResetTime = Time.time + animator.GetCurrentAnimatorStateInfo(0).length;
    }

    protected void TriggerReloadAnimation() { }
    public void CancelReload() { }
    public void ApplyExtraMagazine(int extra) { }
    public void Refill() { }
    public void OnReloadComplete() { }
}

[System.Serializable]
public class WeaponSound
{
    public string name;
    public AudioClip clip;
}