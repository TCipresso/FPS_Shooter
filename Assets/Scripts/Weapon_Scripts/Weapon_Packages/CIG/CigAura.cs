using UnityEngine;
using System.Collections.Generic;
public class CigAura : MonoBehaviour
{
    public WeaponInventory inventory;
    public WeaponInventory.Hand hand;
    public Transform visualTransform;
    public LayerMask zombieMask;
    const int MaxHits = 32;
    readonly Collider[] hitBuffer = new Collider[MaxHits];
    readonly HashSet<ZombieBase> hitZombies = new HashSet<ZombieBase>();
    float tickTimer = 0f;
    void OnEnable()
    {
        tickTimer = 0f;
    }
    void Update()
    {
        if (inventory == null) return;
        WeaponDefinitionSO def = inventory.GetRuntimeDefinition(hand);
        PlayerStats stats = inventory.playerStats;
        if (def == null || stats == null) return;
        float radius = def.auraRadius * stats.aoeSize;
        Transform vt = visualTransform != null ? visualTransform : transform;
        vt.localScale = Vector3.one * radius * 2f;
        float rpm = def.rpm * stats.attackSpeed;
        float interval = 60f / Mathf.Max(rpm, 0.01f);
        tickTimer += Time.deltaTime;
        if (tickTimer < interval) return;
        tickTimer -= interval;
        Tick(radius, def, stats);
    }
    void Tick(float radius, WeaponDefinitionSO def, PlayerStats stats)
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, hitBuffer, zombieMask);
        hitZombies.Clear();
        for (int i = 0; i < count; i++)
        {
            ZombieBase zombie = hitBuffer[i].GetComponentInParent<ZombieBase>();
            if (zombie != null && !zombie.IsDead)
                hitZombies.Add(zombie);
        }
        int scaledDamage = Mathf.RoundToInt(def.damage * stats.damageMultiplier);
        float critChance = def.critChance + stats.critChance;
        float critMultiplier = def.critMultiplier + stats.critMultiplier;
        int finalDamage = Random.value <= critChance ? Mathf.RoundToInt(scaledDamage * critMultiplier) : scaledDamage;
        WeaponBase activeWeapon = inventory.GetActiveWeapon(hand);
        foreach (ZombieBase zombie in hitZombies)
            zombie.TakeDamage(finalDamage, stats, 1f, default, 1f, "", activeWeapon);
    }
}