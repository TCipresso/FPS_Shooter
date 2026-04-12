using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HitMarkerPool : MonoBehaviour
{
    public static HitMarkerPool Instance { get; private set; }

    [Header("Settings")]
    public Sprite hitMarkerSprite;
    public int poolSize = 10;
    public float fadeTime = 0.4f;
    public Vector2 markerSize = new Vector2(32f, 32f);
    public Color markerColor = Color.red;

    [Header("Rotation")]
    public float minRotation = 0f;
    public float maxRotation = 65f;

    List<Image> pool = new List<Image>();
    Canvas canvas;
    Camera mainCamera;

    void Awake()
    {
        Instance = this;
        canvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject("HitMarker", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            Image img = go.GetComponent<Image>();
            img.sprite = hitMarkerSprite;
            img.color = markerColor;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = markerSize;
            rt.anchoredPosition = Vector2.zero;

            go.SetActive(false);
            pool.Add(img);
        }
    }

    public void Spawn(Vector3 worldHitPoint)
    {
        Image marker = GetFromPool();
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

        Color c = markerColor;
        c.a = 1f;
        marker.color = c;

        marker.gameObject.SetActive(true);
        StartCoroutine(FadeOut(marker));
    }

    Image GetFromPool()
    {
        foreach (var m in pool)
            if (!m.gameObject.activeSelf) return m;
        return null;
    }

    IEnumerator FadeOut(Image marker)
    {
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            Color c = marker.color;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            marker.color = c;
            yield return null;
        }

        marker.gameObject.SetActive(false);
    }
}