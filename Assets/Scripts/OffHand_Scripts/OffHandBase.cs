using UnityEngine;

public abstract class OffHandBase : MonoBehaviour
{
    [Header("Durability")]
    public int maxHealth = 5;
    [HideInInspector] public int currentHealth;

    [Header("Melee")]
    public int meleeDamage = 15;
    public float meleeRange = 2f;
    public float meleeRadius = 0.4f;
    public float meleeCooldown = 0.5f;

    [Header("Animation")]
    public Animator animator;
    public string meleeTriggerName = "Melee";

    [Header("Melee Screen Shake")]
    public float meleeShakeMagnitude = 0.08f;
    public float meleeShakeDuration = 0.1f;
    public float meleeShakeFrequency = 30f;

    [HideInInspector] public bool isSwinging = false;

    const int MaxMeleeHits = 8;
    static readonly RaycastHit[] meleeHitBuffer = new RaycastHit[MaxMeleeHits];

    float nextMeleeTime = 0f;

    protected PlayerStats playerStats;
    protected PlayerFpsController fpsController;
    protected WeaponInventory weaponInventory;
    protected Camera mainCamera;
    public PlayerStats OwnerStats => playerStats;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        ResolveOwningPlayerReferences();
    }

    protected virtual void OnEnable()
    {
        ResolveOwningPlayerReferences();
    }

    void ResolveOwningPlayerReferences()
    {
        if (playerStats == null)
            playerStats = GetComponentInParent<PlayerStats>();
        if (fpsController == null)
            fpsController = GetComponentInParent<PlayerFpsController>();
        if (weaponInventory == null)
            weaponInventory = GetComponentInParent<WeaponInventory>();
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public virtual void OnEquip()
    {
        Debug.Log($"[OffHandBase] {gameObject.name} equipped.");
    }

    public virtual void OnUnequip()
    {
        Debug.Log($"[OffHandBase] {gameObject.name} unequipped.");
    }

    public virtual bool CanMelee()
    {
        return !isSwinging && Time.time >= nextMeleeTime;
    }

    public virtual void Melee()
    {
        if (!CanMelee()) return;

        isSwinging = true;
        nextMeleeTime = Time.time + meleeCooldown;

        TriggerMeleeAnimation();

        if (TryFindMeleeTarget(out ZombieBase zombie, out HitBox hitBox, out Vector3 hitPoint, out Vector3 hitNormal))
        {
            if (hitBox != null)
            {
                hitBox.TakeDamageWithHitPoint(meleeDamage, playerStats, null, hitPoint, 1f, -hitNormal, 1f);
            }
            else
            {
                zombie.TakeDamage(meleeDamage, playerStats, 1f, -hitNormal, 1f);
                zombie.hitFlash?.Flash(false);

                if (HitMarkerPool.Instance != null)
                    HitMarkerPool.Instance.Spawn(hitPoint, false);
            }

            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnZombie(hitPoint, hitNormal);

            ConsumeDurability();
        }
    }

    bool TryFindMeleeTarget(out ZombieBase zombie, out HitBox hitBox, out Vector3 hitPoint, out Vector3 hitNormal)
    {
        zombie = null;
        hitBox = null;
        hitPoint = default;
        hitNormal = default;

        if (mainCamera == null) return false;

        Vector3 origin = mainCamera.transform.position;
        Vector3 direction = mainCamera.transform.forward;

        int hitCount = Physics.SphereCastNonAlloc(origin, meleeRadius, direction, meleeHitBuffer, meleeRange);

        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = meleeHitBuffer[i];

            HitBox candidateHitBox = hit.collider.GetComponentInParent<HitBox>();
            ZombieBase candidate = candidateHitBox != null
                ? candidateHitBox.zombie
                : hit.collider.GetComponentInParent<ZombieBase>();

            if (candidate == null || candidate.IsDead) continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                zombie = candidate;
                hitBox = candidateHitBox;
                hitPoint = hit.point;
                hitNormal = -direction;
            }
        }

        return zombie != null;
    }

    void ConsumeDurability()
    {
        currentHealth--;

        if (currentHealth <= 0)
            Break();
    }

    protected virtual void Break()
    {
        Debug.Log($"[OffHandBase] {gameObject.name} broke.");

        //if (weaponInventory != null)
           // weaponInventory.UnequipOffHand();
    }

    protected void TriggerMeleeAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger(meleeTriggerName);
    }

    public void ApplyMeleeScreenShake()
    {
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(meleeShakeMagnitude, meleeShakeDuration, meleeShakeFrequency);
    }

    protected virtual void Update()
    {
        if (isSwinging && Time.time >= nextMeleeTime)
            isSwinging = false;
    }

    public virtual void OnMeleeAnimationComplete()
    {
        isSwinging = false;
    }
}