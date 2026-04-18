using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeReloadSpeed", menuName = "Upgrades/Effects/Reload Speed")]
public class UpgradeReloadSpeed : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddReloadSpeed(value);
        Debug.Log($"[UpgradeReloadSpeed] +{value * 100:F1}% reload speed. Now {stats.reloadSpeed:F2}x");
    }
}