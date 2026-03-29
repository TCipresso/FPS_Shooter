using UnityEngine;
using TMPro;

public class PointsHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;

    [Header("UI")]
    public TextMeshProUGUI goldText;

    void Update()
    {
        if (playerStats == null || goldText == null) return;
        goldText.text = playerStats.gold.ToString();
    }
}