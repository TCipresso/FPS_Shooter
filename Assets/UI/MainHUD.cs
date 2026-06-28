using UnityEngine;
using TMPro;

public class MainHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("Lives")]
    public TextMeshProUGUI livesText;

    void Update()
    {
        if (playerHealth == null) return;
        if (livesText != null) livesText.text = $"x {playerHealth.lives}";
    }
}