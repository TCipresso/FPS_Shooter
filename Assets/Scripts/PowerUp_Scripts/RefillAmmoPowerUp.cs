using UnityEngine;

public class RefillAmmoPowerUp : PowerUpBase
{
    protected override void ApplyEffect()
    {
        if (weaponInventory == null) return;
        weaponInventory.MaxAmmo();
        Debug.Log("[RefillAmmoPowerUp] All weapons refilled.");
    }
}