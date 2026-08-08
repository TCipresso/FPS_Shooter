using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : MonoBehaviour
{
    public const int SlotCount = 2;

    [Header("References")]
    public Transform weaponHolder;
    public PlayerStats playerStats;
    public IKWeaponHandler ikHandler;

    [Header("Input")]
    public InputActionReference fireAction;
    public InputActionReference swapAction;

    [Header("Starting Loadout (element 0 = slot 1, element 1 = slot 2)")]
    public List<WeaponDefinitionSO> startingWeapons = new List<WeaponDefinitionSO>();

    [Header("Weapons (drag pre-placed, disabled weapon children here)")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();

    private readonly WeaponEntry[] slots = new WeaponEntry[SlotCount];
    private Dictionary<WeaponDefinitionSO, WeaponEntry> weaponLookup = new Dictionary<WeaponDefinitionSO, WeaponEntry>();
    private Dictionary<WeaponBase, Material[]> originalMaterialsCache = new Dictionary<WeaponBase, Material[]>();
    private int activeSlot = 0;

    void Awake()
    {
        weaponLookup.Clear();
        originalMaterialsCache.Clear();

        foreach (WeaponEntry entry in weapons)
        {
            if (entry == null || entry.weaponRoot == null || entry.definition == null
                || entry.weaponBases == null || entry.weaponBases.Count == 0)
            {
                Debug.LogWarning("[WeaponInventory] Skipping invalid WeaponEntry.");
                continue;
            }

            weaponLookup[entry.definition] = entry;

            if (entry.level <= 0)
                entry.level = 1;

            foreach (WeaponBase wb in entry.weaponBases)
            {
                if (wb == null) continue;
                if (wb.skinRenderer != null && !originalMaterialsCache.ContainsKey(wb))
                    originalMaterialsCache[wb] = (Material[])wb.skinRenderer.sharedMaterials.Clone();
            }
        }

        if (fireAction != null) fireAction.action.Enable();
        if (swapAction != null) swapAction.action.Enable();
    }

    void OnDisable()
    {
        if (fireAction != null) fireAction.action.Disable();
        if (swapAction != null) swapAction.action.Disable();
    }

    void Start()
    {
        foreach (WeaponEntry entry in weapons)
        {
            if (entry?.weaponRoot == null) continue;

            entry.weaponRoot.SetActive(false);

            foreach (WeaponBase wb in entry.weaponBases)
            {
                if (wb == null) continue;

                if (BulletPool.Instance != null && wb.bulletData != null && wb.bulletData.trailPrefab != null)
                    BulletPool.Instance.EnsurePoolSize(wb.bulletData.trailPoolKey, wb.bulletData.trailPrefab.gameObject, wb.bulletData.trailPoolSize);

                if (ProjectilePool.Instance != null && wb.bulletData != null
                    && wb.bulletData.bulletType == BulletType.Projectile && wb.bulletData.projectilePrefab != null)
                    ProjectilePool.Instance.EnsurePoolSize(wb.bulletData.projectilePrefab, 8);
            }
        }

        InitializeStartingLoadout();
    }

    void InitializeStartingLoadout()
    {
        for (int i = 0; i < SlotCount; i++)
            slots[i] = null;

        for (int i = 0; i < SlotCount && i < startingWeapons.Count; i++)
        {
            WeaponDefinitionSO def = startingWeapons[i];
            if (def == null) continue;

            if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
            {
                Debug.LogWarning($"[WeaponInventory] Starting weapon {def.weaponName} has no matching WeaponEntry.");
                continue;
            }

            slots[i] = entry;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == null) continue;
            EquipSlot(i);
            return;
        }

        Debug.LogWarning("[WeaponInventory] No starting weapons assigned.");
    }

    void Update()
    {
        if (swapAction != null && swapAction.action.WasPressedThisFrame())
            SwapWeapon();

        if (fireAction == null) return;

        List<WeaponBase> activeBases = GetActiveWeaponBases();
        if (activeBases == null || activeBases.Count == 0) return;

        WeaponBase primary = activeBases[0];
        bool shouldFire = primary != null && primary.isAutomatic
            ? fireAction.action.IsPressed()
            : fireAction.action.WasPressedThisFrame();

        if (shouldFire)
        {
            foreach (WeaponBase wb in activeBases)
                wb?.Shoot();
        }
        else if (fireAction.action.WasReleasedThisFrame())
        {
            foreach (WeaponBase wb in activeBases)
                wb?.StopRecoil();
        }
    }

    public void SwapWeapon()
    {
        int next = (activeSlot + 1) % SlotCount;
        if (slots[next] == null) return;
        EquipSlot(next);
    }

    public void EquipSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return;
        if (slots[slot] == null) return;

        activeSlot = slot;
        SetActiveEntry(slots[slot]);
    }

    public int AddWeapon(WeaponDefinitionSO def)
    {
        if (def == null) return -1;

        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot add weapon, no entry for {def.weaponName}.");
            return -1;
        }

        return AddEntry(entry);
    }

    public int AddWeaponByIndex(int index)
    {
        if (index < 0 || index >= weapons.Count)
        {
            Debug.LogWarning($"[WeaponInventory] AddWeaponByIndex: index {index} out of range.");
            return -1;
        }

        return AddEntry(weapons[index]);
    }

    public bool AddWeaponToSlot(WeaponDefinitionSO def, int slot)
    {
        if (def == null) return false;
        if (slot < 0 || slot >= SlotCount) return false;

        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot add weapon, no entry for {def.weaponName}.");
            return false;
        }

        slots[slot] = entry;
        EquipSlot(slot);
        return true;
    }

    int AddEntry(WeaponEntry entry)
    {
        if (entry?.weaponRoot == null || entry.definition == null) return -1;

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != entry) continue;
            EquipSlot(i);
            return i;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null) continue;
            slots[i] = entry;
            EquipSlot(i);
            return i;
        }

        slots[activeSlot] = entry;
        EquipSlot(activeSlot);
        return activeSlot;
    }

    public void RemoveWeapon(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return;
        if (slots[slot] == null) return;

        slots[slot] = null;

        if (slot != activeSlot) return;

        int other = (slot + 1) % SlotCount;
        if (slots[other] != null)
            EquipSlot(other);
        else if (slots[slot] == null && slots[other] == null)
            SetAllWeaponsInactive();
    }

    void SetAllWeaponsInactive()
    {
        foreach (WeaponEntry w in weapons)
        {
            if (w?.weaponRoot != null)
                w.weaponRoot.SetActive(false);
        }
    }

    public void LevelUpWeapon(WeaponDefinitionSO def)
    {
        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot level up, no entry for {def?.weaponName}.");
            return;
        }

        entry.level = Mathf.Min(entry.level + 1, entry.definition.maxLevel);

        if (slots[activeSlot] == entry)
            ApplyLevel(entry);
    }

    void SetActiveEntry(WeaponEntry entry)
    {
        if (entry?.weaponRoot == null) return;

        foreach (WeaponEntry w in weapons)
        {
            if (w?.weaponRoot != null)
                w.weaponRoot.SetActive(false);
        }

        entry.weaponRoot.SetActive(true);
        ApplyLevel(entry);

        WeaponBase primary = entry.Primary;
        if (primary != null)
            primary.LoadRecoilValues();

        if (ikHandler != null)
            ikHandler.UpdateIKTargets(entry.weaponRoot);
    }

    void ApplyLevel(WeaponEntry entry)
    {
        if (entry?.weaponBases == null || entry.definition == null) return;

        foreach (WeaponBase wb in entry.weaponBases)
        {
            if (wb == null) continue;

            wb.ApplyLevel(entry.definition, entry.level);
        }

        ApplyWeaponSkin(entry);
    }

    static MaterialPropertyBlock sharedPropertyBlock;

    void ApplyWeaponSkin(WeaponEntry entry)
    {
        WeaponDefinitionSO def = entry.definition;

        foreach (WeaponBase wb in entry.weaponBases)
        {
            if (wb == null) continue;

            Renderer renderer = wb.skinRenderer;
            if (renderer == null) continue;

            if (entry.level <= 1 || def.packedMaterial == null)
            {
                if (originalMaterialsCache.TryGetValue(wb, out Material[] original))
                    renderer.sharedMaterials = original;
                renderer.SetPropertyBlock(null);
                continue;
            }

            int slotCount = renderer.sharedMaterials.Length;
            Material[] packedSet = new Material[slotCount];
            for (int i = 0; i < slotCount; i++)
                packedSet[i] = def.packedMaterial;
            renderer.sharedMaterials = packedSet;

            if (sharedPropertyBlock == null)
                sharedPropertyBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(sharedPropertyBlock);

            int tintIndex = entry.level - 2;
            Color tint = (def.levelTintColors != null && tintIndex >= 0 && tintIndex < def.levelTintColors.Length)
                ? def.levelTintColors[tintIndex]
                : Color.white;

            sharedPropertyBlock.SetColor(def.tintPropertyName, tint);
            renderer.SetPropertyBlock(sharedPropertyBlock);
        }
    }

    public int ActiveSlot => activeSlot;

    public WeaponEntry GetSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return null;
        return slots[slot];
    }

    public bool HasWeapon(WeaponDefinitionSO def)
    {
        if (def == null) return false;
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null && slots[i].definition == def)
                return true;
        }
        return false;
    }

    public WeaponBase GetActiveWeaponBase()
    {
        return slots[activeSlot]?.Primary;
    }

    public List<WeaponBase> GetActiveWeaponBases()
    {
        return slots[activeSlot]?.weaponBases;
    }

    public int GetLevel(WeaponDefinitionSO def)
    {
        return weaponLookup.TryGetValue(def, out WeaponEntry entry) ? entry.level : 0;
    }
}