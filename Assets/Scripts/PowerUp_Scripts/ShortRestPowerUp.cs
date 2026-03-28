using UnityEngine;

public class ShortRestPowerUp : PowerUpBase
{
    [Header("Short Rest Settings")]
    [Range(0f, 1f)]
    public float refillPercent = 0.25f;

    protected override void ApplyEffect()
    {
        if (weaponInventory == null) return;
        weaponInventory.PartialAmmoRefill(refillPercent);
        Debug.Log($"[ShortRestPowerUp] Refilled {refillPercent * 100}% of all weapon reserves.");
    }
}