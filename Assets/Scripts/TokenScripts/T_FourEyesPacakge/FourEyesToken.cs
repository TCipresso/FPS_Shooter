using UnityEngine;
[CreateAssetMenu(fileName = "FourEyes", menuName = "Zarcade/Tokens/FourEyes")]
public class FourEyesToken : TokenEffectSO
{
    public float critChanceBonus = 0.20f;
    public override void OnApply(PlayerStats stats)
    {
        stats.AddCritChance(critChanceBonus);
    }
    public override void OnRemove(PlayerStats stats)
    {
        stats.AddCritChance(-critChanceBonus);
    }
}