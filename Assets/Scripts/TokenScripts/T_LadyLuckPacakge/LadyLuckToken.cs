using UnityEngine;
[CreateAssetMenu(fileName = "LadyLuck", menuName = "Zarcade/Tokens/LadyLuck")]
public class LadyLuckToken : TokenEffectSO
{
    public float luckBonus = 0.25f;
    public override void OnApply(PlayerStats stats)
    {
        stats.AddLuck(luckBonus);
    }
    public override void OnRemove(PlayerStats stats)
    {
        stats.AddLuck(-luckBonus);
    }
}