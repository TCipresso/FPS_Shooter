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
    public static UpgradeRarity RollRarity(float luck = 0f)
    {
        float r = Random.value;
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
        switch (rarity)
        {
            case UpgradeRarity.Common:
                return Color.green; // Common = Green
            case UpgradeRarity.Rare:
                return Color.blue; // Rare = Blue
            case UpgradeRarity.Epic:
                return Color.red; // Epic = Red
            case UpgradeRarity.Extraterrestrial:
                return new Color(1f, 0.843f, 0f); // Extraterrestrial = Gold
            default:
                return Color.gray;
        }
    }
}