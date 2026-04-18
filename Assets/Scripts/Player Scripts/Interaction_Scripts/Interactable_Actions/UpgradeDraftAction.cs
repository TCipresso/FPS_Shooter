using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeDraftAction", menuName = "Interactable/Upgrade Draft Action")]
public class UpgradeDraftAction : InteractableAction
{
    [Header("Cost")]
    public int cost = 500;

    public override void Execute(PlayerStats stats)
    {
        if (!stats.SpendGold(cost))
        {
            Debug.Log("[UpgradeDraftAction] Not enough gold.");
            return;
        }

        AugmentDraftUI draft = Object.FindFirstObjectByType<AugmentDraftUI>();
        if (draft == null)
        {
            Debug.LogWarning("[UpgradeDraftAction] No AugmentDraftUI found in scene!");
            return;
        }

        draft.OpenUpgradeDraft();
    }
}