using UnityEngine;
[CreateAssetMenu(fileName = "Motion", menuName = "Zarcade/Tokens/Motion")]
public class MotionToken : TokenEffectSO
{
    public float goldGainBonus = 0.10f;
    public override void OnApply(PlayerStats stats)
    {
        stats.AddGoldGain(goldGainBonus);
    }
    public override void OnRemove(PlayerStats stats)
    {
        stats.AddGoldGain(-goldGainBonus);
    }
}