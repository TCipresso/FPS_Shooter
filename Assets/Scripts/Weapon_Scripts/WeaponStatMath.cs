using UnityEngine;

// Shared stat-mutation core used by BOTH roguelike engines:
//   - WeaponStatUpgradeSO (level-up draft picks, rolled value + rarity)
//   - WeaponAttachmentSO  (curated attachments, fixed value)
// Mutates the given WeaponDefinitionSO in place. Callers pass a per-run CLONE
// (runtimeDefinition), never the source asset.
public static class WeaponStatMath
{
    public static void Apply(WeaponDefinitionSO def, WeaponUpgradeStatType stat, UpgradeScalingType scaling, float value)
    {
        if (def == null) return;

        bool isFlat = scaling == UpgradeScalingType.Flat;

        switch (stat)
        {
            case WeaponUpgradeStatType.Damage:
                def.damage = isFlat
                    ? Mathf.Max(1, def.damage + Mathf.RoundToInt(value))
                    : Mathf.Max(1, Mathf.RoundToInt(def.damage * (1f + value)));
                break;

            case WeaponUpgradeStatType.AttackSpeed:
                def.rpm = isFlat
                    ? Mathf.Max(1f, def.rpm + value)
                    : def.rpm * (1f + value);
                break;

            case WeaponUpgradeStatType.CritChance:
                // Always additive.
                def.critChance = Mathf.Clamp01(def.critChance + value);
                break;

            case WeaponUpgradeStatType.CritMultiplier:
                def.critMultiplier = isFlat
                    ? Mathf.Max(1f, def.critMultiplier + value)
                    : def.critMultiplier * (1f + value);
                break;

            case WeaponUpgradeStatType.PelletCount:
                def.pelletCount = isFlat
                    ? Mathf.Max(1f, def.pelletCount + value)
                    : Mathf.Max(1f, def.pelletCount * (1f + value));
                break;

            case WeaponUpgradeStatType.Accuracy:
                // Lower bloom = more accurate.
                def.maxBloom = isFlat
                    ? Mathf.Max(0f, def.maxBloom - value)
                    : Mathf.Max(0f, def.maxBloom * (1f - value));
                break;

            case WeaponUpgradeStatType.ReloadSpeed:
                def.reloadSpeed = isFlat
                    ? Mathf.Max(0.1f, def.reloadSpeed + value)
                    : Mathf.Max(0.1f, def.reloadSpeed * (1f + value));
                break;

            case WeaponUpgradeStatType.MagazineSize:
                if (isFlat)
                    def.magazineSize = Mathf.Max(1, def.magazineSize + Mathf.RoundToInt(value));
                else
                    def.magazineSize = Mathf.Max(1, def.magazineSize + Mathf.Max(1, Mathf.RoundToInt(def.magazineSize * value)));
                break;

            // ---- attachment-oriented stats (level-ups can use them too if a weapon's upgradePool includes them) ----

            case WeaponUpgradeStatType.Range:
                def.range = isFlat
                    ? Mathf.Max(1f, def.range + value)
                    : Mathf.Max(1f, def.range * (1f + value));
                break;

            case WeaponUpgradeStatType.SpreadAngle:
                // Lower spread = tighter grouping. Positive value = tighter (like Accuracy).
                def.pelletSpreadAngle = isFlat
                    ? Mathf.Max(0f, def.pelletSpreadAngle - value)
                    : Mathf.Max(0f, def.pelletSpreadAngle * (1f - value));
                break;

            case WeaponUpgradeStatType.ProjectileSpeed:
                def.projectileSpeed = isFlat
                    ? Mathf.Max(0.01f, def.projectileSpeed + value)
                    : Mathf.Max(0.01f, def.projectileSpeed * (1f + value));
                break;

            case WeaponUpgradeStatType.ExplosionRadius:
                def.explosionRadius = isFlat
                    ? Mathf.Max(0f, def.explosionRadius + value)
                    : Mathf.Max(0f, def.explosionRadius * (1f + value));
                break;

            case WeaponUpgradeStatType.SwarmHitRadius:
                def.swarmHitRadius = isFlat
                    ? Mathf.Max(0.01f, def.swarmHitRadius + value)
                    : Mathf.Max(0.01f, def.swarmHitRadius * (1f + value));
                break;
        }
    }
}
