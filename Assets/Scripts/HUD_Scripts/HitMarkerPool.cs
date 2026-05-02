using UnityEngine;
using UnityEngine.UI;

public class HitMarkerPool : MonoBehaviour
{
    public static HitMarkerPool Instance { get; private set; }

    [Header("Normal Hit Marker")]
    public Sprite hitMarkerSprite;
    public Color markerColor = Color.red;
    public Vector2 markerSize = new Vector2(32f, 32f);

    [Header("Crit Hit Marker")]
    public Sprite critMarkerSprite;
    public Color critMarkerColor = Color.yellow;
    public Vector2 critMarkerSize = new Vector2(48f, 48f);

    [Header("Hit Sounds (2D)")]
    public AudioSource hitSoundSource2D;
    public AudioClip critSound;
    [Range(0f, 1f)] public float critVolume = 1f;
    public AudioClip[] bodyHitSounds;
    [Range(0f, 1f)] public float bodyHitVolume = 1f;

    [Header("Settings")]
    public float fadeTime = 0.4f;
    public float minRotation = 0f;
    public float maxRotation = 65f;

    // Shuffle bag: index into bodyHitSounds, no allocations per refill.
    int[] bagOrder;
    int bagIndex = 0;

    Image marker;
    Canvas canvas;
    Camera mainCamera;
    Color activeColor;
    float fadeElapsed = -1f;

    void Awake()
    {
        Instance = this;
        canvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;
        marker = CreateMarker();

        if (bodyHitSounds != null && bodyHitSounds.Length > 0)
        {
            bagOrder = new int[bodyHitSounds.Length];
            for (int i = 0; i < bagOrder.Length; i++) bagOrder[i] = i;
            ShuffleBag();
        }
    }

    Image CreateMarker()
    {
        GameObject go = new GameObject("HitMarker", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        Image img = go.GetComponent<Image>();
        img.color = markerColor;
        go.GetComponent<RectTransform>().sizeDelta = markerSize;
        go.SetActive(false);
        return img;
    }

    public void Spawn(Vector3 worldHitPoint, bool isCrit = false)
    {
        // Audio — all 2D through one shared source, no allocations.
        if (hitSoundSource2D != null)
        {
            if (isCrit)
            {
                if (critSound != null)
                    hitSoundSource2D.PlayOneShot(critSound, critVolume);
            }
            else if (bagOrder != null && bagOrder.Length > 0)
            {
                AudioClip clip = bodyHitSounds[bagOrder[bagIndex]];
                bagIndex++;
                if (bagIndex >= bagOrder.Length)
                {
                    ShuffleBag();
                    bagIndex = 0;
                }
                if (clip != null)
                    hitSoundSource2D.PlayOneShot(clip, bodyHitVolume);
            }
        }

        // Visuals
        activeColor = isCrit ? critMarkerColor : markerColor;
        marker.sprite = isCrit ? critMarkerSprite : hitMarkerSprite;

        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.sizeDelta = isCrit ? critMarkerSize : markerSize;

        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldHitPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
            out Vector2 localPoint
        );

        rt.SetAsLastSibling();
        rt.anchoredPosition = localPoint;
        rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(minRotation, maxRotation));

        Color c = activeColor;
        c.a = 1f;
        marker.color = c;
        marker.gameObject.SetActive(true);
        fadeElapsed = 0f;
    }

    void Update()
    {
        if (fadeElapsed < 0f) return;
        fadeElapsed += Time.deltaTime;
        Color c = activeColor;
        c.a = Mathf.Lerp(1f, 0f, fadeElapsed / fadeTime);
        marker.color = c;
        if (fadeElapsed >= fadeTime)
        {
            marker.gameObject.SetActive(false);
            fadeElapsed = -1f;
        }
    }

    // Fisher-Yates in-place shuffle. No allocations.
    void ShuffleBag()
    {
        for (int i = bagOrder.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = bagOrder[i];
            bagOrder[i] = bagOrder[j];
            bagOrder[j] = tmp;
        }
    }
}