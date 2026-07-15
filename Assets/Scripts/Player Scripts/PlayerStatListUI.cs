using UnityEngine;
using TMPro;
using System.Text;

public class PlayerStatListUI : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;
    public TMP_Text statListText;

    StringBuilder sb = new StringBuilder();

    void Update()
    {
        if (playerStats == null || statListText == null) return;

        sb.Clear();
        sb.AppendLine($"HP: {playerStats.currentHealth}/{playerStats.maxHealth}");
        sb.AppendLine($"Lives: {playerStats.lives}");
        sb.AppendLine($"Health Regen: {playerStats.healthRegen:F1}/s");
        sb.AppendLine($"Regen Delay: {playerStats.regenDelay:F1}s");
        sb.AppendLine($"Damage: {playerStats.damageMultiplier:F2}x");
        sb.AppendLine($"Crit Chance: {playerStats.critChance * 100:F0}%");
        sb.AppendLine($"Crit Damage: {playerStats.critMultiplier:F2}x");
        sb.AppendLine($"Attack Speed: {playerStats.attackSpeed * 100:F0}%");
        sb.AppendLine($"Ability Damage: {playerStats.abilityDamageMultiplier:F2}x");
        sb.AppendLine($"Ability Cooldown: {playerStats.abilityCooldownMultiplier:F2}x");
        sb.AppendLine($"Move Speed: {playerStats.moveSpeed:F1}");
        sb.AppendLine($"Jump Count: {playerStats.jumpCount}");
        sb.AppendLine($"Dash Count: {playerStats.dashCount}");
        sb.AppendLine($"Luck: {playerStats.luck:F2}");
        sb.AppendLine($"Money Gain: {playerStats.goldGainMultiplier:F2}x");
        sb.AppendLine($"Power-Up Drop Chance: {playerStats.powerUpDropChance * 100:F0}%");

        statListText.text = sb.ToString();
    }
}