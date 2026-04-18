using UnityEngine;

[CreateAssetMenu(fileName = "NewIncAtkSpdUpgrade", menuName = "Upgrades/Effects/Increase Attack Speed")]
public class IncAtkSpd : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (!stats) return;

        stats.attackSpeed += value;

        Debug.Log($"[IncAtkSpdUpgrade] +{value:F3} attack speed. Now {stats.attackSpeed:F3}x");
    }
}
