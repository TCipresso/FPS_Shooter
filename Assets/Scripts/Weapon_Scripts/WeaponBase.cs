using UnityEngine;
using System.Collections.Generic;


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

    [Header("Weapon Recoil")]
    public float kickRotationX = 2f;
    public float kickRotationY = 2f;
    public float kickRotationZ = 5f;
    public float kickPositionZ = -0.1f;
    public float kickPositionY = 0.05f;
    public float kickPositionX = 0.02f;

    [Header("Screen Shake")]
    public float shakeMagnitude = 0.05f;
    public float shakeDuration = 0.08f;
    public float shakeFrequency = 30f;

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
    public float maxBloom = 4f;
    public float bloomDecaySpeed = 3f;
    [HideInInspector] public float currentBloom = 0f;

    [Header("Animation")]
    public Animator animator;
    public float walkStopDelay = 0.1f;
    public string FireClipName = "Enter Clip name Here";
    public Animator universalAnimator;

    [HideInInspector] public bool isCocking = false;
    [HideInInspector] public bool isFiring = false;

    public System.Action<WeaponBase, List<Vector3>, List<byte>> onShotFired;
    public System.Action<WeaponBase, Vector3, Vector3> onProjectileFired;

    readonly List<Vector3> shotEndPoints = new List<Vector3>(16);
    readonly List<byte> shotHitTypes = new List<byte>(16);

    const int MaxSwarmHits = 16;
    readonly RaycastHit[] swarmHitBuffer = new RaycastHit[MaxSwarmHits];

    float walkStopTimer = 0f;
    float fireResetTime = 0f;

    protected FPSLook fpsLook;
    protected Camera mainCamera;
    protected WeaponRecoil weaponRecoil;
    protected PlayerStats playerStats;
    protected PlayerFpsController fpsController;
    public PlayerStats OwnerStats => playerStats;

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

    protected virtual void OnEnable()
    {
        isCocking = false;
        isFiring = false;
        currentBloom = 0f;
        walkStopTimer = 0f;

        ResolveOwningPlayerReferences();

        if (universalAnimator == null)
            universalAnimator = FindUniversalAnimatorInOwnHierarchy();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

            animator.SetBool("IsWalking", false);
            animator.ResetTrigger("Cock");
            animator.Play("Idle", 0, 0f);
        }
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
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

        if (fpsController != null && animator != null)
        {
            currentBloom = Mathf.Min(currentBloom, maxBloom);

            bool isWalking = !isCocking && fpsController.input.Move.sqrMagnitude > 0.01f;

            if (isWalking)
                walkStopTimer = walkStopDelay;
            else if (walkStopTimer > 0f)
                walkStopTimer -= Time.deltaTime;

            bool showWalking = isWalking || walkStopTimer > 0f;

            animator.SetBool("IsWalking", showWalking);

            if (universalAnimator != null)
                universalAnimator.SetBool("IsWalking", showWalking);
        }
    }

    public abstract void Shoot();

    public virtual void Reload() { }

    bool TryFindNearestZombieAlongRay(Vector3 origin, Vector3 direction, float maxDistance, out ZombieBase zombie, out HitBox hitBox, out Vector3 hitPoint)
    {
        int hitCount = Physics.SphereCastNonAlloc(origin, swarmHitRadius, direction, swarmHitBuffer, maxDistance);

        ZombieBase closestZombie = null;
        HitBox closestHitBox = null;
        float closestDistance = float.MaxValue;
        Vector3 closestPoint = default;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = swarmHitBuffer[i];

            HitBox candidateHitBox = hit.collider.GetComponentInParent<HitBox>();
            ZombieBase candidate = candidateHitBox != null
                ? candidateHitBox.zombie
                : hit.collider.GetComponentInParent<ZombieBase>();

            if (candidate == null || candidate.IsDead) continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestZombie = candidate;
                closestHitBox = candidateHitBox;
                closestPoint = hit.point;
            }
        }

        zombie = closestZombie;
        hitBox = closestHitBox;
        hitPoint = closestPoint;
        return closestZombie != null;
    }

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
                    FireHitscanPellet(pelletDamage, range, spreadX, spreadY, true);
                }
                else
                {
                    spreadX = Random.Range(-bulletData.pelletSpreadAngle, bulletData.pelletSpreadAngle);
                    spreadY = Random.Range(-bulletData.pelletSpreadAngle, bulletData.pelletSpreadAngle);
                    FireHitscanPellet(pelletDamage, range, spreadX, spreadY, false);
                }
            }
        }
        else
        {
            FireHitscanPellet(damage, range, 0f, 0f, false);
        }
    }

    private void FireHitscanPellet(int damage, float range, float spreadX, float spreadY, bool exactDirection)
    {
        Vector3 origin = GetAimOrigin();
        Vector3 direction;

        if (exactDirection)
        {
            Quaternion spreadRotation = Quaternion.AngleAxis(spreadY, mainCamera.transform.up)
                                      * Quaternion.AngleAxis(spreadX, mainCamera.transform.right);
            direction = spreadRotation * mainCamera.transform.forward;
        }
        else
        {
            direction = GetAimDirection(spreadX, spreadY);
        }

        Ray ray = new Ray(origin, direction);
        Vector3 endPoint;

        bool didHitWorld = bulletData.hitMask != 0
            ? Physics.Raycast(ray, out RaycastHit hit, range, bulletData.hitMask)
            : Physics.Raycast(ray, out hit, range);

        float searchDistance = didHitWorld ? hit.distance : range;

        bool hitZombie = TryFindNearestZombieAlongRay(origin, direction, searchDistance, out ZombieBase zombie, out HitBox hitBox, out Vector3 zombieHitPos);

        if (hitZombie)
        {
            endPoint = zombieHitPos;

            if (hitBox != null)
            {
                hitBox.TakeDamageWithHitPoint(damage, playerStats, this, endPoint, 1f, -direction, ragdollForceMultiplier);
            }
            else
            {
                zombie.TakeDamage(ApplyCrit(damage), playerStats, 1f, -direction, ragdollForceMultiplier);
                zombie.hitFlash?.Flash(false);

                if (HitMarkerPool.Instance != null)
                    HitMarkerPool.Instance.Spawn(endPoint, false);
            }

            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnZombie(endPoint, -direction);
        }
        else if (didHitWorld)
        {
            endPoint = hit.point;
            SpawnImpactEffect(hit, false);

            SandboxSpawner spawner = hit.collider.GetComponentInParent<SandboxSpawner>();
            if (spawner != null)
                spawner.TriggerSpawn();
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
        isFiring = false;
        currentBloom = 0f;
        walkStopTimer = 0f;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

            animator.SetBool("IsWalking", false);
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
        ApplyScreenShake();
    }

    protected void ApplyScreenShake()
    {
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(shakeMagnitude, shakeDuration, shakeFrequency);
    }

    private void FireProjectile(int damage)
    {
        if (bulletData == null || bulletData.projectilePrefab == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No projectilePrefab assigned on BulletDataSO.");
            return;
        }

        Vector3 origin;
        Vector3 direction = GetProjectileLaunch(out origin);

        SpawnProjectileVisual(origin, direction, damage, true);
        onProjectileFired?.Invoke(this, origin, direction);
    }

    Vector3 GetProjectileLaunch(out Vector3 origin)
    {
        origin = muzzlePoint != null ? muzzlePoint.position : GetAimOrigin();

        Vector3 camOrigin = GetAimOrigin();
        Vector3 camDir = GetAimDirection(0f, 0f);
        LayerMask mask = bulletData.hitMask != 0 ? bulletData.hitMask : (LayerMask)(~0);

        Vector3 aimPoint = Physics.Raycast(camOrigin, camDir, out RaycastHit hit, range, mask)
            ? hit.point
            : camOrigin + camDir * range;

        float muzzleForwardOffset = Vector3.Dot(origin - camOrigin, camDir);
        float aimPointForwardDistance = Vector3.Dot(aimPoint - camOrigin, camDir);

        if (aimPointForwardDistance <= muzzleForwardOffset + 0.75f)
            return camDir;

        Vector3 dir = aimPoint - origin;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : camDir;
    }

    void SpawnProjectileVisual(Vector3 origin, Vector3 direction, int damage, bool applyDamage)
    {
        if (ProjectilePool.Instance == null || bulletData == null || bulletData.projectilePrefab == null)
            return;

        ProjectileBase p = ProjectilePool.Instance.Get(
            bulletData.projectilePrefab, origin, Quaternion.LookRotation(direction));
        if (p == null) return;

        float speed = bulletData.projectileSpeed;
        float life = speed > 0.01f ? range / speed : 3f;

        p.Launch(origin, direction, speed, bulletData.projectileGravityScale,
            life, damage, applyDamage, this, bulletData, swarmHitRadius);
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