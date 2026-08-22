using UnityEngine;
public enum WeaponUpgradeStatType
{
    Damage,
    AttackSpeed,
    Range,
    CritChance,
    CritMultiplier
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
    static readonly System.Collections.Generic.Dictionary<WeaponUpgradeStatType, string> statLabels = new System.Collections.Generic.Dictionary<WeaponUpgradeStatType, string>
    {
        { WeaponUpgradeStatType.Damage, "Damage" },
        { WeaponUpgradeStatType.AttackSpeed, "Attack Speed" },
        { WeaponUpgradeStatType.Range, "Range" },
        { WeaponUpgradeStatType.CritChance, "Crit Chance" },
        { WeaponUpgradeStatType.CritMultiplier, "Crit Multiplier" }
    };
    public string GetRolledDescription(float percent)
    {
        string label = statLabels.TryGetValue(statType, out string s) ? s : statType.ToString();
        return $"Gain {percent * 100f:F0}% {label}";
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
            case WeaponUpgradeStatType.Range:
                def.range = def.range * (1f + percent);
                break;
            case WeaponUpgradeStatType.CritChance:
                def.critChance = Mathf.Clamp01(def.critChance * (1f + percent));
                break;
            case WeaponUpgradeStatType.CritMultiplier:
                def.critMultiplier = def.critMultiplier * (1f + percent);
                break;
        }
    }
}