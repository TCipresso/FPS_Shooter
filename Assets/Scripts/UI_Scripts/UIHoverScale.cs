using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    public float hoverScale = 1.2f;       // how big it gets when hovered
    public float scaleSpeed = 8f;         // how fast it scales
    public bool resetOnDisable = true;    // reset if hidden

    Vector3 originalScale;
    Vector3 targetScale;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void OnEnable()
    {
        if (resetOnDisable)
        {
            transform.localScale = originalScale;
            targetScale = originalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime));
    }
}
