using UnityEngine;
using TMPro;

public class PointsHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;

    [Header("UI")]
    public TextMeshProUGUI pointsText;

    void Update()
    {
        if (playerStats == null || pointsText == null) return;
        pointsText.text = playerStats.points.ToString();
    }
}