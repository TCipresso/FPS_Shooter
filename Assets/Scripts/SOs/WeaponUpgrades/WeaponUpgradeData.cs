using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Weapon Upgrade Data")]
public class WeaponUpgradeData : ScriptableObject
{
    [Header("Info")]
    public string weaponName;
    public Sprite icon;

    [Header("Prefabs (index 0 = level 1, index 9 = level 10)")]
    public GameObject[] levelPrefabs = new GameObject[10];

    public GameObject GetPrefabForLevel(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, levelPrefabs.Length - 1);
        return levelPrefabs[idx];
    }

    public static UpgradeRarity GetRarityForLevel(int level)
    {
        if (level <= 3) return UpgradeRarity.Common;
        if (level <= 7) return UpgradeRarity.Rare;
        if (level <= 9) return UpgradeRarity.Epic;
        return UpgradeRarity.Extraterrestrial;
    }

    public static Color GetColorForLevel(int level)
    {
        return UpgradeRarityHelper.GetColor(GetRarityForLevel(level));
    }
}