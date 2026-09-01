using UnityEngine;

public enum WeaponUpgradeStatType
{
    Damage,
    AttackSpeed,
    CritChance,
    CritMultiplier,
    PelletCount,
    Accuracy,
    ReloadSpeed,    // NEW
    MagazineSize    // NEW
}

[CreateAssetMenu(fileName = "NewWeaponStatUpgrade", menuName = "Zarcade/Weapon Stat Upgrade")]
public class WeaponStatUpgradeSO : ScriptableObject
{
    public string displayName = "Stat Upgrade";
    public Sprite icon;
    public WeaponUpgradeStatType statType;
    public FloatRange commonRange = new FloatRange { min = 0.05f, max = 0.10f };
    public FloatRange rareRange = new FloatRange { min = 0.10f, max = 0.18f };
    public FloatRange epicRange = new FloatRange { min = 0.18f, max = 0.28f };
    public FloatRange extraterrestrialRange = new FloatRange { min = 0.28f, max = 0.45f };

    public FloatRange GetRange(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return commonRange;
            case UpgradeRarity.Rare: return rareRange;
            case UpgradeRarity.Epic: return epicRange;
            case UpgradeRarity.Extraterrestrial: return extraterrestrialRange;
            default: return commonRange;
        }
    }

    static readonly System.Collections.Generic.Dictionary<WeaponUpgradeStatType, string> statLabels =
        new System.Collections.Generic.Dictionary<WeaponUpgradeStatType, string>
    {
        { WeaponUpgradeStatType.Damage, "Damage" },
        { WeaponUpgradeStatType.AttackSpeed, "Attack Speed" },
        { WeaponUpgradeStatType.CritChance, "Crit Chance" },
        { WeaponUpgradeStatType.CritMultiplier, "Crit Multiplier" },
        { WeaponUpgradeStatType.PelletCount, "Pellet Count" },
        { WeaponUpgradeStatType.Accuracy, "Accuracy" },
        { WeaponUpgradeStatType.ReloadSpeed, "Reload Speed" },
        { WeaponUpgradeStatType.MagazineSize, "Magazine Size" }
    };

    public string GetRolledDescription(float percent)
    {
        string label = statLabels.TryGetValue(statType, out string s) ? s : statType.ToString();

        // Special descriptions for specific stats
        switch (statType)
        {
            case WeaponUpgradeStatType.PelletCount:
                return $"Gain {percent * 100f:F0}% more Pellets";
            case WeaponUpgradeStatType.ReloadSpeed:
                return $"Reload {percent * 100f:F0}% faster";
            case WeaponUpgradeStatType.MagazineSize:
                return $"+{Mathf.RoundToInt(percent * 100f)}% Magazine Size";
            default:
                return $"Gain {percent * 100f:F0}% {label}";
        }
    }

    public void Apply(WeaponDefinitionSO def, float percent)
    {
        if (def == null) return;

        switch (statType)
        {
            case WeaponUpgradeStatType.Damage:
                def.damage = Mathf.Max(1, Mathf.RoundToInt(def.damage * (1f + percent)));
                break;

            case WeaponUpgradeStatType.AttackSpeed:
                def.rpm = def.rpm * (1f + percent);
                break;

            case WeaponUpgradeStatType.CritChance:
                def.critChance = Mathf.Clamp01(def.critChance + percent);
                break;

            case WeaponUpgradeStatType.CritMultiplier:
                def.critMultiplier = def.critMultiplier * (1f + percent);
                break;

            case WeaponUpgradeStatType.PelletCount:
                // Add to pellet count (can be decimal, will be rounded down at runtime)
                def.pelletCount = Mathf.Max(1f, def.pelletCount * (1f + percent));
                Debug.Log($"[{def.weaponName}] Pellet count: {def.pelletCount:F1} (will be rounded down to {def.GetActualPelletCount()} at runtime)");
                break;

            case WeaponUpgradeStatType.Accuracy:
                // Lower bloom = more accurate
                def.maxBloom = Mathf.Max(0f, def.maxBloom * (1f - percent));
                break;

            case WeaponUpgradeStatType.ReloadSpeed:
                // Higher reload speed = faster reload
                def.reloadSpeed = Mathf.Max(0.1f, def.reloadSpeed * (1f + percent));
                Debug.Log($"[{def.weaponName}] Reload speed: {def.reloadSpeed:F2}x");
                break;

            case WeaponUpgradeStatType.MagazineSize:
                // Increase magazine size (round up so player gets at least +1 if percent > 0)
                int increase = Mathf.Max(1, Mathf.RoundToInt(def.magazineSize * percent));
                def.magazineSize += increase;
                Debug.Log($"[{def.weaponName}] Magazine size: {def.magazineSize} (+{increase})");
                break;
        }
    }
}