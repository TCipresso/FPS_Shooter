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
            case UpgradeRarity.Common: return Color.white;
            case UpgradeRarity.Rare: return new Color(0.3f, 0.6f, 1f);
            case UpgradeRarity.Epic: return new Color(0.7f, 0.2f, 0.9f);
            case UpgradeRarity.Extraterrestrial: return new Color(0.1f, 1f, 0.1f);
            default: return Color.gray;
        }
    }
}
