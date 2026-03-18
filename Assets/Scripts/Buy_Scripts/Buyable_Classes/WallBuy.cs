using UnityEngine;

public class WallBuy : Buyable
{
    protected override void OnPurchase(PlayerStats stats)
    {
        // Hook your weapon logic here later.
        Debug.Log($"[WallBuy] Player bought wall weapon: {gameObject.name}");
    }
}