using UnityEngine;

public class PowerMachine : Buyable
{
    [Header("Power Settings")]
    public PowerDefinitionSO power;

    protected override void OnPurchase(PlayerStats stats)
    {
        PowerInventory inventory = stats.GetComponent<PowerInventory>();
        if (inventory != null)
        {
            inventory.AddPower(power);
        }
        else
        {
            Debug.LogWarning("[PowerMachine] No PowerInventory found on player.");
        }
    }
}