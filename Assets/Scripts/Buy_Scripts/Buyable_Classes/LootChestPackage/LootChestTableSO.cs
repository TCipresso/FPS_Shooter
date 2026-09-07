using System.Collections.Generic;
using UnityEngine;

// The pool a loot chest can roll from. One shared asset; chests reference it.
[CreateAssetMenu(fileName = "NewLootChestTable", menuName = "Zarcade/Loot Chest Table")]
public class LootChestTableSO : ScriptableObject
{
    [Tooltip("Every attachment this chest can drop. Rarity + luck decide which one is picked.")]
    public List<WeaponAttachmentSO> pool = new List<WeaponAttachmentSO>();
}
