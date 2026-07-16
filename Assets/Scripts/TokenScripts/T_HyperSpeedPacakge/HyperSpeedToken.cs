using UnityEngine;
[CreateAssetMenu(fileName = "HyperSpeed", menuName = "Zarcade/Tokens/HyperSpeed")]
public class HyperSpeedToken : TokenEffectSO
{
    public float moveSpeedBonusPercent = 0.20f;
    float appliedAmount;
    public override void OnApply(PlayerStats stats)
    {
        appliedAmount = stats.moveSpeed * moveSpeedBonusPercent;
        stats.AddMoveSpeed(appliedAmount);
    }
    public override void OnRemove(PlayerStats stats)
    {
        stats.AddMoveSpeed(-appliedAmount);
    }
}