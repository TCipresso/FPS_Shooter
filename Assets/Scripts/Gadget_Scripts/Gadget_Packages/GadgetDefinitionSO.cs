using UnityEngine;
using System.Collections.Generic;

public enum GadgetType
{
    Consumable,
    Tool,
    Support
}

[CreateAssetMenu(fileName = "NewGadgetDefinition", menuName = "Bloodsport/Gadget Definition")]
public class GadgetDefinitionSO : ScriptableObject
{
    [Header("Info")]
    public string gadgetName = "Gadget";
    public GadgetType gadgetType;
    public WeaponHandType handType = WeaponHandType.Versatile;

    [Header("Left Hand Prefab")]
    public GameObject leftPrefab;
    public Vector3 leftPositionOffset = Vector3.zero;
    public Vector3 leftRotationOffset = Vector3.zero;

    [Header("Right Hand Prefab")]
    public GameObject rightPrefab;
    public Vector3 rightPositionOffset = Vector3.zero;
    public Vector3 rightRotationOffset = Vector3.zero;

    [Header("Base Stats")]
    public float baseCooldown = 10f;
    public float baseDuration = 5f;
    public float basePotency = 1f;

    [Header("Rarity Stat Multipliers")]
    public float rareBonus = 0.05f;
    public float epicBonus = 0.10f;
    public float legendaryBonus = 0.15f;
    public float contrabandBonus = 0.25f;

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