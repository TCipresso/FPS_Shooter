using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public abstract class ZombieBase : MonoBehaviour
{
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
    public float ragdollForce = 8f;
    float lastRagdollForceMultiplier = 1f;

    [Header("Skeleton")]
    public Transform skeletonRoot;

    [Header("Health Bar")]
    public Transform headTransform;

    [Header("Identity")]
    public string enemyId;

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

    Vector3 lastHitDirection = Vector3.back;

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
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                Debug.Log($"[{gameObject.name}] Warped to NavMesh at {hit.position}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Could not find NavMesh near spawn point!");
            }
        }
    }

    public void ClearDeathListeners()
    {
        OnDeath = null;
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
        wasClimbing = false;

        if (isGrunt)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            agent.enabled = true;
            agent.isStopped = false;
        }
    }

    void HandleDeath()
    {
        isDead = true;

        if (isGrunt)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        else
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

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

        if (WeaponDropManager.Instance != null)
            WeaponDropManager.Instance.TryDrop(transform.position);

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

    IEnumerator SpawnRagdollNextFrame(Vector3 hitDirection, float force)
    {
        yield return new WaitForEndOfFrame();
        if (EnemySpawnManager.Instance != null)
        {
            GameObject corpse = EnemySpawnManager.Instance.SpawnRagdoll(enemyId, transform.position, transform.rotation, hitDirection, force);
            if (corpse != null)
                CopyPoseToRagdoll(corpse);
        }
    }
}