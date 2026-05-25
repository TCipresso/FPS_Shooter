using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Shape")]
    public float armLength = 10f;
    public float lineThickness = 2f;

    [Header("Spread Settings")]
    public float baseSpread = 20f;
    public float minSpread = 5f;
    public float bloomSpreadMultiplier = 10f;
    public float spreadLerpSpeed = 10f;

    [Header("ADS Settings")]
    public float adsSpread = 0f;
    public float adsAlpha = 0f;

    [Header("ADS Crosshair Swap")]
    public Image adsCrosshair;
    public float adsSwapFadeSpeed = 10f;


    [Header("Color")]
    public Color normalColor = Color.white;
    public Color adsColor = Color.white;

    [Header("Weapon Reference")]
    public WeaponInventory weaponInventory;

    [Header("Weapon Recoil Follow")]
    public WeaponRecoil weaponRecoil;
    [Range(0f, 1f)] public float recoilFollowStrength = 0.4f;
    public float recoilFollowPixelScale = 120f;
    public float recoilReturnSpeed = 40f;

    // Runtime
    float _currentSpread;
    Vector2 _reticleOffset;
    bool _fadeToNothing = false;

    RectTransform[] _lineRTs = new RectTransform[4];
    Image[] _lineImgs = new Image[4];

    void Awake()
    {
        BuildLines();

        if (adsCrosshair != null)
        {
            Color c = adsCrosshair.color;
            c.a = 0f;
            adsCrosshair.color = c;
        }
    }

    void BuildLines()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject go = new GameObject("CrosshairLine_" + i, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            _lineRTs[i] = rt;
            _lineImgs[i] = go.GetComponent<Image>();
            _lineImgs[i].color = normalColor;
        }
    }

    public void SetReticle(Sprite sprite, Color color, float scale, bool fadeToNothing)
    {
        _fadeToNothing = fadeToNothing;
        if (adsCrosshair != null)
        {
            adsCrosshair.sprite = sprite;
            adsCrosshair.color = new Color(color.r, color.g, color.b, 0f);
            adsCrosshair.rectTransform.localScale = Vector3.one * scale;
        }
    }

    public void ClearReticle()
    {
        _fadeToNothing = false;
        if (adsCrosshair != null)
            adsCrosshair.sprite = null;
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

        _currentSpread = Mathf.Lerp(_currentSpread, targetSpread, spreadLerpSpeed * Time.deltaTime);

        ApplyLayout(_currentSpread);

        // Recoil follow
        Vector2 targetOffset = Vector2.zero;
        if (weaponRecoil != null)
        {
            Vector3 posKick = weaponRecoil.targetPosition - weaponRecoil.originalLocalPosition;
            Vector2 posOffset = new Vector2(
                posKick.x * recoilFollowPixelScale,
                posKick.y * recoilFollowPixelScale
            ) * recoilFollowStrength;

            Quaternion rotDelta = weaponRecoil.targetRotation * Quaternion.Inverse(weaponRecoil.originalLocalRotation);
            Vector3 forwardKicked = rotDelta * Vector3.forward;
            Vector2 rotOffset = new Vector2(
                forwardKicked.x * recoilFollowPixelScale,
                forwardKicked.y * recoilFollowPixelScale
            ) * recoilFollowStrength;

            targetOffset = posOffset + rotOffset;
        }
        _reticleOffset = Vector2.Lerp(_reticleOffset, targetOffset, recoilReturnSpeed * Time.deltaTime);

        // Alpha
        float targetLineAlpha = isAiming
            ? (activeWeapon.adsFadeCrosshair || _fadeToNothing ? 0f : adsAlpha)
            : 1f;

        Color targetColor = isAiming ? adsColor : normalColor;
        targetColor.a = targetLineAlpha;

        foreach (Image img in _lineImgs)
            img.color = Color.Lerp(img.color, targetColor, spreadLerpSpeed * Time.deltaTime);

        if (adsCrosshair != null)
        {
            float targetAdsAlpha = isAiming && !_fadeToNothing && adsCrosshair.sprite != null ? 1f : 0f;
            Color ac = adsCrosshair.color;
            ac.a = Mathf.Lerp(ac.a, targetAdsAlpha, adsSwapFadeSpeed * Time.deltaTime);
            adsCrosshair.color = ac;
            adsCrosshair.rectTransform.anchoredPosition = _reticleOffset;
        }

        if (isAiming)
        {
            foreach (RectTransform rt in _lineRTs)
                rt.anchoredPosition += _reticleOffset;
        }
    }

    void ApplyLayout(float spread)
    {
        (Vector2 pos, Vector2 size)[] configs =
        {
            (new Vector2(0f,  spread + armLength * 0.5f), new Vector2(lineThickness, armLength)),
            (new Vector2(0f, -spread - armLength * 0.5f), new Vector2(lineThickness, armLength)),
            (new Vector2(-spread - armLength * 0.5f, 0f), new Vector2(armLength, lineThickness)),
            (new Vector2( spread + armLength * 0.5f, 0f), new Vector2(armLength, lineThickness)),
        };

        for (int i = 0; i < 4; i++)
        {
            _lineRTs[i].anchoredPosition = configs[i].pos;
            _lineRTs[i].sizeDelta = configs[i].size;
        }
    }
}