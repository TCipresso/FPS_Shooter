using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyHealthBarManager : MonoBehaviour
{
    public static EnemyHealthBarManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] RectTransform barRoot;
    [SerializeField] Image fillImage;
    [SerializeField] CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] Vector2 screenOffset = new Vector2(0f, 60f);
    [SerializeField] float offsetDistanceScale = 10f;
    [SerializeField] float fadeDuration = 1.2f;
    [SerializeField] float deathFadeDuration = 0.15f;

    Camera cam;
    ZombieBase currentTarget;
    Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        cam = Camera.main;
        canvasGroup.alpha = 0f;
        barRoot.gameObject.SetActive(false);
    }

    public void SetTarget(ZombieBase zombie)
    {
        if (zombie == currentTarget) return;

        Unsubscribe();

        currentTarget = zombie;

        if (currentTarget == null)
        {
            BeginFade(fadeDuration);
            return;
        }

        currentTarget.OnHealthChanged += HandleHealthChanged;

        UpdateFill(currentTarget.currentHealth, currentTarget.maxHealth);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        canvasGroup.alpha = 1f;
        barRoot.gameObject.SetActive(true);
    }

    public void ClearTarget()
    {
        if (currentTarget == null) return;

        Unsubscribe();
        currentTarget = null;
        BeginFade(fadeDuration);
    }

    void LateUpdate()
    {
        // currentTarget goes null the moment Unity destroys the zombie GameObject
        // Always restart the fade when that happens — no guard, BeginFade handles it
        if (currentTarget == null)
        {
            if (canvasGroup.alpha > 0f)
                BeginFade(deathFadeDuration);
            return;
        }

        if (currentTarget.headTransform == null) return;
        if (canvasGroup.alpha <= 0f) return;

        Vector3 screenPos = cam.WorldToScreenPoint(currentTarget.headTransform.position);

        if (screenPos.z < 0f)
        {
            barRoot.gameObject.SetActive(false);
            return;
        }

        barRoot.gameObject.SetActive(true);

        float distance = Vector3.Distance(cam.transform.position, currentTarget.headTransform.position);
        float scaledOffset = screenOffset.y / distance * offsetDistanceScale;
        barRoot.position = (Vector2)screenPos + new Vector2(screenOffset.x, scaledOffset);
    }

    void HandleHealthChanged(int current, int max)
    {
        UpdateFill(current, max);
    }

    void UpdateFill(int current, int max)
    {
        fillImage.fillAmount = (float)current / max;
    }

    void Unsubscribe()
    {
        if (currentTarget == null) return;
        currentTarget.OnHealthChanged -= HandleHealthChanged;
    }

    void BeginFade(float duration)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut(duration));
    }

    IEnumerator FadeOut(float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        barRoot.gameObject.SetActive(false);
        fadeCoroutine = null;
    }

    void OnDestroy()
    {
        Unsubscribe();
    }
}