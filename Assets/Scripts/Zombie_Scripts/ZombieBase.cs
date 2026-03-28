using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class ZombieBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 3.5f;

    [Header("Attack")]
    public int attackDamage = 25;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;

    protected NavMeshAgent agent;
    protected Transform player;
    protected PlayerStats playerStats;
    protected float lastAttackTime;
    protected bool isDead = false;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
            player = playerStats.transform;
        else
            Debug.LogWarning($"[{gameObject.name}] PlayerStats not found in scene.");

        if (!agent.isOnNavMesh)
            Debug.LogError($"[{gameObject.name}] NavMeshAgent is NOT on the NavMesh!");
    }

    protected virtual void Update()
    {
        if (isDead) return;
        UpdateBehaviour();
    }

    protected abstract void UpdateBehaviour();

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        // Use global hit points from PlayerStats
        if (playerStats != null)
            playerStats.AddPoints(playerStats.pointsOnHit);

        Debug.Log($"[{gameObject.name}] Took {amount} damage | Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        agent.isStopped = true;

        // Use global kill points from PlayerStats
        if (playerStats != null)
            playerStats.AddPoints(playerStats.pointsOnKill);

        Debug.Log($"[{gameObject.name}] Died. Player awarded {playerStats.pointsOnKill} points.");

        OnDeath();
    }

    protected virtual void OnDeath()
    {
        Destroy(gameObject, 0.1f);
    }

    protected bool IsPlayerInRange(float range)
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= range;
    }

    protected void ChasePlayer()
    {
        if (player == null || !agent.isOnNavMesh) return;
        agent.SetDestination(player.position);
    }
}