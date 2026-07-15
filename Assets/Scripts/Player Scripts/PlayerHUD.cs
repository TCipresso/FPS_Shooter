using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;

    [Header("HP Bar")]
    public Image hpBar;
    public TMP_Text hpText;

    [Header("Money")]
    public TMP_Text moneyText;

    [Header("Lives")]
    public TMP_Text livesText;

    void Update()
    {
        if (playerStats == null) return;

        if (hpBar != null)
            hpBar.fillAmount = playerStats.maxHealth > 0 ? (float)playerStats.currentHealth / playerStats.maxHealth : 0f;

        if (hpText != null) hpText.text = $"{playerStats.currentHealth}/{playerStats.maxHealth}";
        if (moneyText != null) moneyText.text = $"{playerStats.gold}";
        if (livesText != null) livesText.text = $"{playerStats.lives}";
    }
}