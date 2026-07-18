using UnityEngine;

public class ZombieHitFlash : MonoBehaviour
{
    [SerializeField] MeshRenderer[] renderers;
    [SerializeField] Color bodyFlashColor = Color.white;
    [SerializeField] Color headshotFlashColor = Color.red;
    [SerializeField] float flashDuration = 0.08f;

    MaterialPropertyBlock mpb;
    float flashTimer;
    Color activeColor;

    static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<MeshRenderer>();
        enabled = false;
    }

    public void Flash(bool isHeadshot)
    {
        if (!gameObject.activeInHierarchy) return;
        activeColor = isHeadshot ? headshotFlashColor : bodyFlashColor;
        flashTimer = flashDuration;
        enabled = true;
        ApplyFlash(1f);
    }

    public void ForceReset()
    {
        flashTimer = 0f;
        enabled = false;
        ClearFlash();
    }

    void Update()
    {
        flashTimer -= Time.deltaTime;

        if (flashTimer <= 0f)
        {
            ClearFlash();
            enabled = false;
            return;
        }

        ApplyFlash(flashTimer / flashDuration);
    }

    void ApplyFlash(float amount)
    {
        mpb.SetColor(FlashColorID, activeColor);
        mpb.SetFloat(FlashAmountID, amount);
        foreach (var r in renderers)
            r.SetPropertyBlock(mpb);
    }

    void ClearFlash()
    {
        foreach (var r in renderers)
            r.SetPropertyBlock(null);
    }
}