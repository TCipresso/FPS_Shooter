using UnityEngine;
using System.Collections.Generic;

public class DmgAura : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;

    [Header("Aura")]
    public float radius = 5f;
    public float tickInterval = 0.25f;
    public bool instantKill = true;
    public int damageAmount = 50;
    public LayerMask targetLayers = ~0;

    [Header("Visuals")]
    public ParticleSystem edgeParticles;
    public Transform ringSprite;

    float tickTimer = 0f;
    HashSet<ZombieBase> hitZombies = new HashSet<ZombieBase>();

    float baseRadius;
    float baseParticleRadius;
    Vector3 baseSpriteScale = Vector3.one;

    void Awake()
    {
        baseRadius = radius;

        if (edgeParticles != null)
            baseParticleRadius = edgeParticles.shape.radius;

        if (ringSprite != null)
            baseSpriteScale = ringSprite.localScale;
    }

    void OnEnable()
    {
        tickTimer = 0f;
        ApplyAoeScale();
    }

    void Update()
    {
        ApplyAoeScale();

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;
        tickTimer = tickInterval;

        Tick();
    }

    void ApplyAoeScale()
    {
        float scale = playerStats != null ? playerStats.aoeSize : 1f;

        radius = baseRadius * scale;

        if (edgeParticles != null)
        {
            ParticleSystem.ShapeModule shape = edgeParticles.shape;
            shape.radius = baseParticleRadius * scale;
        }

        if (ringSprite != null)
            ringSprite.localScale = baseSpriteScale * scale;
    }

    void Tick()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers);
        Debug.Log($"[DmgAura] Tick at {transform.position}, radius {radius} — {hits.Length} colliders found.");

        hitZombies.Clear();

        foreach (Collider col in hits)
        {
            ZombieBase zombie = col.GetComponentInParent<ZombieBase>();
            if (zombie == null)
            {
                Debug.Log($"[DmgAura] Collider '{col.name}' has no ZombieBase.");
                continue;
            }

            if (zombie.IsDead) continue;

            hitZombies.Add(zombie);
        }

        foreach (ZombieBase zombie in hitZombies)
        {
            int amount = instantKill ? zombie.currentHealth : damageAmount;
            Debug.Log($"[DmgAura] Hitting {zombie.name} for {amount}.");
            zombie.TakeDamage(amount, playerStats,
                playerStats != null ? playerStats.goldGainMultiplier : 1f,
                Vector3.zero, 1f, "");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}