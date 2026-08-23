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
    public Color rightColor = Color.white; // normal (+) crosshair
    public Color leftColor = Color.red;    // diagonal (x) crosshair

    [Header("Weapon Reference")]
    public WeaponInventory weaponInventory;

    [Header("Weapon Recoil Follow")]
    public WeaponRecoil rightWeaponRecoil;
    public WeaponRecoil leftWeaponRecoil;
    [Range(0f, 1f)] public float recoilFollowStrength = 0.4f;
    public float recoilFollowPixelScale = 120f;
    public float recoilReturnSpeed = 40f;

    class CrosshairGroup
    {
        public float[] angles;
        public RectTransform[] lineRTs = new RectTransform[4];
        public Image[] lineImgs = new Image[4];
        public float currentSpread;
        public Vector2 reticleOffset;
    }

    // Right hand = normal crosshair (up/down/left/right)
    readonly CrosshairGroup rightGroup = new CrosshairGroup { angles = new float[] { 0f, 90f, 180f, 270f } };
    // Left hand = diagonal crosshair (rotated 45 degrees)
    readonly CrosshairGroup leftGroup = new CrosshairGroup { angles = new float[] { 45f, 135f, 225f, 315f } };

    void Awake()
    {
        BuildGroup(rightGroup, "Right", rightColor);
        BuildGroup(leftGroup, "Left", leftColor);
    }

    void BuildGroup(CrosshairGroup group, string label, Color color)
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject go = new GameObject($"Crosshair_{label}_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            group.lineRTs[i] = rt;
            group.lineImgs[i] = go.GetComponent<Image>();
            group.lineImgs[i].color = color;
        }
    }

    void Update()
    {
        if (weaponInventory == null) return;

        UpdateGroup(rightGroup, weaponInventory.GetActiveWeapon(WeaponInventory.Hand.Right), rightColor, rightWeaponRecoil);
        UpdateGroup(leftGroup, weaponInventory.GetActiveWeapon(WeaponInventory.Hand.Left), leftColor, leftWeaponRecoil);
    }

    void UpdateGroup(CrosshairGroup group, WeaponBase activeWeapon, Color baseColor, WeaponRecoil recoil)
    {
        bool hasWeapon = activeWeapon != null;
        float bloom = hasWeapon ? activeWeapon.currentBloom : 0f;
        float targetSpread = Mathf.Max(baseSpread + bloom * bloomSpreadMultiplier, minSpread);
        group.currentSpread = Mathf.Lerp(group.currentSpread, targetSpread, spreadLerpSpeed * Time.deltaTime);

        Vector2 targetOffset = Vector2.zero;
        if (recoil != null)
        {
            Vector3 posKick = recoil.targetPosition - recoil.originalLocalPosition;
            Vector2 posOffset = new Vector2(posKick.x, posKick.y) * recoilFollowPixelScale * recoilFollowStrength;

            Quaternion rotDelta = recoil.targetRotation * Quaternion.Inverse(recoil.originalLocalRotation);
            Vector3 forwardKicked = rotDelta * Vector3.forward;
            Vector2 rotOffset = new Vector2(forwardKicked.x, forwardKicked.y) * recoilFollowPixelScale * recoilFollowStrength;

            targetOffset = posOffset + rotOffset;
        }
        group.reticleOffset = Vector2.Lerp(group.reticleOffset, targetOffset, recoilReturnSpeed * Time.deltaTime);

        // fade out when that hand has no weapon equipped
        Color targetColor = hasWeapon ? baseColor : new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        for (int i = 0; i < 4; i++)
        {
            float angle = group.angles[i];
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            group.lineRTs[i].anchoredPosition = dir * (group.currentSpread + armLength * 0.5f) + group.reticleOffset;
            group.lineRTs[i].localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            group.lineRTs[i].sizeDelta = new Vector2(lineThickness, armLength);

            group.lineImgs[i].color = Color.Lerp(group.lineImgs[i].color, targetColor, spreadLerpSpeed * Time.deltaTime);
        }
    }
}