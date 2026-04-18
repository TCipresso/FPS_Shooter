using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeMaxHp", menuName = "Upgrades/Effects/Max HP")]
public class UpgradeMaxHp : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        int amount = Mathf.RoundToInt(value);
        stats.maxHealth += amount;
        stats.currentHealth += amount;
        Debug.Log($"[UpgradeMaxHp] +{amount} max HP. Now {stats.maxHealth}");
    }
}