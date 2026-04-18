using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeCritDmg", menuName = "Upgrades/Effects/Crit Damage")]
public class UpgradeCritDmg : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddCritMultiplier(value);
        Debug.Log($"[UpgradeCritDmg] +{value:F2}x crit multiplier. Now {stats.critMultiplier:F2}x");
    }
}