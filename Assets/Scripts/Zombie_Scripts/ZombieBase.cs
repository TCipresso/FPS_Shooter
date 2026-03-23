using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class ZombieBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int pointsOnHit = 10;
    public int pointsOnKill = 100;

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

        // Find player
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
            player = playerStats.transform;
        else
            Debug.LogWarning($"[{gameObject.name}] PlayerStats not found in scene.");
    }

    protected virtual void Update()
    {
        if (isDead) return;

        UpdateBehaviour();
    }

    // Override this in each zombie type to define behaviour
    protected abstract void UpdateBehaviour();

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        // Award hit points to player
        if (playerStats != null)
            playerStats.AddPoints(pointsOnHit);

        Debug.Log($"[{gameObject.name}] Took {amount} damage | Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        agent.isStopped = true;

        // Award kill points to player
        if (playerStats != null)
            playerStats.AddPoints(pointsOnKill);

        Debug.Log($"[{gameObject.name}] Died. Player awarded {pointsOnKill} points.");

        OnDeath();
    }

    // Override for custom death behaviour per zombie type
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
        if (player == null) return;
        agent.SetDestination(player.position);
    }
}