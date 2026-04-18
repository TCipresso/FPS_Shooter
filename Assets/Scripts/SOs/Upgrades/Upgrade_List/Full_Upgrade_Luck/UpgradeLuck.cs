using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeLuck", menuName = "Upgrades/Effects/Luck")]
public class UpgradeLuck : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddLuck(value);
        Debug.Log($"[UpgradeLuck] +{value:F1}% luck. Now {stats.luck:F1}%");
    }
}