using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeGoldGain", menuName = "Upgrades/Effects/Gold Gain")]
public class UpgradeGoldGain : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddGoldGain(value);
        Debug.Log($"[UpgradeGoldGain] +{value * 100:F1}% gold gain. Now {stats.goldGainMultiplier:F2}x");
    }
}