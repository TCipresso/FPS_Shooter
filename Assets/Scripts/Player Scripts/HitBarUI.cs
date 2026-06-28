using UnityEngine;
using UnityEngine.UI;

public class HitBarUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("Layout")]
    public RectTransform barContainer;
    public float barSpacing = 4f;

    [Header("Colors")]
    public Color fullColor = new Color(0.6f, 0f, 0.05f);
    public Color recoveryColor = new Color(0.1f, 0.7f, 0.2f);

    Image[] bars;
    int lastHitsToDown = -1;

    void Start()
    {
        BuildBars();
    }

    void BuildBars()
    {
        foreach (Transform child in barContainer)
            Destroy(child.gameObject);

        int count = playerHealth.hitsToDown;
        lastHitsToDown = count;
        bars = new Image[count];

        float containerWidth = barContainer.rect.width;
        float totalSpacing = barSpacing * (count - 1);
        float barWidth = (containerWidth - totalSpacing) / count;
        float barHeight = barContainer.rect.height;

        for (int i = 0; i < count; i++)
        {
            GameObject bar = new GameObject($"Bar_{i}", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(barContainer, false);
            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(barWidth, barHeight);
            rect.anchoredPosition = new Vector2(i * (barWidth + barSpacing), 0f);

            Image img = bar.GetComponent<Image>();
            img.color = fullColor;
            bars[i] = img;
        }
    }

    void Update()
    {
        if (playerHealth == null || bars == null) return;

        if (playerHealth.hitsToDown != lastHitsToDown)
            BuildBars();

        int hitsRemaining = playerHealth.HitsRemaining;
        int hitsToDown = playerHealth.hitsToDown;
        float recoveryProgress = playerHealth.HitRecoveryProgress;

        for (int i = 0; i < bars.Length; i++)
        {
            if (i < hitsRemaining)
            {
                // Full — visible
                Color c = fullColor;
                c.a = 1f;
                bars[i].color = c;
            }
            else if (i == hitsRemaining && hitsRemaining < hitsToDown && recoveryProgress > 0f)
            {
                // Recovering — green, alpha matches recovery progress
                Color c = recoveryColor;
                c.a = recoveryProgress;
                bars[i].color = c;
            }
            else
            {
                // Empty — invisible
                Color c = fullColor;
                c.a = 0f;
                bars[i].color = c;
            }
        }
    }
}