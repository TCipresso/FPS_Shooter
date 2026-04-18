using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeAtkSpd", menuName = "Upgrades/Effects/Attack Speed")]
public class UpgradeAtkSpd : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddAttackSpeed(value);
        Debug.Log($"[UpgradeAtkSpd] +{value:F3} attack speed. Now {stats.attackSpeed:F3}x");
    }
}