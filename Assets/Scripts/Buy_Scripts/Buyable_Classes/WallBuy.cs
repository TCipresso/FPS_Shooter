using UnityEngine;

public class WallBuy : Buyable
{
    [Header("Weapon")]
    public WeaponData weaponData;

    new public string interactPrompt =>
        weaponData != null
            ? $"Buy {weaponData.weaponName} - {cost} Points"
            : $"Buy {itemName} - {cost} Points";

    protected override void OnPurchase(PlayerStats stats)
    {
        if (weaponData == null)
        {
            Debug.LogWarning("[WallBuy] No WeaponData assigned!");
            return;
        }

        WeaponInventory inventory = stats.GetComponent<WeaponInventory>();

        if (inventory == null)
        {
            Debug.LogWarning("[WallBuy] No WeaponInventory found on player!");
            return;
        }

        inventory.TryAddWeapon(weaponData);
    }
}