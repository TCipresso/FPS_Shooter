using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class ZombieBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Gold")]
    public int goldBounty = 100;

    [Header("Drop")]
    public GameObject xpShardPrefab;
    public float xpShardSpawnHeight = 1f;

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

    // Tracks damage dealt per player (ready for multiplayer later)
    Dictionary<PlayerStats, int> damageContributors = new Dictionary<PlayerStats, int>();
    Dictionary<PlayerStats, float> goldMultipliers = new Dictionary<PlayerStats, float>();
    int totalDamageDealt = 0;

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

    public virtual void TakeDamage(int amount, PlayerStats dealer, float weaponMultiplier = 1f)
    {
        if (isDead) return;

        int actualDamage = Mathf.Min(amount, currentHealth);
        currentHealth -= actualDamage;

        // Track damage contribution
        if (dealer != null)
        {
            if (damageContributors.ContainsKey(dealer))
                damageContributors[dealer] += actualDamage;
            else
                damageContributors[dealer] = actualDamage;

            // Store the last weapon multiplier used by this dealer
            if (weaponMultiplier > 0f)
                goldMultipliers[dealer] = weaponMultiplier;

            totalDamageDealt += actualDamage;
        }

        // Gold on hit if active
        if (dealer != null && dealer.goldOnHit > 0)
            dealer.AddGold(dealer.goldOnHit);

        Debug.Log($"[{gameObject.name}] Took {actualDamage} damage | Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    // Overload for backwards compatibility
    public virtual void TakeDamage(int amount)
    {
        TakeDamage(amount, playerStats);
    }

    protected virtual void Die()
    {
        isDead = true;
        agent.isStopped = true;

        // Award proportional gold to each contributor with weapon multiplier
        foreach (var kvp in damageContributors)
        {
            PlayerStats contributor = kvp.Key;
            int damageDealt = kvp.Value;
            float proportion = (float)damageDealt / maxHealth;
            float multiplier = goldMultipliers.ContainsKey(contributor) ? goldMultipliers[contributor] : 1f;
            int goldAwarded = Mathf.RoundToInt(goldBounty * proportion * multiplier);
            contributor.AddGold(goldAwarded);
            Debug.Log($"[{gameObject.name}] Awarded {goldAwarded} gold to {contributor.gameObject.name} ({proportion * 100:F0}% damage, x{multiplier} multiplier).");
        }

        Debug.Log($"[{gameObject.name}] Died.");
        OnDeath();
    }

    protected virtual void OnDeath()
    {
        if (xpShardPrefab != null)
            Instantiate(xpShardPrefab, transform.position + Vector3.up * xpShardSpawnHeight, Quaternion.identity);

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