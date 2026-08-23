using System;
using UnityEngine;

public enum HandSide
{
    Left = 0,
    Right = 1
}

[Serializable]
public class HandCatalogEntry
{
    [Tooltip("Inspector label only.")]
    public string displayName;

    [Header("Definitions (assign the one that matches this item)")]
    public WeaponDefinitionSO weaponDefinition;
    public OffHandDefinitionSO offHandDefinition;

    [Header("Left Hand (pre-placed, disabled on the player)")]
    public GameObject leftRoot;
    public WeaponBase leftWeapon;
    public OffHandBase leftOffHand;

    [Header("Right Hand (pre-placed, disabled on the player)")]
    public GameObject rightRoot;
    public WeaponBase rightWeapon;
    public OffHandBase rightOffHand;

    public GameObject GetRoot(HandSide side)
    {
        return side == HandSide.Left ? leftRoot : rightRoot;
    }

    public WeaponBase GetWeapon(HandSide side)
    {
        return side == HandSide.Left ? leftWeapon : rightWeapon;
    }

    public OffHandBase GetOffHand(HandSide side)
    {
        return side == HandSide.Left ? leftOffHand : rightOffHand;
    }

    public bool HasSide(HandSide side)
    {
        if (GetRoot(side) == null) return false;
        return GetWeapon(side) != null || GetOffHand(side) != null;
    }
}

[Serializable]
public class HandLoadout
{
    [Tooltip("Catalog index. -1 = empty.")]
    public int slot0 = -1;
    [Tooltip("Catalog index. -1 = empty.")]
    public int slot1 = -1;
    [Tooltip("Which slot is currently equipped. 0 or 1.")]
    public int activeSlot = 0;

    public const int SlotCount = 2;

    public int GetSlot(int index)
    {
        if (index == 0) return slot0;
        if (index == 1) return slot1;
        return -1;
    }

    public void SetSlot(int index, int catalogIndex)
    {
        if (index == 0) slot0 = catalogIndex;
        else if (index == 1) slot1 = catalogIndex;
    }
}
