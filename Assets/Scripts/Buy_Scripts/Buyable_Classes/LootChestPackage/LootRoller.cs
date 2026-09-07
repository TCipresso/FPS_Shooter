using System.Collections.Generic;
using UnityEngine;

// Picks one attachment from a table. Rolls a rarity (biased by luck via UpgradeRarityHelper),
// then a random attachment of that rarity. Steps down a tier if the pool has nothing at the
// rolled rarity.
public static class LootRoller
{
    static readonly List<WeaponAttachmentSO> scratch = new List<WeaponAttachmentSO>(32);

    public static WeaponAttachmentSO Roll(LootChestTableSO table, float luck)
    {
        if (table == null || table.pool == null || table.pool.Count == 0)
            return null;

        UpgradeRarity rolled = UpgradeRarityHelper.RollRarity(luck);

        for (int r = (int)rolled; r >= 0; r--)
        {
            scratch.Clear();
            for (int i = 0; i < table.pool.Count; i++)
            {
                WeaponAttachmentSO a = table.pool[i];
                if (a != null && (int)a.rarity == r)
                    scratch.Add(a);
            }
            if (scratch.Count > 0)
                return scratch[Random.Range(0, scratch.Count)];
        }

        // Nothing at or below the rolled rarity - just take any valid entry.
        scratch.Clear();
        for (int i = 0; i < table.pool.Count; i++)
            if (table.pool[i] != null)
                scratch.Add(table.pool[i]);

        return scratch.Count > 0 ? scratch[Random.Range(0, scratch.Count)] : null;
    }
}
