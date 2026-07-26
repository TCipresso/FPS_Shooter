using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class WeaponPickup : NetworkBehaviour
{
    [Header("Weapon")]
    public WeaponDefinitionSO definition;

    [Header("Skin (Pack-a-Punch)")]
    [Tooltip("Renderers on this prefab that receive the packed material + level tint once level > 1.")]
    public Renderer[] skinRenderers;

    [Header("Juice")]
    public Transform visualRoot;
    public float spinSpeed = 45f;
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;

    [SyncVar(hook = nameof(OnLevelChanged))]
    public int level = 1;

    static MaterialPropertyBlock sharedBlock;
    Material[][] originalMaterials;
    Vector3 baseLocalPos;

    void Awake()
    {
        if (visualRoot != null)
            baseLocalPos = visualRoot.localPosition;

        CacheOriginalMaterials();
    }

    void CacheOriginalMaterials()
    {
        if (skinRenderers == null) return;

        originalMaterials = new Material[skinRenderers.Length][];
        for (int i = 0; i < skinRenderers.Length; i++)
        {
            if (skinRenderers[i] != null)
                originalMaterials[i] = (Material[])skinRenderers[i].sharedMaterials.Clone();
        }
    }

    [Server]
    public void Initialize(int weaponLevel)
    {
        level = weaponLevel;
    }

    public override void OnStartClient()
    {
        ApplySkin();
    }

    void OnLevelChanged(int oldValue, int newValue) => ApplySkin();

    void Update()
    {
        if (visualRoot == null) return;

        if (spinSpeed != 0f)
            visualRoot.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (bobHeight != 0f)
        {
            float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            visualRoot.localPosition = baseLocalPos + new Vector3(0f, offset, 0f);
        }
    }

    void ApplySkin()
    {
        if (skinRenderers == null || definition == null) return;

        for (int r = 0; r < skinRenderers.Length; r++)
        {
            Renderer renderer = skinRenderers[r];
            if (renderer == null) continue;

            if (level <= 1 || definition.packedMaterial == null)
            {
                if (originalMaterials != null && originalMaterials[r] != null)
                    renderer.sharedMaterials = originalMaterials[r];
                renderer.SetPropertyBlock(null);
                continue;
            }

            int slotCount = renderer.sharedMaterials.Length;
            Material[] packedSet = new Material[slotCount];
            for (int i = 0; i < slotCount; i++)
                packedSet[i] = definition.packedMaterial;
            renderer.sharedMaterials = packedSet;

            if (sharedBlock == null)
                sharedBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(sharedBlock);

            int tintIndex = level - 2;
            Color tint = (definition.levelTintColors != null && tintIndex >= 0 && tintIndex < definition.levelTintColors.Length)
                ? definition.levelTintColors[tintIndex]
                : Color.white;

            sharedBlock.SetColor(definition.tintPropertyName, tint);
            renderer.SetPropertyBlock(sharedBlock);
        }
    }

    public string BuildPrompt()
    {
        string name = definition != null ? definition.weaponName : "Weapon";
        return level > 1
            ? $"Press E to swap for {name} (Lv {level})"
            : $"Press E to swap for {name}";
    }
}