using UnityEngine;
using System.Collections;

public class ZombieHitFlash : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer[] renderers;
    [SerializeField] Color bodyFlashColor = Color.white;
    [SerializeField] Color headshotFlashColor = Color.red;
    [SerializeField] float flashDuration = 0.08f;

    MaterialPropertyBlock mpb;
    Coroutine flashRoutine;

    static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    public void Flash(bool isHeadshot)
    {
        if (!gameObject.activeInHierarchy) return;

        Color color = isHeadshot ? headshotFlashColor : bodyFlashColor;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashCoroutine(color));
    }

    // Call this whenever the zombie is reset for pool reuse. Guarantees the
    // flash is cleared even if the previous flash coroutine got cut off
    // mid-fade by the object being deactivated (e.g. died while flashing).
    public void ForceReset()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        SetFlash(Color.white, 0f);
    }

    IEnumerator FlashCoroutine(Color color)
    {
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            SetFlash(color, 1f - (t / flashDuration));
            yield return null;
        }
        SetFlash(color, 0f);
        flashRoutine = null;
    }

    void SetFlash(Color color, float amount)
    {
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(FlashColorID, color);
            mpb.SetFloat(FlashAmountID, amount);
            r.SetPropertyBlock(mpb);
        }
    }
}