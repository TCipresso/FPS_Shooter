using UnityEngine;

public class Cigarette : GadgetBase
{
    [Header("Aura")]
    public float auraRadius = 3f;
    public int auraDamage = 5;
    public float tickRate = 0.5f;
    public LayerMask enemyMask;

    float tickTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnHeld()
    {
        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            tickTimer = tickRate;
            ApplyAuraDamage();
        }
    }

    public override void Activate()
    {
        // Cigarette has no active ability
    }

    void ApplyAuraDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, auraRadius, enemyMask);
        foreach (Collider hit in hits)
        {
            ZombieBase zombie = hit.GetComponentInParent<ZombieBase>();
            if (zombie != null)
                zombie.TakeDamage(auraDamage, playerStats,
                    playerStats != null ? playerStats.goldGainMultiplier : 1f,
                    Vector3.zero, 0f, hit.name);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawSphere(transform.position, auraRadius);
    }
}