using UnityEngine;
public static class StatEffectApplier
{
    public static void Apply(PlayerStats stats, PlayerStatType type, float amount)
    {
        switch (type)
        {
            case PlayerStatType.MaxHealth:
                stats.AddMaxHealth(Mathf.RoundToInt(amount));
                break;
            case PlayerStatType.HealthRegen:
                stats.AddHealthRegen(amount);
                break;
            case PlayerStatType.RegenDelay:
                stats.ReduceRegenDelay(amount);
                break;
            case PlayerStatType.CritChance:
                stats.AddCritChance(amount);
                break;
            case PlayerStatType.CritMultiplier:
                stats.AddCritMultiplier(amount);
                break;
            case PlayerStatType.AttackSpeed:
                stats.AddAttackSpeed(amount);
                break;
            case PlayerStatType.DamageMultiplier:
                stats.AddDamageMultiplier(amount);
                break;
            case PlayerStatType.AbilityDamageMultiplier:
                stats.AddAbilityDamageMultiplier(amount);
                break;
            case PlayerStatType.AbilityCooldownReduction:
                stats.AddAbilityCooldownReduction(amount);
                break;
            case PlayerStatType.AoeSize:
                stats.AddAoeSize(amount);
                break;
            case PlayerStatType.MoveSpeed:
                stats.AddMoveSpeed(amount);
                break;
            case PlayerStatType.JumpCount:
                stats.AddJumpCount(Mathf.RoundToInt(amount));
                break;
            case PlayerStatType.DashCount:
                stats.AddDashCount(Mathf.RoundToInt(amount));
                break;
            case PlayerStatType.Luck:
                stats.AddLuck(amount);
                break;
            case PlayerStatType.GoldGain:
                stats.AddGoldGain(amount);
                break;
            case PlayerStatType.PowerUpDropChance:
                stats.AddPowerUpDropChance(amount);
                break;
        }
    }
}