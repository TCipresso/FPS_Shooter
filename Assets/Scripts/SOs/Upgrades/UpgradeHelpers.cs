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
    public static UpgradeRarity RollRarity(float luck = 0f)
    {
        float r = Random.value;

        // luck is additive percentage — 100 luck = 1.0 bonus multiplier
        // multiplies down the common threshold so higher luck = fewer commons
        // no cap — 300% luck is very powerful but commons still possible
        float luckMult = 1f / (1f + luck / 100f);

        float commonThreshold = 0.60f * luckMult;
        float rareThreshold = commonThreshold + 0.25f * luckMult;
        float epicThreshold = rareThreshold + 0.10f * luckMult;

        if (r < commonThreshold) return UpgradeRarity.Common;
        if (r < rareThreshold) return UpgradeRarity.Rare;
        if (r < epicThreshold) return UpgradeRarity.Epic;
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