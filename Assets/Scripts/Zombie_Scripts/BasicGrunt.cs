using UnityEngine;

public class BasicGrunt : ZombieBase
{
    void TryAttack()
    {
        StopMovement();
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
        if (playerStats != null)
            playerStats.TakeDamage(attackDamage);
        Debug.Log($"[BasicGrunt] Attacked player for {attackDamage} damage.");
    }

    protected override void UpdateBehaviour()
    {
        if (IsPlayerInRange(attackRange))
            TryAttack();
        else
        {
            ResumeMovement();
            if (!isGrunt)
                ChasePlayer();
            // Grunt movement is handled in ZombieBase.FixedUpdate automatically
        }
    }
}