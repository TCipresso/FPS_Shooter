using UnityEngine;

public abstract class Buyable : MonoBehaviour
{
    [Header("Purchase Settings")]
    public int cost = 500;
    public string itemName = "Item";

    [Header("Interact Prompt")]
    public string interactPrompt => $"Buy {itemName} - {cost} Points";

    public bool TryPurchase(PlayerStats stats)
    {
        bool success = stats.SpendGold(cost);

        if (success)
        {
            Debug.Log($"[Buyable] Purchased {gameObject.name} for {cost} points.");
            OnPurchase(stats);
        }
        else
        {
            Debug.Log($"[Buyable] Can't afford {gameObject.name}. Need {cost}, have {stats.gold}.");
        }

        return success;
    }

    protected abstract void OnPurchase(PlayerStats stats);
}