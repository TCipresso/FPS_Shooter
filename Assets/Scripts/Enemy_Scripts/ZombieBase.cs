using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public abstract class ZombieBase : MonoBehaviour
{
    public enum ZombieState { Idle, Engage, Attack }

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    string lastHitBone = "";

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

    [Header("Climbing")]
    public LayerMask groundLayer;
    public bool canClimb = true;
    public float climbSpeed = 2f;
    public Vector3 rayOffset = new Vector3(0f, -0.9f, 0f);
    public float rayLength = 0.6f;
    public float launchForce = 8f;
    bool wasClimbing = false;

    [Header("Ragdoll")]
    public float ragdollForce = 8f;
    float lastRagdollForceMultiplier = 1f;

    [Header("Skeleton")]
    public Transform skeletonRoot;

    [Header("Health Bar")]
    public Transform headTransform;

    [Header("Identity")]
    public string enemyId;

    [Header("Animation")]
    public Animator animator;

    [Header("Debug")]
    public bool verboseLogging = false;

    public ZombieState State { get; private set; } = ZombieState.Idle;

    public event System.Action OnDeath;
    public event System.Action<int, int> OnHealthChanged;

    protected Rigidbody rb;
    CapsuleCollider col;
    protected Transform player;
    protected PlayerStats playerStats;
    protected PlayerHealth playerHealth;
    protected float lastAttackTime;
    protected bool isDead = false;

    float attackCooldownTimer = 0f;
    Vector3 lastHitDirection = Vector3.back;

    Dictionary<PlayerStats, int> damageContributors = new Dictionary<PlayerStats, int>();
    Dictionary<PlayerStats, float> goldMultipliers = new Dictionary<PlayerStats, float>();
    int totalDamageDealt = 0;

    Coroutine kinematicReleaseRoutine;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            player = playerStats.transform;
            playerHealth = playerStats.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] PlayerStats not found in scene.");
        }
    }

    public void ClearDeathListeners() => OnDeath = null;

    protected virtual void Update()
    {
        if (isDead) return;

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        float dist = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        switch (State)
        {
            case ZombieState.Idle:
                if (dist <= engageRange)
                    SetState(ZombieState.Engage);
                break;

            case ZombieState.Engage:
                if (dist <= attackRange && attackCooldownTimer <= 0f)
                    SetState(ZombieState.Attack);
                break;

            case ZombieState.Attack:
                break;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || player == null) return;
        if (rb.isKinematic) return;
        if (State == ZombieState.Engage)
            GruntMove();
    }

    protected void SetState(ZombieState newState)
    {
        State = newState;

        switch (newState)
        {
            case ZombieState.Idle:
                rb.linearVelocity = Vector3.zero;
                animator?.SetBool("IsWalking", false);
                animator?.SetBool("IsAttacking", false);
                break;

            case ZombieState.Engage:
                animator?.SetBool("IsWalking", true);
                animator?.SetBool("IsAttacking", false);
                break;

            case ZombieState.Attack:
                rb.linearVelocity = Vector3.zero;
                animator?.SetBool("IsWalking", false);
                animator?.SetTrigger("Attack");
                break;
        }
    }

    public virtual void OnHitFrame()
    {
        float dist = player != null ? Vector3.Distance(transform.position, player.position) : -1f;
        if (player == null || playerHealth == null) return;
        if (dist <= hitFrameRange)
            playerHealth.TakeHit();
    }

    public virtual void OnAttackComplete()
    {
        attackCooldownTimer = attackCooldown;
        float dist = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
        SetState(dist <= engageRange ? ZombieState.Engage : ZombieState.Idle);
    }

    void GruntMove()
    {
        Vector3 dir = player.position - transform.position;
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

        Vector3 move = hitWall ? Vector3.up * climbSpeed : dir * moveSpeed;
        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);
    }

    void CopyPoseToRagdoll(GameObject ragdoll)
    {
        if (skeletonRoot == null) return;

        Transform[] liveJoints = skeletonRoot.GetComponentsInChildren<Transform>();
        Transform[] ragdollJoints = ragdoll.GetComponentsInChildren<Transform>();

        Dictionary<string, Transform> ragdollMap = new Dictionary<string, Transform>();
        foreach (Transform t in ragdollJoints)
            ragdollMap[t.name] = t;

        foreach (Transform liveJoint in liveJoints)
        {
            if (ragdollMap.TryGetValue(liveJoint.name, out Transform ragdollJoint))
            {
                ragdollJoint.position = liveJoint.position;
                ragdollJoint.rotation = liveJoint.rotation;
            }
        }
    }

    public virtual void TakeDamage(int amount, PlayerStats dealer, float weaponMultiplier = 1f, Vector3 hitDirection = default, float ragdollForceMultiplier = 1f, string hitBone = "")
    {
        if (isDead) return;
        if (hitDirection != default) lastHitDirection = hitDirection;
        lastRagdollForceMultiplier = ragdollForceMultiplier;
        if (!string.IsNullOrEmpty(hitBone)) lastHitBone = hitBone;

        int actualDamage = Mathf.Min(amount, currentHealth);
        currentHealth -= actualDamage;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (dealer != null)
        {
            if (damageContributors.ContainsKey(dealer))
                damageContributors[dealer] += actualDamage;
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
        lastHitDirection = Vector3.back;
        damageContributors.Clear();
        goldMultipliers.Clear();
        totalDamageDealt = 0;
        lastAttackTime = 0f;
        attackCooldownTimer = 0f;
        wasClimbing = false;
        State = ZombieState.Idle;

        animator?.SetBool("IsWalking", false);
        animator?.SetBool("IsAttacking", false);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (kinematicReleaseRoutine != null)
            StopCoroutine(kinematicReleaseRoutine);
        kinematicReleaseRoutine = StartCoroutine(ReleaseKinematicNextFixedUpdate());
    }

    IEnumerator ReleaseKinematicNextFixedUpdate()
    {
        yield return new WaitForFixedUpdate();
        rb.isKinematic = false;
        kinematicReleaseRoutine = null;
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
            float multiplier = goldMultipliers.ContainsKey(contributor) ? goldMultipliers[contributor] : 1f;
            int goldAwarded = Mathf.RoundToInt(goldBounty * proportion * multiplier * contributor.goldGainMultiplier);
            contributor.AddGold(goldAwarded);
        }

        if (verboseLogging) Debug.Log($"[{gameObject.name}] Died.");

        StartCoroutine(SpawnRagdollThenReturn(lastHitDirection, ragdollForce * lastRagdollForceMultiplier));
    }

    IEnumerator SpawnRagdollThenReturn(Vector3 hitDirection, float force)
    {
        yield return new WaitForEndOfFrame();

        if (EnemySpawnManager.Instance != null)
        {
            GameObject corpse = EnemySpawnManager.Instance.SpawnRagdoll(enemyId, transform.position, transform.rotation, hitDirection, force, lastHitBone);
            if (corpse != null)
                CopyPoseToRagdoll(corpse);
        }

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
        return Vector3.Distance(transform.position, player.position) <= range;
    }
}