using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeCritChance", menuName = "Upgrades/Effects/Crit Chance")]
public class UpgradeCritChance : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddCritChance(value);
        Debug.Log($"[UpgradeCritChance] +{value * 100:F1}% crit chance. Now {stats.critChance * 100:F1}%");
    }
}