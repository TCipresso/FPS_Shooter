using UnityEngine;
[CreateAssetMenu(fileName = "Chonk", menuName = "Zarcade/Tokens/Chonk")]
public class ChonkToken : TokenEffectSO
{
    public float damageMultiplierBonus = 0.20f;
    public override void OnApply(PlayerStats stats)
    {
        stats.AddDamageMultiplier(damageMultiplierBonus);
    }
    public override void OnRemove(PlayerStats stats)
    {
        stats.AddDamageMultiplier(-damageMultiplierBonus);
    }
}