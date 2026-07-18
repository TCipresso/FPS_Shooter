using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public abstract class ZombieBase : MonoBehaviour
{
    public enum ZombieState { Idle, Engage, Attack, FollowingPath }

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Gold")]
    public int goldBounty = 100;

    [Header("Movement")]
    public float moveSpeed = 3.5f;

    [Header("Attack")]
    public int attackDamage = 25;
    public float attackRange = 1.8f;
    public float attackCooldown = 1.2f;
    public float hitFrameRange = 2.2f;

    [Header("Detection")]
    public float engageRange = 15f;

    [Header("AI Tick")]
    public float aiTickInterval = 0.1f;
    float nextAiTick;

    [Header("Climbing")]
    public LayerMask groundLayer;
    public bool canClimb = true;
    public float climbSpeed = 2f;
    public Vector3 rayOffset = new Vector3(0f, -0.9f, 0f);
    public float rayLength = 0.6f;
    public float launchForce = 8f;
    bool wasClimbing = false;

    [Header("Path Following")]
    public float pathMoveSpeed = 6f;
    public float waypointReachDistance = 0.5f;
    List<Transform> currentPath;
    int pathIndex = 0;

    [Header("Identity")]
    public string enemyId;

    [Header("Animation")]
    public ZombieFlipbook flipbook;

    [Header("Cached References")]
    public ZombieHitFlash hitFlash;

    [Header("Debug")]
    public bool verboseLogging = false;

    [Header("Attack Failsafe")]
    [Tooltip("If OnAttackComplete hasn't fired within this many seconds of entering Attack state, force the zombie out of Attack anyway. Prevents a permanent softlock if the flipbook attack frames are missing/misconfigured.")]
    public float attackFailsafeDuration = 2f;
    float attackStateEnteredTime = -1f;

    public ZombieState State { get; private set; } = ZombieState.Idle;

    public event System.Action OnDeath;

    protected Rigidbody rb;
    CapsuleCollider col;
    protected Transform player;
    protected PlayerStats playerStats;
    protected float lastAttackTime;
    protected bool isDead = false;

    public bool IsDead => isDead;

    float attackCooldownTimer = 0f;

    float engageRangeSqr;
    float attackRangeSqr;
    float hitFrameRangeSqr;
    float waypointReachDistanceSqr;

    Dictionary<PlayerStats, int> damageContributors = new Dictionary<PlayerStats, int>();
    Dictionary<PlayerStats, float> goldMultipliers = new Dictionary<PlayerStats, float>();
    int totalDamageDealt = 0;

    bool pendingKinematicRelease = false;

    static PlayerStats sharedPlayer;

    static PlayerStats ResolvePlayer()
    {
        if (sharedPlayer == null)
            sharedPlayer = FindFirstObjectByType<PlayerStats>();
        return sharedPlayer;
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        if (hitFlash == null)
            hitFlash = GetComponent<ZombieHitFlash>();

        if (flipbook == null)
            flipbook = GetComponentInChildren<ZombieFlipbook>();

        CacheSquaredRanges();
        nextAiTick = Time.time + Random.value * aiTickInterval;
    }

    void CacheSquaredRanges()
    {
        engageRangeSqr = engageRange * engageRange;
        attackRangeSqr = attackRange * attackRange;
        hitFrameRangeSqr = hitFrameRange * hitFrameRange;
        waypointReachDistanceSqr = waypointReachDistance * waypointReachDistance;
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        playerStats = ResolvePlayer();
        if (playerStats != null)
            player = playerStats.transform;
    }

    public void ClearDeathListeners() => OnDeath = null;

    public void SetPath(List<Transform> path)
    {
        currentPath = path;
        pathIndex = 0;
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        if (State == ZombieState.Attack)
        {
            if (attackStateEnteredTime >= 0f && Time.time - attackStateEnteredTime >= attackFailsafeDuration)
            {
                if (verboseLogging)
                    Debug.LogWarning($"[{gameObject.name}] Attack failsafe triggered - OnAttackComplete never fired (missing/broken flipbook attack setup). Forcing state recovery.");
                OnAttackComplete();
            }
            return;
        }

        if (State == ZombieState.FollowingPath) return;

        if (Time.time < nextAiTick) return;
        nextAiTick = Time.time + aiTickInterval;

        float sqrDist = player != null ? (transform.position - player.position).sqrMagnitude : float.MaxValue;

        switch (State)
        {
            case ZombieState.Idle:
                if (sqrDist <= engageRangeSqr)
                    SetState(ZombieState.Engage);
                break;

            case ZombieState.Engage:
                if (sqrDist <= attackRangeSqr && attackCooldownTimer <= 0f)
                    SetState(ZombieState.Attack);
                break;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (pendingKinematicRelease)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            pendingKinematicRelease = false;
            return;
        }

        if (isDead || player == null) return;
        if (rb.isKinematic) return;

        if (State == ZombieState.FollowingPath)
        {
            FollowPath();
            return;
        }

        if (State == ZombieState.Engage)
            GruntMove();
    }

    protected void SetState(ZombieState newState)
    {
        if (verboseLogging)
            Debug.Log($"[{gameObject.name}] SetState: {State} -> {newState} at t={Time.time:F2}");
        State = newState;

        switch (newState)
        {
            case ZombieState.Idle:
                rb.linearVelocity = Vector3.zero;
                flipbook?.SetWalking(false);
                break;

            case ZombieState.Engage:
                flipbook?.SetWalking(true);
                break;

            case ZombieState.Attack:
                rb.linearVelocity = Vector3.zero;
                flipbook?.TriggerAttack();
                attackStateEnteredTime = Time.time;
                break;
        }
    }

    public virtual void OnHitFrame()
    {
        if (player == null || playerStats == null) return;
        float sqrDist = (transform.position - player.position).sqrMagnitude;
        if (sqrDist <= hitFrameRangeSqr)
            playerStats.TakeDamage(attackDamage);
    }

    public virtual void OnAttackComplete()
    {
        attackStateEnteredTime = -1f;
        attackCooldownTimer = attackCooldown;
        float sqrDist = player != null ? (transform.position - player.position).sqrMagnitude : float.MaxValue;
        SetState(sqrDist <= engageRangeSqr ? ZombieState.Engage : ZombieState.Idle);
    }

    void GruntMove()
    {
        MoveToward(player.position, moveSpeed);
    }

    void FollowPath()
    {
        if (currentPath == null || pathIndex >= currentPath.Count)
        {
            float sqrDist = player != null ? (transform.position - player.position).sqrMagnitude : float.MaxValue;
            State = sqrDist <= engageRangeSqr ? ZombieState.Engage : ZombieState.Idle;
            flipbook?.SetWalking(true);
            return;
        }

        Transform target = currentPath[pathIndex];
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= waypointReachDistanceSqr)
        {
            pathIndex++;
            return;
        }

        MoveToward(target.position, pathMoveSpeed);
    }

    void MoveToward(Vector3 targetPosition, float speed)
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(dir);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));

        Vector3 v = rb.linearVelocity;
        v.x = 0f;
        v.z = 0f;
        rb.linearVelocity = v;

        Vector3 rayStart = transform.position + transform.TransformVector(rayOffset);
        bool hitWall = Physics.Raycast(rayStart, dir, rayLength, groundLayer);

        if (hitWall) rb.linearVelocity = Vector3.zero;

        if (wasClimbing && !hitWall)
            rb.linearVelocity = (dir + Vector3.up).normalized * launchForce;

        wasClimbing = hitWall;

        Vector3 move = hitWall ? Vector3.up * climbSpeed : dir * speed;
        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);
    }

    public virtual void TakeDamage(int amount, PlayerStats dealer, float weaponMultiplier = 1f, Vector3 hitDirection = default, float ragdollForceMultiplier = 1f, string hitBone = "")
    {
        if (isDead) return;

        int actualDamage = Mathf.Min(amount, currentHealth);
        currentHealth -= actualDamage;

        if (dealer != null)
        {
            if (damageContributors.TryGetValue(dealer, out int existing))
                damageContributors[dealer] = existing + actualDamage;
            else
                damageContributors[dealer] = actualDamage;

            if (weaponMultiplier > 0f)
                goldMultipliers[dealer] = weaponMultiplier;

            totalDamageDealt += actualDamage;
        }

        if (dealer != null && dealer.goldOnHit > 0)
            dealer.AddGold(dealer.goldOnHit);

        if (verboseLogging)
            Debug.Log($"[{gameObject.name}] Took {actualDamage} damage | Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            HandleDeath();
    }

    public virtual void TakeDamage(int amount)
    {
        TakeDamage(amount, playerStats);
    }

    public virtual void ResetEnemy()
    {
        isDead = false;
        currentHealth = maxHealth;
        damageContributors.Clear();
        goldMultipliers.Clear();
        totalDamageDealt = 0;
        lastAttackTime = 0f;
        attackCooldownTimer = 0f;
        wasClimbing = false;
        attackStateEnteredTime = -1f;
        pathIndex = 0;

        CacheSquaredRanges();
        nextAiTick = Time.time + Random.value * aiTickInterval;

        if (playerStats == null)
        {
            playerStats = ResolvePlayer();
            if (playerStats != null)
                player = playerStats.transform;
        }

        hitFlash?.ForceReset();

        pendingKinematicRelease = true;

        if (currentPath != null && currentPath.Count > 0)
        {
            State = ZombieState.FollowingPath;
            flipbook?.SetWalking(true);
        }
        else
        {
            State = ZombieState.Idle;
            flipbook?.ForceIdle();
        }
    }

    void HandleDeath()
    {
        isDead = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        foreach (var kvp in damageContributors)
        {
            PlayerStats contributor = kvp.Key;
            int damageDealt = kvp.Value;
            float proportion = (float)damageDealt / maxHealth;
            float multiplier = goldMultipliers.TryGetValue(contributor, out float m) ? m : 1f;
            int goldAwarded = Mathf.RoundToInt(goldBounty * proportion * multiplier * contributor.goldGainMultiplier);
            contributor.AddGold(goldAwarded);
        }

        if (verboseLogging) Debug.Log($"[{gameObject.name}] Died.");

        if (KillMarkerPool.Instance != null)
            KillMarkerPool.Instance.Spawn(transform.position, goldBounty);

        OnDeath?.Invoke();
    }

    void OnDrawGizmos()
    {
        if (!canClimb) return;
        Gizmos.color = Color.cyan;
        Vector3 rayStart = transform.position + transform.TransformVector(rayOffset);
        Gizmos.DrawRay(rayStart, transform.forward * rayLength);
        Gizmos.DrawWireSphere(rayStart, 0.05f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, hitFrameRange);
        Gizmos.color = new Color(1f, 0f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, hitFrameRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawSphere(transform.position, engageRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, engageRange);
    }

    protected bool IsPlayerInRange(float range)
    {
        if (player == null) return false;
        return (transform.position - player.position).sqrMagnitude <= range * range;
    }
}