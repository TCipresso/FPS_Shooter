using UnityEngine;
[CreateAssetMenu(fileName = "TriggerFinger", menuName = "Zarcade/Tokens/TriggerFinger")]
public class TriggerFingerToken : TokenEffectSO
{
    public float attackSpeedBonus = 0.25f;
    public override void OnApply(PlayerStats stats)
    {
        stats.AddAttackSpeed(attackSpeedBonus);
    }
    public override void OnRemove(PlayerStats stats)
    {
        stats.AddAttackSpeed(-attackSpeedBonus);
    }
}