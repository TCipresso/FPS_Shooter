using UnityEngine;

public enum WeaponUpgradeStatType
{
    Damage,
    AttackSpeed,
    CritChance,
    CritMultiplier,
    PelletCount,
    Accuracy,
    ReloadSpeed,
    MagazineSize,

    // Shared with the attachment engine. Level-ups only offer these if a weapon's
    // upgradePool includes an upgrade SO set to one of them.
    Range,
    SpreadAngle,
    ProjectileSpeed,
    ExplosionRadius,
    SwarmHitRadius
}

public enum UpgradeScalingType
{
    Percentage,  // Multiplicative (e.g., +10% damage)
    Flat         // Additive (e.g., +1 magazine size)
}

[CreateAssetMenu(fileName = "NewWeaponStatUpgrade", menuName = "Zarcade/Weapon Stat Upgrade")]
public class WeaponStatUpgradeSO : ScriptableObject
{
    [Header("Upgrade Info")]
    public string displayName = "Stat Upgrade";
    public Sprite icon;
    public WeaponUpgradeStatType statType;

    [Header("Scaling Type")]
    [Tooltip("Percentage = multiplicative (10% more damage). Flat = additive (+1 magazine size).")]
    public UpgradeScalingType scalingType = UpgradeScalingType.Percentage;

    [Header("Rarity Ranges")]
    [Tooltip("For Percentage scaling: 0.05 = 5%. For Flat scaling: raw value (e.g., 1 = +1 magazine)")]
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

    public string GetRolledDescription(float value)
    {
        string label = statLabels.TryGetValue(statType, out string s) ? s : statType.ToString();
        bool isFlat = scalingType == UpgradeScalingType.Flat;

        switch (statType)
        {
            case WeaponUpgradeStatType.PelletCount:
                if (isFlat)
                    return $"+{value:F1} {label}";
                else
                    return $"+{value * 100f:F0}% {label}";

            case WeaponUpgradeStatType.ReloadSpeed:
                if (isFlat)
                    return $"+{value:F2}x {label}";
                else
                    return $"+{value * 100f:F0}% {label}";

            case WeaponUpgradeStatType.MagazineSize:
                if (isFlat)
                    return $"+{Mathf.RoundToInt(value)} {label}";
                else
                    return $"+{Mathf.RoundToInt(value * 100f)}% {label}";

            case WeaponUpgradeStatType.Damage:
                if (isFlat)
                    return $"+{Mathf.RoundToInt(value)} {label}";
                else
                    return $"+{value * 100f:F0}% {label}";

            case WeaponUpgradeStatType.AttackSpeed:
                if (isFlat)
                    return $"+{value:F0} RPM";
                else
                    return $"+{value * 100f:F0}% {label}";

            case WeaponUpgradeStatType.CritChance:
                // Crit chance is always additive, show as percentage
                return $"+{value * 100f:F0}% {label}";

            case WeaponUpgradeStatType.CritMultiplier:
                if (isFlat)
                    return $"+{value:F1}x {label}";
                else
                    return $"+{value * 100f:F0}% {label}";

            case WeaponUpgradeStatType.Accuracy:
                if (isFlat)
                    return $"-{value:F1} Bloom";
                else
                    return $"-{value * 100f:F0}% Bloom";

            default:
                if (isFlat)
                    return $"+{value:F1} {label}";
                else
                    return $"+{value * 100f:F0}% {label}";
        }
    }

    public void Apply(WeaponDefinitionSO def, float value)
    {
        // Shared mutation core - see WeaponStatMath (also used by WeaponAttachmentSO).
        WeaponStatMath.Apply(def, statType, scalingType, value);
    }
}