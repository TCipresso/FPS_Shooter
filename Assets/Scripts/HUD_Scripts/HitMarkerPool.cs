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

    [Header("Crit Hit Marker")]
    public Sprite critMarkerSprite;
    public Color critMarkerColor = Color.yellow;

    [Header("Settings")]
    public int poolSize = 10;
    public float fadeTime = 0.4f;
    public Vector2 markerSize = new Vector2(32f, 32f);

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
            pool.Add(CreateMarker("HitMarker", hitMarkerSprite, markerColor));
            critPool.Add(CreateMarker("CritMarker", critMarkerSprite, critMarkerColor));
        }
    }

    Image CreateMarker(string name, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = markerSize;
        rt.anchoredPosition = Vector2.zero;
        go.SetActive(false);
        return img;
    }

    public void Spawn(Vector3 worldHitPoint, bool isCrit = false)
    {
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