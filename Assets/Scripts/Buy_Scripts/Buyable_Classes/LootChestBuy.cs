using UnityEngine;

// Buyable loot chest. On purchase: roll one attachment from the table (biased by player
// luck), tell MenuUIHelper to open the reveal panel with it, and pause. The panel's L/R
// buttons close it; this chest then destroys itself.
public class LootChestBuy : Buyable
{
    [Header("Loot")]
    public LootChestTableSO table;

    protected override void OnPurchase(PlayerStats stats)
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        float luck = stats != null ? stats.luck : 0f;
        WeaponAttachmentSO reward = LootRoller.Roll(table, luck);

        if (reward == null)
            Debug.LogWarning("[LootChestBuy] Roll returned nothing - is the LootChestTable assigned and populated?");
        else
            Debug.Log($"[LootChestBuy] Rolled: {reward.displayName} ({reward.rarity}) | luck {luck}");

        MenuUIHelper menu = MenuUIHelper.Instance != null
            ? MenuUIHelper.Instance
            : FindFirstObjectByType<MenuUIHelper>();

        if (menu != null)
            menu.OpenLootChest(reward, () => { if (this) Destroy(gameObject); });
        else
            Debug.LogWarning("[LootChestBuy] No MenuUIHelper in the scene.");
    }
}
