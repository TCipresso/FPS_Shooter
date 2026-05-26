using UnityEngine;
using System.Collections.Generic;

public class WeaponInstance
{
    public WeaponDefinitionSO definition;
    public WeaponRarity rarity;
    public List<AttachmentSO> rolledAttachments = new List<AttachmentSO>();

    public int finalDamage;
    public float finalRpm;
    public int finalMagSize;
    public int finalReserveAmmo;
    public float finalRange;
    public float finalReloadTime;

    public WeaponInstance(WeaponDefinitionSO definition, WeaponRarity rarity, List<AttachmentSO> attachments)
    {
        this.definition = definition;
        this.rarity = rarity;
        this.rolledAttachments = attachments ?? new List<AttachmentSO>();
        ComputeStats();
    }

    void ComputeStats()
    {
        float rarityMult = definition.GetRarityMultiplier(rarity);

        float damage = definition.baseDamage * rarityMult;
        float rpm = definition.baseRpm * rarityMult;
        float magSize = definition.baseMagSize * rarityMult;
        float reserve = definition.baseReserveAmmo * rarityMult;
        float range = definition.baseRange * rarityMult;
        float reloadTime = definition.baseReloadTime;

        // Pass 1 — additives
        foreach (AttachmentSO attachment in rolledAttachments)
        {
            if (attachment == null) continue;
            foreach (StatModifier mod in attachment.modifiers)
            {
                if (mod.modifierType != ModifierType.Additive) continue;
                switch (mod.stat)
                {
                    case StatType.Damage: damage += mod.value; break;
                    case StatType.Rpm: rpm += mod.value; break;
                    case StatType.MagSize: magSize += mod.value; break;
                    case StatType.ReserveAmmo: reserve += mod.value; break;
                    case StatType.RangeStat: range += mod.value; break;
                    case StatType.ReloadTime: reloadTime += mod.value; break;
                }
            }
        }

        // Pass 2 — multiplicatives
        foreach (AttachmentSO attachment in rolledAttachments)
        {
            if (attachment == null) continue;
            foreach (StatModifier mod in attachment.modifiers)
            {
                if (mod.modifierType != ModifierType.Multiplicative) continue;
                switch (mod.stat)
                {
                    case StatType.Damage: damage *= 1f + mod.value; break;
                    case StatType.Rpm: rpm *= 1f + mod.value; break;
                    case StatType.MagSize: magSize *= 1f + mod.value; break;
                    case StatType.ReserveAmmo: reserve *= 1f + mod.value; break;
                    case StatType.RangeStat: range *= 1f + mod.value; break;
                    case StatType.ReloadTime: reloadTime *= 1f + mod.value; break;
                }
            }
        }

        finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
        finalRpm = Mathf.Max(1f, rpm);
        finalMagSize = Mathf.Max(1, Mathf.RoundToInt(magSize));
        finalReserveAmmo = Mathf.Max(0, Mathf.RoundToInt(reserve));
        finalRange = Mathf.Max(1f, range);
        finalReloadTime = Mathf.Max(0.1f, reloadTime);
    }
}