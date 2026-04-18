using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Info")]
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effect")]
    public UpgradeEffect effect;

    [Header("Per-Rarity Value Ranges")]
    public FloatRange commonRange;            // e.g. 3–5  (percent or flat)
    public FloatRange rareRange;              // e.g. 6–8
    public FloatRange epicRange;              // e.g. 9–12
    public FloatRange extraterrestrialRange;  // e.g. 15–20

    public float GetRandomValueForRarity(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common:
                return commonRange.GetRandom();
            case UpgradeRarity.Rare:
                return rareRange.GetRandom();
            case UpgradeRarity.Epic:
                return epicRange.GetRandom();
            case UpgradeRarity.Extraterrestrial:
                return extraterrestrialRange.GetRandom();
            default:
                return commonRange.GetRandom();
        }
    }

    public void ApplyWithRarity(GameObject player, UpgradeRarity rarity)
    {
        if (effect == null)
        {
            Debug.LogWarning($"[UpgradeData] No effect assigned for upgrade '{upgradeName}'.");
            return;
        }

        float value = GetRandomValueForRarity(rarity);
        effect.ApplyUpgrade(player, value);

        // Dynamically update description text for the current roll
        string formattedDesc = description.Replace("{value}", value.ToString("F1"));
        Debug.Log($"[UpgradeData] Applied {upgradeName} ({rarity}, {value:F1})  {formattedDesc}");
    }

}
