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
    public bool isGrunt = false;

    [Header("Climbing")]
    public LayerMask groundLayer;
    public bool canClimb = true;
    public float climbSpeed = 2f;
    public Vector3 rayOffset = new Vector3(0f, -0.9f, 0f);
    public float rayLength = 0.6f;

    public event System.Action OnDeath;

    protected NavMeshAgent agent;
    protected Rigidbody rb;
    CapsuleCollider col;
    protected Transform player;
    protected PlayerStats playerStats;
    protected float lastAttackTime;
    protected bool isDead = false;

    Dictionary<PlayerStats, int> damageContributors = new Dictionary<PlayerStats, int>();
    Dictionary<PlayerStats, float> goldMultipliers = new Dictionary<PlayerStats, float>();
    int totalDamageDealt = 0;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        if (isGrunt)
        {
            agent.enabled = false;

            // Non-kinematic so collisions still register, but configured so
            // MovePosition is authoritative and impulses can't spin/launch us.
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

    // Megabonk style: MovePosition forward each tick, raycast from the BASE of the
    // collider to detect obstacles, climb by adding Y when blocked. Collisions
    // between zombies are preserved — we kill residual velocity so impulses from
    // bumping into each other don't pinball them around.
    void GruntMove()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        // Rotate toward player
        Quaternion targetRot = Quaternion.LookRotation(dir);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));

        // Kill horizontal impulse velocity from zombie-on-zombie collisions.
        // Leave Y alone so gravity accumulates and they fall at normal speed.
        Vector3 v = rb.linearVelocity;
        v.x = 0f;
        v.z = 0f;
        rb.linearVelocity = v;

        // Raycast forward from offset point. Hit wall = go up. No wall = go forward.
        Vector3 rayStart = transform.position + transform.TransformVector(rayOffset);
        bool hitWall = Physics.Raycast(rayStart, dir, rayLength, groundLayer);

        // While climbing, kill Y velocity so gravity doesn't fight the upward
        // MovePosition and cause shaking.
        if (hitWall) rb.linearVelocity = Vector3.zero;

        Vector3 move = hitWall ? Vector3.up * climbSpeed : dir * moveSpeed;
        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);
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

    public virtual void TakeDamage(int amount, PlayerStats dealer, float weaponMultiplier = 1f)
    {
        if (isDead) return;
        int actualDamage = Mathf.Min(amount, currentHealth);
        currentHealth -= actualDamage;

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

        if (isGrunt)
            rb.linearVelocity = Vector3.zero;
        else
            agent.isStopped = true;

        foreach (var kvp in damageContributors)
        {
            PlayerStats contributor = kvp.Key;
            int damageDealt = kvp.Value;
            float proportion = (float)damageDealt / maxHealth;
            float multiplier = goldMultipliers.ContainsKey(contributor) ? goldMultipliers[contributor] : 1f;
            int goldAwarded = Mathf.RoundToInt(goldBounty * proportion * multiplier * contributor.goldGainMultiplier);
            contributor.AddGold(goldAwarded);
            Debug.Log($"[{gameObject.name}] Awarded {goldAwarded} gold to {contributor.gameObject.name} ({proportion * 100:F0}% damage, x{multiplier} multiplier).");
        }

        Debug.Log($"[{gameObject.name}] Died.");
        OnDeath?.Invoke();
        Destroy(gameObject, 0.1f);
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