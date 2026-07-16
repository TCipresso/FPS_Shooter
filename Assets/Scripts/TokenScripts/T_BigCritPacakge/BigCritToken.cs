using UnityEngine;
[CreateAssetMenu(fileName = "BigCrit", menuName = "Zarcade/Tokens/BigCrit")]
public class BigCritToken : TokenEffectSO
{
    public float critMultiplierBonus = 0.5f;
    public override void OnApply(PlayerStats stats)
    {
        stats.AddCritMultiplier(critMultiplierBonus);
    }
    public override void OnRemove(PlayerStats stats)
    {
        stats.AddCritMultiplier(-critMultiplierBonus);
    }
}