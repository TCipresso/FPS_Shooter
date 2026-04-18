using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeMobility", menuName = "Upgrades/Effects/Mobility")]
public class UpgradeMobility : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddMobility(value);
        Debug.Log($"[UpgradeMobility] +{value * 100:F1}% mobility. Now {stats.mobilityMultiplier:F2}x");
    }
}