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

    [Header("Color")]
    public Color normalColor = Color.white;

    [Header("Weapon Reference")]
    public WeaponInventory weaponInventory;

    [Header("Weapon Recoil Follow")]
    public WeaponRecoil weaponRecoil;
    [Range(0f, 1f)] public float recoilFollowStrength = 0.4f;
    public float recoilFollowPixelScale = 120f;
    public float recoilReturnSpeed = 40f;

    // Right hand — normal crosshair (slot 1)
    RectTransform _rightContainer;
    RectTransform[] _rightRTs = new RectTransform[4];
    Image[] _rightImgs = new Image[4];
    float _rightSpread;

    // Left hand — 45-degree crosshair (slot 0)
    RectTransform _leftContainer;
    RectTransform[] _leftRTs = new RectTransform[4];
    Image[] _leftImgs = new Image[4];
    float _leftSpread;

    Vector2 _reticleOffset;

    void Awake()
    {
        _rightContainer = BuildContainer("Crosshair_Right", 0f);
        BuildLines(_rightContainer, _rightRTs, _rightImgs, "Right");

        _leftContainer = BuildContainer("Crosshair_Left", 45f);
        BuildLines(_leftContainer, _leftRTs, _leftImgs, "Left");
    }

    RectTransform BuildContainer(string name, float rotationDeg)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.localRotation = Quaternion.Euler(0f, 0f, rotationDeg);
        return rt;
    }

    void BuildLines(RectTransform container, RectTransform[] rts, Image[] imgs, string label)
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject go = new GameObject($"Line_{label}_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(container, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localRotation = Quaternion.identity;
            rts[i] = rt;
            imgs[i] = go.GetComponent<Image>();
            imgs[i].color = normalColor;
        }
    }

    public void ClearReticle() { }
    public void SetReticle(Sprite sprite, Color color, float scale, bool fadeToNothing) { }

    void Update()
    {
        if (weaponInventory == null) return;

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

        UpdateHand(1, _rightRTs, _rightImgs, ref _rightSpread);

        WeaponBase leftWeapon = weaponInventory.GetWeaponBase(0);
        UpdateHand(0, _leftRTs, _leftImgs, ref _leftSpread, leftWeapon != null);
    }

    void UpdateHand(int slot, RectTransform[] rts, Image[] imgs, ref float currentSpread, bool visible = true)
    {
        float targetAlpha = visible ? 1f : 0f;
        Color targetColor = normalColor;
        targetColor.a = targetAlpha;
        foreach (Image img in imgs)
            img.color = Color.Lerp(img.color, targetColor, spreadLerpSpeed * Time.deltaTime);

        if (!visible) return;

        WeaponBase wb = weaponInventory.GetWeaponBase(slot);
        float bloom = wb != null ? wb.currentBloom : 0f;

        float targetSpread = Mathf.Max(baseSpread + bloom * bloomSpreadMultiplier, minSpread);
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, spreadLerpSpeed * Time.deltaTime);

        ApplyLayout(rts, currentSpread);
    }

    void ApplyLayout(RectTransform[] rts, float spread)
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
            rts[i].anchoredPosition = configs[i].pos;
            rts[i].sizeDelta = configs[i].size;
        }
    }
}