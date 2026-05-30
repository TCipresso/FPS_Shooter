using UnityEngine;
using System.Collections;

public class ZombieHitFlash : MonoBehaviour
{
    [Header("Body Hit")]
    public Color bodyHitColor = Color.white;
    public float bodyHitDuration = 0.08f;

    [Header("Crit Hit")]
    public Color critHitColor = Color.red;
    public float critHitDuration = 0.12f;

    [Header("References")]
    public Renderer[] renderers;

    MaterialPropertyBlock propBlock;
    static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    Coroutine flashCoroutine;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    public void Flash(bool isCrit)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(DoFlash(
            isCrit ? critHitColor : bodyHitColor,
            isCrit ? critHitDuration : bodyHitDuration));
    }

    IEnumerator DoFlash(Color color, float duration)
    {
        SetFlash(color, 1f);
        yield return new WaitForSeconds(duration);
        SetFlash(color, 0f);
        flashCoroutine = null;
    }

    void SetFlash(Color color, float amount)
    {
        Debug.Log($"SetFlash called — amount: {amount}, color: {color}");
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor(FlashColorID, color);
            propBlock.SetFloat(FlashAmountID, amount);
            r.SetPropertyBlock(propBlock);
        }
    }
}