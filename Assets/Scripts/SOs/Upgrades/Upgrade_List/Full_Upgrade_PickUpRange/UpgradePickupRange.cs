using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradePickupRange", menuName = "Upgrades/Effects/Pickup Range")]
public class UpgradePickupRange : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddPickupRange(value);
        Debug.Log($"[UpgradePickupRange] +{value * 100:F1}% pickup range. Now {stats.pickupRange:F2}x");
    }
}