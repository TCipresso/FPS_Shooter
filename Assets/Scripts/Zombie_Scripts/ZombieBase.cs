using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public abstract class ZombieBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Gold")]
    public int goldBounty = 100;

    [Header("Movement")]
    public float moveSpeed = 3.5f;

    [Header("Attack")]
    public int attackDamage = 25;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;

    [Header("Pathfinding Mode")]
    public bool isGrunt = true;

    [Header("Climbing")]
    public LayerMask groundLayer;
    public bool canClimb = true;
    public float climbSpeed = 2f;
    public Vector3 rayOffset = new Vector3(0f, -0.9f, 0f);
    public float rayLength = 0.6f;
    public float launchForce = 8f;
    bool wasClimbing = false;

    [Header("Ragdoll")]
    public float ragdollDestroyDelay = 4f;
    public float ragdollForce = 8f;

    [Header("Health Bar")]
    public Transform headTransform;

    [Header("Debug")]
    public bool verboseLogging = false;

    public event System.Action OnDeath;
    public event System.Action<int, int> OnHealthChanged;

    protected NavMeshAgent agent;
    protected Rigidbody rb;
    CapsuleCollider col;
    protected Transform player;
    protected PlayerStats playerStats;
    protected float lastAttackTime;
    protected bool isDead = false;

    Animator animator;
    Rigidbody[] ragdollBodies;
    Collider[] ragdollColliders;

    Vector3 lastHitDirection = Vector3.back;

    Dictionary<PlayerStats, int> damageContributors = new Dictionary<PlayerStats, int>();
    Dictionary<PlayerStats, float> goldMultipliers = new Dictionary<PlayerStats, float>();
    int totalDamageDealt = 0;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();

        // Grab all rigidbodies on child bones (excluding root rb)
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        SetRagdollActive(false);

        if (isGrunt)
        {
            agent.enabled = false;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        else
        {
            agent.speed = moveSpeed;
            rb.isKinematic = true;
        }
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
            player = playerStats.transform;
        else
            Debug.LogWarning($"[{gameObject.name}] PlayerStats not found in scene.");

        if (!isGrunt && !agent.isOnNavMesh)
            Debug.LogError($"[{gameObject.name}] NavMeshAgent is NOT on the NavMesh!");
    }

    protected virtual void Update()
    {
        if (isDead) return;
        UpdateBehaviour();
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || !isGrunt || player == null) return;
        GruntMove();
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

    void SetRagdollActive(bool active)
    {
        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == rb) continue; // skip root
            body.isKinematic = !active;
        }

        foreach (Collider c in ragdollColliders)
        {
            if (c == col) continue; // skip root collider
            c.enabled = active;
        }
    }

    protected abstract void UpdateBehaviour();

    protected void ChasePlayer()
    {
        if (player == null || !agent.isOnNavMesh) return;
        agent.SetDestination(player.position);
    }

    protected void StopMovement()
    {
        if (isGrunt)
            rb.linearVelocity = Vector3.zero;
        else
            agent.isStopped = true;
    }

    protected void ResumeMovement()
    {
        if (!isGrunt)
            agent.isStopped = false;
    }

    public virtual void TakeDamage(int amount, PlayerStats dealer, float weaponMultiplier = 1f, Vector3 hitDirection = default)
    {
        if (isDead) return;

        if (hitDirection != default)
            lastHitDirection = hitDirection;

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

    void HandleDeath()
    {
        isDead = true;

        // Stop all movement
        if (isGrunt)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; // hand off physics to ragdoll bones
        }
        else
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Disable animator, enable ragdoll
        if (animator != null) animator.enabled = false;
        if (col != null) col.enabled = false;
        SetRagdollActive(true);

        // Apply knockback force to hips (first non-root rb, usually hips bone)
        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == rb) continue;
            body.AddForce(lastHitDirection * ragdollForce + Vector3.up * (ragdollForce * 0.5f), ForceMode.Impulse);
            break; // just hips, let joints propagate naturally
        }

        // Gold payout
        foreach (var kvp in damageContributors)
        {
            PlayerStats contributor = kvp.Key;
            int damageDealt = kvp.Value;
            float proportion = (float)damageDealt / maxHealth;
            float multiplier = goldMultipliers.ContainsKey(contributor) ? goldMultipliers[contributor] : 1f;
            int goldAwarded = Mathf.RoundToInt(goldBounty * proportion * multiplier * contributor.goldGainMultiplier);
            contributor.AddGold(goldAwarded);

            if (verboseLogging)
                Debug.Log($"[{gameObject.name}] Awarded {goldAwarded} gold to {contributor.gameObject.name}.");
        }

        if (verboseLogging) Debug.Log($"[{gameObject.name}] Died.");
        OnDeath?.Invoke();

        if (WeaponDropManager.Instance != null)
            WeaponDropManager.Instance.TryDrop(transform.position);

        Destroy(gameObject, ragdollDestroyDelay);
    }

    void OnDrawGizmos()
    {
        if (!isGrunt || !canClimb) return;
        Gizmos.color = Color.cyan;
        Vector3 rayStart = transform.position + transform.TransformVector(rayOffset);
        Gizmos.DrawRay(rayStart, transform.forward * rayLength);
        Gizmos.DrawWireSphere(rayStart, 0.05f);
    }

    protected bool IsPlayerInRange(float range)
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= range;
    }
}