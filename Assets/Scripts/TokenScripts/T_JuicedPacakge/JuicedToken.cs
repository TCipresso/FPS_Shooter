using UnityEngine;
[CreateAssetMenu(fileName = "Juiced", menuName = "Zarcade/Tokens/Juiced")]
public class JuicedToken : TokenEffectSO
{
    public int maxHpBonus = 50;
    public float regenDelayReduction = 1f;
    public override void OnApply(PlayerStats stats)
    {
        stats.AddMaxHealth(maxHpBonus);
        stats.ReduceRegenDelay(regenDelayReduction);
    }
    public override void OnRemove(PlayerStats stats)
    {
        stats.AddMaxHealth(-maxHpBonus);
        stats.ReduceRegenDelay(-regenDelayReduction);
    }
}