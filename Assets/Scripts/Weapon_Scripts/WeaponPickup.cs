using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class WeaponPickup : NetworkBehaviour
{
    [Header("Registry")]
    public WeaponRegistrySO registry;

    [Header("Display")]
    [Tooltip("MeshFilter on the child that shows the weapon model. Its mesh is swapped per-weapon at runtime.")]
    public MeshFilter displayMeshFilter;
    [Tooltip("Renderer on the same child. Receives the weapon's ground materials, then the pack-a-punch skin on top.")]
    public MeshRenderer displayRenderer;

    [Tooltip("Optional root spun/bobbed for juice. Leave null to keep it static.")]
    public Transform visualRoot;
    public float spinSpeed = 45f;
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;

    [SyncVar(hook = nameof(OnDefIndexChanged))]
    public int defIndex = -1;

    [SyncVar(hook = nameof(OnLevelChanged))]
    public int level = 1;

    Material[] groundMaterials;
    static MaterialPropertyBlock sharedBlock;
    Vector3 baseLocalPos;
    bool modelApplied;

    public WeaponDefinitionSO Definition =>
        registry != null ? registry.GetDefinition(defIndex) : null;

    void Awake()
    {
        if (visualRoot != null)
            baseLocalPos = visualRoot.localPosition;
    }

    [Server]
    public void Initialize(int weaponDefIndex, int weaponLevel)
    {
        defIndex = weaponDefIndex;
        level = weaponLevel;
    }

    public override void OnStartClient()
    {
        ApplyModel();
        ApplySkin();
    }

    void OnDefIndexChanged(int oldValue, int newValue)
    {
        modelApplied = false;
        ApplyModel();
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

    void ApplyModel()
    {
        if (modelApplied) return;

        WeaponDefinitionSO def = Definition;
        if (def == null || registry == null) return;

        WeaponGroundModel model = registry.GetGroundModel(def);
        if (model == null) return;

        if (displayMeshFilter != null && model.mesh != null)
            displayMeshFilter.sharedMesh = model.mesh;

        if (displayRenderer != null && model.materials != null && model.materials.Length > 0)
        {
            groundMaterials = (Material[])model.materials.Clone();
            displayRenderer.sharedMaterials = groundMaterials;
        }

        Transform t = displayMeshFilter != null ? displayMeshFilter.transform
                    : displayRenderer != null ? displayRenderer.transform
                    : null;

        if (t != null)
        {
            t.localPosition = model.localPosition;
            t.localEulerAngles = model.localEuler;
            t.localScale = model.localScale;
        }

        modelApplied = true;
    }

    void ApplySkin()
    {
        if (displayRenderer == null) return;

        WeaponDefinitionSO def = Definition;
        if (def == null) return;

        if (level <= 1 || def.packedMaterial == null)
        {
            if (groundMaterials != null)
                displayRenderer.sharedMaterials = groundMaterials;
            displayRenderer.SetPropertyBlock(null);
            return;
        }

        int slotCount = displayRenderer.sharedMaterials.Length;
        Material[] packedSet = new Material[slotCount];
        for (int i = 0; i < slotCount; i++)
            packedSet[i] = def.packedMaterial;
        displayRenderer.sharedMaterials = packedSet;

        if (sharedBlock == null)
            sharedBlock = new MaterialPropertyBlock();

        displayRenderer.GetPropertyBlock(sharedBlock);

        int tintIndex = level - 2;
        Color tint = (def.levelTintColors != null && tintIndex >= 0 && tintIndex < def.levelTintColors.Length)
            ? def.levelTintColors[tintIndex]
            : Color.white;

        sharedBlock.SetColor(def.tintPropertyName, tint);
        displayRenderer.SetPropertyBlock(sharedBlock);
    }

    public string BuildPrompt()
    {
        WeaponDefinitionSO def = Definition;
        string name = def != null ? def.weaponName : "Weapon";
        return level > 1
            ? $"Press E to swap for {name} (Lv {level})"
            : $"Press E to swap for {name}";
    }
}