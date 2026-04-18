using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeMagSize", menuName = "Upgrades/Effects/Mag Size")]
public class UpgradeMagSize : UpgradeEffect
{
    public override void ApplyUpgrade(GameObject player, float value)
    {
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;
        stats.AddExtraMagazine(Mathf.RoundToInt(value));
        Debug.Log($"[UpgradeMagSize] +{Mathf.RoundToInt(value)} mag size. Total extra: {stats.extraMagazine}");
    }
}