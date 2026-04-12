using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Lines")]
    public RectTransform top;
    public RectTransform bottom;
    public RectTransform left;
    public RectTransform right;

    [Header("Spread Settings")]
    public float baseSpread = 20f;
    public float minSpread = 5f;
    public float bloomSpreadMultiplier = 10f;
    public float spreadLerpSpeed = 10f;

    [Header("ADS Settings")]
    public float adsSpread = 0f;
    public float adsAlpha = 0f;

    [Header("Color")]
    public Color normalColor = Color.white;
    public Color adsColor = Color.white;

    [Header("Weapon Reference")]
    public WeaponInventory weaponInventory;

    float currentSpread;
    Image[] images;

    void Start()
    {
        images = new Image[]
        {
            top.GetComponent<Image>(),
            bottom.GetComponent<Image>(),
            left.GetComponent<Image>(),
            right.GetComponent<Image>()
        };
    }

    void Update()
    {
        if (weaponInventory == null) return;
        WeaponBase activeWeapon = weaponInventory.GetActiveWeaponBase();
        if (activeWeapon == null) return;

        bool isAiming = activeWeapon.isAiming;
        float bloom = activeWeapon.currentBloom;

        float targetSpread = isAiming
            ? Mathf.Max(adsSpread, minSpread)
            : Mathf.Max(baseSpread + bloom * bloomSpreadMultiplier, minSpread);

        currentSpread = Mathf.Lerp(currentSpread, targetSpread, spreadLerpSpeed * Time.deltaTime);

        top.anchoredPosition = new Vector2(0f, currentSpread);
        bottom.anchoredPosition = new Vector2(0f, -currentSpread);
        left.anchoredPosition = new Vector2(-currentSpread, 0f);
        right.anchoredPosition = new Vector2(currentSpread, 0f);

        Color targetColor = isAiming ? adsColor : normalColor;
        float targetAlpha = (isAiming && activeWeapon.adsFadeCrosshair) ? adsAlpha : 1f;
        targetColor.a = targetAlpha;

        foreach (Image img in images)
            img.color = Color.Lerp(img.color, targetColor, spreadLerpSpeed * Time.deltaTime);
    }
}