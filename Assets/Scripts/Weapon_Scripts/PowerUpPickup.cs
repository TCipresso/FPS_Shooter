using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [Tooltip("Index of the weapon in WeaponInventory's Weapons list this pickup activates.")]
    public int weaponIndex;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        WeaponInventory inventory = other.GetComponentInParent<WeaponInventory>();
        if (inventory == null) return;

        inventory.PickupPowerUpByIndex(weaponIndex);
        gameObject.SetActive(false);
    }
}