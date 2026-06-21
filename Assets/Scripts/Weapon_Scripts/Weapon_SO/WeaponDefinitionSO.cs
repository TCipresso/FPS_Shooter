using UnityEngine;
using System.Collections.Generic;

public enum WeaponRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
    Contraband
}

public enum CrosshairType
{
    Default
}

[CreateAssetMenu(fileName = "NewWeaponDefinition", menuName = "Bloodsport/Weapon Definition")]
public class WeaponDefinitionSO : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "Weapon";
    public WeaponType weaponType;

    [Header("Left Hand Prefab")]
    public GameObject leftPrefab;
    public Vector3 leftPositionOffset = Vector3.zero;
    public Vector3 leftRotationOffset = Vector3.zero;

    [Header("Right Hand Prefab")]
    public GameObject rightPrefab;
    public Vector3 rightPositionOffset = Vector3.zero;
    public Vector3 rightRotationOffset = Vector3.zero;

    [Header("Bullet Data")]
    public BulletDataSO bulletData;

    [Header("Base Stats")]
    public int baseDamage = 25;
    public float baseRpm = 300f;
    public int baseMagSize = 30;
    public int baseReserveAmmo = 90;
    public float baseRange = 50f;
    public float baseReloadTime = 2f;

    [Header("Rarity Stat Multipliers")]
    public float rareBonus = 0.05f;
    public float epicBonus = 0.10f;
    public float legendaryBonus = 0.15f;
    public float contrabandBonus = 0.25f;

    [Header("Alternate Fire")]
    public bool willAlternate = false;
    public float alternateRPM = 300f;

    [Header("Crosshair")]
    public CrosshairType crosshairType = CrosshairType.Default;

    [Header("Perks")]
    public List<WeaponPerkSO> perkPool = new List<WeaponPerkSO>();

    public GameObject GetPrefabForSlot(int slot) => slot == 0 ? leftPrefab : rightPrefab;
    public Vector3 GetPositionOffsetForSlot(int slot) => slot == 0 ? leftPositionOffset : rightPositionOffset;
    public Vector3 GetRotationOffsetForSlot(int slot) => slot == 0 ? leftRotationOffset : rightRotationOffset;

    public float GetRarityMultiplier(WeaponRarity rarity)
    {
        return rarity switch
        {
            WeaponRarity.Rare => 1f + rareBonus,
            WeaponRarity.Epic => 1f + epicBonus,
            WeaponRarity.Legendary => 1f + legendaryBonus,
            WeaponRarity.Contraband => 1f + contrabandBonus,
            _ => 1f
        };
    }
}

public enum WeaponType
{
    Pistol,
    SMG,
    AR,
    Shotgun,
    Sniper,
    LMG,
    Launcher
}