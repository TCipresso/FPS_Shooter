using UnityEngine;

public class BasicZombie : ZombieBase
{
    void TryAttack()
    {
        agent.isStopped = true;

        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        if (playerStats != null)
            playerStats.TakeDamage(attackDamage);

        Debug.Log($"[BasicZombie] Attacked player for {attackDamage} damage.");
    }

    protected override void UpdateBehaviour()
    {
        if (IsPlayerInRange(attackRange))
            TryAttack();
        else
        {
            agent.isStopped = false;
            ChasePlayer();
        }
    }
}