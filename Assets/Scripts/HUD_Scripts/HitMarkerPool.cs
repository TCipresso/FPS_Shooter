using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("Hit Sounds")]
    public AudioSource hitSoundSource2D;
    public AudioClip critSound;
    public AudioClip[] bodyHitSounds;
    public float bodyHitVolume = 1f;

    [Header("Settings")]
    public float fadeTime = 0.4f;
    public float minRotation = 0f;
    public float maxRotation = 65f;

    Queue<AudioClip> _bodyHitBag = new Queue<AudioClip>();

    Image marker;
    Canvas canvas;
    Camera mainCamera;
    Color activeColor;
    float fadeElapsed = -1f; // -1 means inactive

    void Awake()
    {
        Instance = this;
        canvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;
        marker = CreateMarker();
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
        // Audio
        if (isCrit)
        {
            if (hitSoundSource2D != null && critSound != null)
                hitSoundSource2D.PlayOneShot(critSound);
        }
        else
        {
            if (bodyHitSounds != null && bodyHitSounds.Length > 0)
            {
                if (_bodyHitBag.Count == 0) RefillBodyHitBag();
                AudioSource.PlayClipAtPoint(_bodyHitBag.Dequeue(), worldHitPoint, bodyHitVolume);
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

        fadeElapsed = 0f; // reset and start fade
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

    void RefillBodyHitBag()
    {
        var list = new List<AudioClip>(bodyHitSounds);
        while (list.Count > 0)
        {
            int i = Random.Range(0, list.Count);
            _bodyHitBag.Enqueue(list[i]);
            list.RemoveAt(i);
        }
    }
}