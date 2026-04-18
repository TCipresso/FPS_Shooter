using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeXPGain", menuName = "Upgrades/Effects/XP Gain")]
public class UpgradeXPGain : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddXPGain(value);
        Debug.Log($"[UpgradeXPGain] +{value * 100:F1}% XP gain. Now {stats.xpGainMultiplier:F2}x");
    }
}