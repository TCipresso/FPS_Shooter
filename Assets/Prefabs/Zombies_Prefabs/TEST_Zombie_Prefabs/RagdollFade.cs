using UnityEngine;
using System.Collections;

public class RagdollFade : MonoBehaviour
{
    public float fadeDuration = 1.5f;
    public float fadeDelay = 0.2f;

    private Renderer[] _renderers;

    void OnEnable()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        SetAlpha(1f);

        yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(1f - (elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        gameObject.SetActive(false);
    }

    void SetAlpha(float alpha)
    {
        foreach (var r in _renderers)
            foreach (var mat in r.materials)
                mat.SetFloat("_Fade", alpha);
    }
}