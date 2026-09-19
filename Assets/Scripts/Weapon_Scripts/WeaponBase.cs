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
    [Header("Weapon Definition")]
    public WeaponDefinitionSO weaponDefinition;
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireSound;
    public List<WeaponSound> sounds = new List<WeaponSound>();
    [Header("Weapon Recoil")]
    public float kickRotationX = 2f;
    public float kickRotationY = 2f;
    public float kickRotationZ = 5f;
    public float kickPositionZ = -0.1f;
    public float kickPositionY = 0.05f;
    public float kickPositionX = 0.02f;
    [Header("Shot FOV")]
    public float shotFOVImpulse = 3f;
    [HideInInspector] public float currentBloom = 0f;
    [Header("Animation")]
    public Animator animator;
    public float walkStopDelay = 0.1f;
    public string FireClipName = "Enter Clip name Here";
    public Animator universalAnimator;
    [HideInInspector] public bool isCocking = false;
    [HideInInspector] public bool isFiring = false;
    [HideInInspector] public bool isReloading = false;
    [HideInInspector] public int currentAmmo = 0;
    [HideInInspector] public bool isLowered = false;
    [Header("Melee")]
    public string meleeAttackClipName = "Enter Melee Attack Clip Name Here";
    public string meleeAttackAltClipName = "Enter Alt Melee Attack Clip Name Here";
    public float comboResetWindow = 0.6f;
    public List<AudioClip> meleeSwingSounds = new List<AudioClip>();
    int lastMeleeSoundIndex = -1;
    public string parryClipName = "Enter Parry Clip Name Here";
    [HideInInspector] public bool isMeleeAttacking = false;
    [HideInInspector] public bool isMeleeComboActive = false;
    [HideInInspector] public bool isParrying = false;
    int meleeComboIndex = 0;
    bool meleeAttackQueued = false;
    float comboExpireTime = 0f;
    float meleeAttackResetTime = 0f;
    float parryResetTime = 0f;

    public System.Action<WeaponBase, List<Vector3>, List<byte>> onShotFired;
    public System.Action<WeaponBase, Vector3, Vector3> onProjectileFired;
    readonly List<Vector3> shotEndPoints = new List<Vector3>(16);
    readonly List<byte> shotHitTypes = new List<byte>(16);
    const int MaxSwarmHits = 16;
    readonly RaycastHit[] swarmHitBuffer = new RaycastHit[MaxSwarmHits];
    float walkStopTimer = 0f;
    float fireResetTime = 0f;
    float currentRpm;
    Material[] cachedOriginalMaterials;
    bool originalMaterialsCached = false;
    static MaterialPropertyBlock sharedSkinPropertyBlock;
    protected FPSLook fpsLook;
    protected Camera mainCamera;
    protected WeaponRecoil weaponRecoil;
    protected PlayerStats playerStats;
    protected PlayerFpsController fpsController;

    public PlayerStats OwnerStats => playerStats;
    public bool IsMelee => weaponDefinition != null && weaponDefinition.isMelee;
    public int damage => weaponDefinition != null ? weaponDefinition.damage : 0;
    public bool isAutomatic => weaponDefinition != null && weaponDefinition.isAutomatic;
    public float critMultiplier => (weaponDefinition != null ? weaponDefinition.critMultiplier : 1f) + (playerStats != null ? playerStats.critMultiplier : 0f);
    public float critChance => (weaponDefinition != null ? weaponDefinition.critChance : 0f) + (playerStats != null ? playerStats.critChance : 0f);
    public float FireInterval => 60f / Mathf.Max(currentRpm, 0.01f);
    public int MaxAmmo => weaponDefinition != null ? weaponDefinition.magazineSize : 0;
    public bool IsReloading => isReloading;
    public float ReloadSpeed => weaponDefinition != null ? weaponDefinition.reloadSpeed : 1f;

    protected virtual void Awake()
    {
        ResolveOwningPlayerReferences();
        if (fpsLook == null)
            Debug.LogWarning($"[{gameObject.name}] FPSLook not found on owning player.");
        if (mainCamera == null)
            Debug.LogWarning($"[{gameObject.name}] Main Camera not found.");
        if (weaponDefinition == null)
            Debug.LogWarning($"[{gameObject.name}] No WeaponDefinitionSO assigned.");
        if (playerStats == null)
            Debug.LogWarning($"[{gameObject.name}] PlayerStats not found on owning player.");
        if (universalAnimator == null)
            universalAnimator = FindUniversalAnimatorInOwnHierarchy();

        // Initialize ammo
        currentAmmo = MaxAmmo;
    }

    void ResolveOwningPlayerReferences()
    {
        if (fpsLook == null)
            fpsLook = GetComponentInParent<FPSLook>();
        if (playerStats == null)
            playerStats = PlayerStats.Instance;
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
        isReloading = false;
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
            animator.SetBool("IsReloading", false);
            animator.SetFloat("ReloadSpeed", ReloadSpeed);
            animator.ResetTrigger("Cock");
            animator.ResetTrigger("Reload");
            animator.Play("Idle", 0, 0f);
        }
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        isCocking = false;
        isFiring = false;
        isReloading = false;
        currentBloom = 0f;
        walkStopTimer = 0f;
        isLowered = false;
        isMeleeAttacking = false;
        isMeleeComboActive = false;
        isParrying = false;
        meleeAttackQueued = false;
        meleeComboIndex = 0;
        comboExpireTime = 0f;
    }

    protected virtual void Update()
    {
        float baseRpmValue = weaponDefinition != null ? weaponDefinition.rpm : 1f;
        currentRpm = playerStats != null ? baseRpmValue * playerStats.attackSpeed : baseRpmValue;

        if (isFiring && Time.time >= fireResetTime)
            isFiring = false;

        if (isMeleeAttacking && Time.time >= meleeAttackResetTime)
        {
            isMeleeAttacking = false;
            if (meleeAttackQueued)
                StartMeleeSwing();
            else
                isMeleeComboActive = false;
        }

        if (isParrying && Time.time >= parryResetTime)
            isParrying = false;

        if (currentBloom > 0f)
        {
            float decay = weaponDefinition != null ? weaponDefinition.bloomDecaySpeed : 0f;
            currentBloom = Mathf.Max(0f, currentBloom - decay * Time.deltaTime);
        }

        if (fpsController != null && animator != null)
        {
            float maxBloom = weaponDefinition != null ? weaponDefinition.maxBloom : 0f;
            currentBloom = Mathf.Min(currentBloom, maxBloom);
            bool isWalking = !isCocking && !IsReloading && fpsController.input.Move.sqrMagnitude > 0.01f;
            if (isWalking)
                walkStopTimer = walkStopDelay;
            else if (walkStopTimer > 0f)
                walkStopTimer -= Time.deltaTime;
            bool showWalking = isWalking || walkStopTimer > 0f;
            animator.SetBool("IsWalking", showWalking);
            animator.SetBool("IsReloading", IsReloading);
            if (universalAnimator != null)
            {
                universalAnimator.SetBool("IsWalking", showWalking);
                universalAnimator.SetBool("IsReloading", IsReloading);
            }
        }
    }

    public abstract void Shoot();

    public virtual void Reload()
    {
    }

    public virtual void OnReloadComplete()
    {
        CancelReload();
    }
    public virtual void CancelReload()
    {
        if (!isReloading) return;

        isReloading = false;

        if (animator != null)
        {
            animator.SetBool("IsReloading", false);
            animator.ResetTrigger("Reload");
        }

        if (universalAnimator != null)
        {
            universalAnimator.SetBool("IsReloading", false);
        }
    }

    public bool TryUseAmmo(int amount = 1)
    {
        if (currentAmmo < amount) return false;
        currentAmmo -= amount;
        return true;
    }

    public bool HasAmmo()
    {
        return currentAmmo > 0;
    }

    public void Refill()
    {
    }

    public void ApplyExtraMagazine(int extra)
    {
        if (weaponDefinition != null)
        {
            weaponDefinition.magazineSize += extra;
            currentAmmo = Mathf.Clamp(currentAmmo, 0, MaxAmmo);
        }
    }

    bool TryFindNearestZombieAlongRay(Vector3 origin, Vector3 direction, float maxDistance, out ZombieBase zombie, out HitBox hitBox, out Vector3 hitPoint)
    {
        float radius = weaponDefinition != null ? weaponDefinition.swarmHitRadius : 0.4f;
        int hitCount = Physics.SphereCastNonAlloc(origin, radius, direction, swarmHitBuffer, maxDistance);
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

    // ============= FIRING METHODS =============

    private void FireHitscan(int damage, float range)
    {
        if (weaponDefinition == null) return;

        // Get actual pellet count (rounded down)
        int pellets = weaponDefinition.GetActualPelletCount();

        for (int i = 0; i < pellets; i++)
        {
            float spreadX = 0f;
            float spreadY = 0f;

            // ONLY apply spread if there are 2 or more pellets
            if (pellets > 1)
            {
                if (weaponDefinition.flatSpread)
                {
                    // Flat line spread
                    float t = (float)i / (pellets - 1);
                    spreadX = 0f;
                    spreadY = Mathf.Lerp(-weaponDefinition.pelletSpreadAngle, weaponDefinition.pelletSpreadAngle, t);
                }
                else
                {
                    // Circular spread
                    spreadX = Random.Range(-weaponDefinition.pelletSpreadAngle, weaponDefinition.pelletSpreadAngle);
                    spreadY = Random.Range(-weaponDefinition.pelletSpreadAngle, weaponDefinition.pelletSpreadAngle);
                }
            }
            // If only 1 pellet, spreadX and spreadY stay 0 (perfect accuracy)

            FireHitscanPellet(damage, range, spreadX, spreadY, pellets > 1 && weaponDefinition.flatSpread);
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
        bool didHitWorld = weaponDefinition.hitMask != 0
            ? Physics.Raycast(ray, out RaycastHit hit, range, weaponDefinition.hitMask)
            : Physics.Raycast(ray, out hit, range);
        float searchDistance = didHitWorld ? hit.distance : range;
        bool hitZombie = TryFindNearestZombieAlongRay(origin, direction, searchDistance, out ZombieBase zombie, out HitBox hitBox, out Vector3 zombieHitPos);
        if (hitZombie)
        {
            endPoint = zombieHitPos;
            if (hitBox != null)
            {
                hitBox.TakeDamageWithHitPoint(damage, playerStats, this, endPoint, 1f, -direction, 1f);
            }
            else
            {
                zombie.TakeDamage(ApplyCrit(damage), playerStats, 1f, -direction, 1f, "", this);
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

    private void FireProjectile(int damage)
    {
        if (weaponDefinition == null || weaponDefinition.projectilePrefab == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No projectilePrefab assigned on WeaponDefinitionSO.");
            return;
        }

        // Get actual pellet count (rounded down)
        int pellets = weaponDefinition.GetActualPelletCount();

        for (int i = 0; i < pellets; i++)
        {
            float spreadX = 0f;
            float spreadY = 0f;

            // ONLY apply spread if there are 2 or more pellets
            if (pellets > 1)
            {
                if (weaponDefinition.flatSpread)
                {
                    // Flat line spread
                    float t = (float)i / (pellets - 1);
                    spreadX = 0f;
                    spreadY = Mathf.Lerp(-weaponDefinition.pelletSpreadAngle, weaponDefinition.pelletSpreadAngle, t);
                }
                else
                {
                    // Circular spread
                    spreadX = Random.Range(-weaponDefinition.pelletSpreadAngle, weaponDefinition.pelletSpreadAngle);
                    spreadY = Random.Range(-weaponDefinition.pelletSpreadAngle, weaponDefinition.pelletSpreadAngle);
                }
            }
            // If only 1 pellet, spreadX and spreadY stay 0 (perfect accuracy)

            Vector3 origin;
            Vector3 direction = GetProjectileLaunch(out origin, spreadX, spreadY);
            SpawnProjectileVisual(origin, direction, damage, true);
            onProjectileFired?.Invoke(this, origin, direction);
        }
    }

    Vector3 GetProjectileLaunch(out Vector3 origin, float spreadX = 0f, float spreadY = 0f)
    {
        origin = muzzlePoint != null ? muzzlePoint.position : GetAimOrigin();
        Vector3 camOrigin = GetAimOrigin();
        Vector3 camDir = GetAimDirection(spreadX, spreadY);
        LayerMask mask = weaponDefinition.hitMask != 0 ? weaponDefinition.hitMask : (LayerMask)(~0);
        Vector3 aimPoint = Physics.Raycast(camOrigin, camDir, out RaycastHit hit, weaponDefinition.range, mask)
            ? hit.point
            : camOrigin + camDir * weaponDefinition.range;
        float muzzleForwardOffset = Vector3.Dot(origin - camOrigin, camDir);
        float aimPointForwardDistance = Vector3.Dot(aimPoint - camOrigin, camDir);
        if (aimPointForwardDistance <= muzzleForwardOffset + 0.75f)
            return camDir;
        Vector3 dir = aimPoint - origin;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : camDir;
    }

    void SpawnProjectileVisual(Vector3 origin, Vector3 direction, int damage, bool applyDamage)
    {
        if (ProjectilePool.Instance == null || weaponDefinition == null || weaponDefinition.projectilePrefab == null)
            return;
        ProjectileBase p = ProjectilePool.Instance.Get(
            weaponDefinition.projectilePrefab, origin, Quaternion.LookRotation(direction));
        if (p == null) return;
        float speed = weaponDefinition.projectileSpeed;
        float life = speed > 0.01f ? weaponDefinition.range / speed : 3f;
        p.Launch(origin, direction, speed, weaponDefinition.projectileGravityScale,
            life, damage, applyDamage, this, weaponDefinition, weaponDefinition.swarmHitRadius);
    }

    public void ApplyAttackSpeed(float attackSpeed)
    {
        float baseRpmValue = weaponDefinition != null ? weaponDefinition.rpm : 1f;
        currentRpm = baseRpmValue * attackSpeed;
    }

    public bool CanShoot()
    {
        if (isLowered) return false;
        if (isCocking) return false;
        if (IsReloading) return false;
        if (!HasAmmo()) return false;
        return true;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        isCocking = false;
        isFiring = false;
        isReloading = false;
        currentBloom = 0f;
        walkStopTimer = 0f;
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsReloading", false);
            animator.SetFloat("ReloadSpeed", ReloadSpeed);
            animator.ResetTrigger("Cock");
            animator.ResetTrigger("Reload");
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
        if (weaponDefinition == null) return;

        if (!TryUseAmmo())
            return;

        shotEndPoints.Clear();
        shotHitTypes.Clear();
        int scaledDamage = playerStats != null
            ? Mathf.RoundToInt(damage * playerStats.damageMultiplier)
            : damage;

        switch (weaponDefinition.bulletType)
        {
            case BulletType.Hitscan:
                FireHitscan(scaledDamage, weaponDefinition.range);
                break;
            case BulletType.Projectile:
                FireProjectile(scaledDamage);
                break;
        }

        onShotFired?.Invoke(this, shotEndPoints, shotHitTypes);
        ApplyShotFOV();
        AddBloom();

    }

    protected void ApplyShotFOV()
    {
        if (fpsLook != null)
            fpsLook.AddShotFOV(shotFOVImpulse);
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
        if (weaponDefinition == null || BulletPool.Instance == null) return;
        GameObject obj = BulletPool.Instance.Get(weaponDefinition.trailPoolKey, start, Quaternion.identity);
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

    public void ApplyLevel(WeaponDefinitionSO def)
    {
        if (def == null) return;
        weaponDefinition = def;
        RefreshWeaponSkin();
    }

    public void RefreshWeaponSkin()
    {
        if (skinRenderer == null || weaponDefinition == null) return;
        if (!originalMaterialsCached)
        {
            cachedOriginalMaterials = (Material[])skinRenderer.sharedMaterials.Clone();
            originalMaterialsCached = true;
        }
        if (weaponDefinition.level <= 1 || weaponDefinition.packedMaterial == null)
        {
            skinRenderer.sharedMaterials = cachedOriginalMaterials;
            skinRenderer.SetPropertyBlock(null);
            return;
        }
        int slotCount = cachedOriginalMaterials.Length;
        Material[] packedSet = new Material[slotCount];
        for (int i = 0; i < slotCount; i++)
            packedSet[i] = weaponDefinition.packedMaterial;
        skinRenderer.sharedMaterials = packedSet;
        float hue = Mathf.Repeat((weaponDefinition.level - 1) / Mathf.Max(1f, weaponDefinition.tintHueCycleLength), 1f);
        Color tint = Color.HSVToRGB(hue, weaponDefinition.tintSaturation, weaponDefinition.tintValue);
        if (sharedSkinPropertyBlock == null)
            sharedSkinPropertyBlock = new MaterialPropertyBlock();
        skinRenderer.GetPropertyBlock(sharedSkinPropertyBlock);
        sharedSkinPropertyBlock.SetColor(weaponDefinition.tintPropertyName, tint);
        skinRenderer.SetPropertyBlock(sharedSkinPropertyBlock);
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
        if (Random.value <= critChance)
            return Mathf.RoundToInt(damage * critMultiplier);
        return damage;
    }

    protected void AddBloom()
    {
        float bloomAmount = weaponDefinition != null ? weaponDefinition.bloomPerShot : 0f;
        float cap = weaponDefinition != null ? weaponDefinition.maxBloom : 0f;
        currentBloom = Mathf.Min(currentBloom + bloomAmount, cap);
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

    public virtual void PlayMeleeAttack()
    {
        if (!IsMelee) return;
        if (isParrying) return;
        if (animator == null) return;

        if (isMeleeAttacking)
        {
            // Already mid-swing: remember the request and chain it the instant
            // this swing ends, instead of dropping the input.
            meleeAttackQueued = true;
            return;
        }

        StartMeleeSwing();
    }

    void StartMeleeSwing()
    {
        if (Time.time > comboExpireTime)
            meleeComboIndex = 0;

        string clip = meleeComboIndex == 0 ? meleeAttackClipName : meleeAttackAltClipName;

        isMeleeAttacking = true;
        isMeleeComboActive = true;
        meleeAttackQueued = false;
        animator.Play(clip, 1, 0f);
        animator.Update(0f);
        meleeAttackResetTime = Time.time + animator.GetCurrentAnimatorStateInfo(1).length;
        comboExpireTime = meleeAttackResetTime + comboResetWindow;
        meleeComboIndex = 1 - meleeComboIndex;

        if (universalAnimator != null)
            universalAnimator.Play(clip, 1, 0f);

        PlayRandomMeleeSound();
    }

    void PlayRandomMeleeSound()
    {
        if (audioSource == null || meleeSwingSounds.Count == 0) return;

        int index = 0;
        if (meleeSwingSounds.Count > 1)
        {
            do
            {
                index = Random.Range(0, meleeSwingSounds.Count);
            } while (index == lastMeleeSoundIndex);
        }
        lastMeleeSoundIndex = index;
        audioSource.PlayOneShot(meleeSwingSounds[index]);
    }

    public virtual void PlayParry()
    {
        if (!IsMelee) return;
        if (isMeleeAttacking || isParrying) return;
        if (animator == null) return;
        isParrying = true;
        animator.Play(parryClipName, 2, 0f);
        animator.Update(0f);
        parryResetTime = Time.time + animator.GetCurrentAnimatorStateInfo(2).length;
        if (universalAnimator != null)
            universalAnimator.Play(parryClipName, 2, 0f);
    }
}

[System.Serializable]
public class WeaponSound
{
    public string name;
    public AudioClip clip;
}
