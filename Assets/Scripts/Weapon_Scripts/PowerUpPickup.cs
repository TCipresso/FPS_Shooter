using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [Tooltip("Index of the weapon in WeaponInventory's Weapons list this pickup activates.")]
    public int weaponIndex;

    [Tooltip("Check this for a weapon that has its own permanent baseLevel (e.g. Z16). Starts from baseLevel+1 and decays back to baseLevel instead of resetting to level 1.")]
    public bool isBaseTypeWeapon;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        WeaponInventory inventory = other.GetComponentInParent<WeaponInventory>();
        if (inventory == null) return;

        if (isBaseTypeWeapon)
            inventory.PickupBaseLevelPowerUpByIndex(weaponIndex);
        else
            inventory.PickupPowerUpByIndex(weaponIndex);

        gameObject.SetActive(false);
    }
}