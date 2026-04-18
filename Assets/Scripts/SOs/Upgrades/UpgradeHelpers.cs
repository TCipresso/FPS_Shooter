using UnityEngine;

public enum UpgradeRarity
{
    Common,
    Rare,
    Epic,
    Extraterrestrial
}

[System.Serializable]
public struct FloatRange
{
    public float min;
    public float max;

    public float GetRandom()
    {
        return Random.Range(min, max);
    }
}

public static class UpgradeRarityHelper
{
    // tweak these weights however you want
    public static UpgradeRarity RollRarity()
    {
        float r = Random.value;

        // example distribution:
        // 60% Common, 25% Rare, 10% Epic, 5% Extraterrestrial
        if (r < 0.60f) return UpgradeRarity.Common;
        if (r < 0.85f) return UpgradeRarity.Rare;
        if (r < 0.95f) return UpgradeRarity.Epic;
        return UpgradeRarity.Extraterrestrial;
    }

    public static Color GetColor(UpgradeRarity rarity)
    {
        // optional helper if you want to color-code rarities in UI
        switch (rarity)
        {
            case UpgradeRarity.Common: return Color.white;
            case UpgradeRarity.Rare: return new Color(0.3f, 0.6f, 1f); // blue-ish
            case UpgradeRarity.Epic: return new Color(0.7f, 0.2f, 0.9f); // purple
            case UpgradeRarity.Extraterrestrial: return new Color(0.1f, 1f, 0.1f); // neon green
            default: return Color.gray;
        }
    }
}
