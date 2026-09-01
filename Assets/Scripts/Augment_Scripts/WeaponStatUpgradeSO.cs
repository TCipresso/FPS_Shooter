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
    MagazineSize
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
        if (def == null) return;

        bool isFlat = scalingType == UpgradeScalingType.Flat;

        switch (statType)
        {
            case WeaponUpgradeStatType.Damage:
                if (isFlat)
                    def.damage = Mathf.Max(1, def.damage + Mathf.RoundToInt(value));
                else
                    def.damage = Mathf.Max(1, Mathf.RoundToInt(def.damage * (1f + value)));
                break;

            case WeaponUpgradeStatType.AttackSpeed:
                if (isFlat)
                    def.rpm = Mathf.Max(1f, def.rpm + value);
                else
                    def.rpm = def.rpm * (1f + value);
                break;

            case WeaponUpgradeStatType.CritChance:
                // Crit chance is always additive (flat)
                def.critChance = Mathf.Clamp01(def.critChance + value);
                break;

            case WeaponUpgradeStatType.CritMultiplier:
                if (isFlat)
                    def.critMultiplier = Mathf.Max(1f, def.critMultiplier + value);
                else
                    def.critMultiplier = def.critMultiplier * (1f + value);
                break;

            case WeaponUpgradeStatType.PelletCount:
                if (isFlat)
                    def.pelletCount = Mathf.Max(1f, def.pelletCount + value);
                else
                    def.pelletCount = Mathf.Max(1f, def.pelletCount * (1f + value));
                Debug.Log($"[{def.weaponName}] Pellet count: {def.pelletCount:F1} (rounded down to {def.GetActualPelletCount()})");
                break;

            case WeaponUpgradeStatType.Accuracy:
                // Lower bloom = more accurate
                if (isFlat)
                    def.maxBloom = Mathf.Max(0f, def.maxBloom - value);
                else
                    def.maxBloom = Mathf.Max(0f, def.maxBloom * (1f - value));
                break;

            case WeaponUpgradeStatType.ReloadSpeed:
                if (isFlat)
                    def.reloadSpeed = Mathf.Max(0.1f, def.reloadSpeed + value);
                else
                    def.reloadSpeed = Mathf.Max(0.1f, def.reloadSpeed * (1f + value));
                Debug.Log($"[{def.weaponName}] Reload speed: {def.reloadSpeed:F2}x");
                break;

            case WeaponUpgradeStatType.MagazineSize:
                if (isFlat)
                {
                    int increase = Mathf.RoundToInt(value);
                    def.magazineSize = Mathf.Max(1, def.magazineSize + increase);
                    Debug.Log($"[{def.weaponName}] Magazine size: {def.magazineSize} (+{increase})");
                }
                else
                {
                    int increase = Mathf.Max(1, Mathf.RoundToInt(def.magazineSize * value));
                    def.magazineSize += increase;
                    Debug.Log($"[{def.weaponName}] Magazine size: {def.magazineSize} (+{increase})");
                }
                break;
        }
    }
}