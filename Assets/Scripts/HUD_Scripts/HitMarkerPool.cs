using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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

    Queue<AudioClip> _bodyHitBag = new Queue<AudioClip>();
    public int poolSize = 10;
    public float fadeTime = 0.4f;

    [Header("Rotation")]
    public float minRotation = 0f;
    public float maxRotation = 65f;

    List<Image> pool = new List<Image>();
    List<Image> critPool = new List<Image>();
    Canvas canvas;
    Camera mainCamera;

    void Awake()
    {
        Instance = this;
        canvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;

        for (int i = 0; i < poolSize; i++)
        {
            pool.Add(CreateMarker("HitMarker", hitMarkerSprite, markerColor, markerSize));
            critPool.Add(CreateMarker("CritMarker", critMarkerSprite, critMarkerColor, critMarkerSize));
        }
    }

    Image CreateMarker(string name, Sprite sprite, Color color, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.SetActive(false);
        return img;
    }

    public void Spawn(Vector3 worldHitPoint, bool isCrit = false)
    {
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

        List<Image> targetPool = isCrit ? critPool : pool;
        Color targetColor = isCrit ? critMarkerColor : markerColor;

        Image marker = GetFromPool(targetPool);
        if (marker == null) return;

        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldHitPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
            out Vector2 localPoint
        );

        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.SetAsLastSibling();
        rt.anchoredPosition = localPoint;
        rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(minRotation, maxRotation));

        Color c = targetColor;
        c.a = 1f;
        marker.color = c;
        marker.gameObject.SetActive(true);
        StartCoroutine(FadeOut(marker, targetColor));
    }

    void RefillBodyHitBag()
    {
        var pool = new List<AudioClip>(bodyHitSounds);
        while (pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            _bodyHitBag.Enqueue(pool[i]);
            pool.RemoveAt(i);
        }
    }

    Image GetFromPool(List<Image> targetPool)
    {
        foreach (var m in targetPool)
            if (!m.gameObject.activeSelf) return m;
        return null;
    }

    IEnumerator FadeOut(Image marker, Color baseColor)
    {
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            Color c = baseColor;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            marker.color = c;
            yield return null;
        }
        marker.gameObject.SetActive(false);
    }
}