using UnityEngine;

public class TokenMachine : Buyable
{
    [Header("Token Settings")]
    public TokenDefinitionSO token;
    protected override void OnPurchase(PlayerStats stats)
    {
        TokenInventory inventory = stats.GetComponent<TokenInventory>();
        if (inventory != null)
        {
            inventory.AddToken(token);
        }
        else
        {
            Debug.LogWarning("[TokenMachine] No TokenInventory found on player.");
        }
    }
}