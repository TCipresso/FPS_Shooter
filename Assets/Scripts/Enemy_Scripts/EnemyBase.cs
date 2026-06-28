using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBase : MonoBehaviour
{
    public enum EnemyState { Idle, Engage, Attack }

    [Header("References")]
    public Animator animator;

    [Header("Stats")]
    public int health = 100;

    [Header("Detection")]
    public float engageRange = 15f;

    [Header("Attack")]
    public float attackRange = 1.8f;
    public float attackCooldown = 1.2f;
    public float hitFrameRange = 2.2f;

    public EnemyState State { get; private set; } = EnemyState.Idle;

    protected NavMeshAgent agent;
    protected Transform player;
    protected PlayerHealth playerHealth;

    float attackCooldownTimer = 0f;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponentInChildren<PlayerHealth>();
        }
    }

    protected virtual void Update()
    {
        if (player == null) return;

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (State)
        {
            case EnemyState.Idle:
                UpdateIdle(dist);
                break;
            case EnemyState.Engage:
                UpdateEngage(dist);
                break;
            case EnemyState.Attack:
                // attack state is driven by animation events
                break;
        }
    }

    void UpdateIdle(float dist)
    {
        if (dist <= engageRange)
            SetState(EnemyState.Engage);
    }

    void UpdateEngage(float dist)
    {
        if (dist <= attackRange && attackCooldownTimer <= 0f)
        {
            SetState(EnemyState.Attack);
            return;
        }

        agent.SetDestination(player.position);
    }

    void SetState(EnemyState newState)
    {
        State = newState;

        switch (newState)
        {
            case EnemyState.Idle:
                agent.ResetPath();
                animator?.SetBool("IsWalking", false);
                animator?.SetBool("IsAttacking", false);
                break;

            case EnemyState.Engage:
                animator?.SetBool("IsWalking", true);
                animator?.SetBool("IsAttacking", false);
                break;

            case EnemyState.Attack:
                agent.ResetPath();
                animator?.SetBool("IsWalking", false);
                animator?.SetTrigger("Attack");
                break;
        }
    }

    // Called by animation event on the hit frame
    public virtual void OnHitFrame()
    {
        if (player == null || playerHealth == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= hitFrameRange)
            playerHealth.TakeHit();
    }

    // Called by animation event when attack animation finishes
    public virtual void OnAttackComplete()
    {
        attackCooldownTimer = attackCooldown;
        float dist = Vector3.Distance(transform.position, player.position);
        SetState(dist <= engageRange ? EnemyState.Engage : EnemyState.Idle);
    }

    public virtual void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Debug.Log($"[EnemyBase] {gameObject.name} died.");
        Destroy(gameObject);
    }
}