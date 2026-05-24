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

[System.Serializable]
public class AttachmentSlot
{
    public string slotName;
    public List<AttachmentSO> pool = new List<AttachmentSO>();
}

[CreateAssetMenu(fileName = "NewWeaponDefinition", menuName = "Bloodsport/Weapon Definition")]
public class WeaponDefinitionSO : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "Weapon";
    public WeaponType weaponType;

    [Header("Prefab")]
    public GameObject prefab;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

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

    [Header("Attachment Slots")]
    public List<AttachmentSlot> attachmentSlots = new List<AttachmentSlot>();

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