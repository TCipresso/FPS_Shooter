using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : MonoBehaviour
{
    [Header("References")]
    public Transform weaponHolder;
    public PlayerStats playerStats;
    public IKWeaponHandler ikHandler;
    public FPSInput input;

    [Header("Input")]
    public InputActionReference fireAction;
    public InputActionReference swapAction;

    [Header("Starting Loadout")]
    public List<WeaponDefinitionSO> startingWeapons = new List<WeaponDefinitionSO>();
    public OffHandDefinitionSO startingOffHand;

    [Header("Weapons (drag pre-placed, disabled weapon children here)")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();

    [Header("Off-Hand (drag pre-placed, disabled off-hand children here)")]
    public List<OffHandEntry> offHandItems = new List<OffHandEntry>();

    private readonly List<WeaponEntry> equippedWeapons = new List<WeaponEntry>();
    private Dictionary<WeaponDefinitionSO, WeaponEntry> weaponLookup = new Dictionary<WeaponDefinitionSO, WeaponEntry>();
    private Dictionary<WeaponBase, Material[]> originalMaterialsCache = new Dictionary<WeaponBase, Material[]>();
    private int activeIndex = -1;

    private OffHandEntry activeOffHand;
    private Dictionary<OffHandDefinitionSO, OffHandEntry> offHandLookup = new Dictionary<OffHandDefinitionSO, OffHandEntry>();

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

        offHandLookup.Clear();

        foreach (OffHandEntry entry in offHandItems)
        {
            if (entry == null || entry.offHandRoot == null || entry.definition == null
                || entry.offHandBases == null || entry.offHandBases.Count == 0)
            {
                Debug.LogWarning("[WeaponInventory] Skipping invalid OffHandEntry.");
                continue;
            }

            offHandLookup[entry.definition] = entry;
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

        foreach (OffHandEntry entry in offHandItems)
        {
            if (entry?.offHandRoot == null) continue;
            entry.offHandRoot.SetActive(false);
        }

        InitializeStartingLoadout();
    }

    void InitializeStartingLoadout()
    {
        equippedWeapons.Clear();
        activeIndex = -1;

        foreach (WeaponDefinitionSO def in startingWeapons)
        {
            if (def == null) continue;

            if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
            {
                Debug.LogWarning($"[WeaponInventory] Starting weapon {def.weaponName} has no matching WeaponEntry.");
                continue;
            }

            if (!equippedWeapons.Contains(entry))
                equippedWeapons.Add(entry);
        }

        if (equippedWeapons.Count > 0)
            EquipIndex(0);
        else
            Debug.LogWarning("[WeaponInventory] No starting weapons assigned.");

        if (startingOffHand != null)
            EquipOffHand(startingOffHand);
    }

    void Update()
    {
        if (swapAction != null && swapAction.action.WasPressedThisFrame())
            SwapWeapon();

        if (input != null && input.MeleePressed)
            GetActiveOffHandBase()?.Melee();

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
        if (equippedWeapons.Count <= 1) return;

        int next = (activeIndex + 1) % equippedWeapons.Count;
        EquipIndex(next);
    }

    public void EquipIndex(int index)
    {
        if (index < 0 || index >= equippedWeapons.Count) return;

        activeIndex = index;
        SetActiveEntry(equippedWeapons[index]);
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

    int AddEntry(WeaponEntry entry)
    {
        if (entry?.weaponRoot == null || entry.definition == null) return -1;

        int existingIndex = equippedWeapons.IndexOf(entry);
        if (existingIndex >= 0)
        {
            EquipIndex(existingIndex);
            return existingIndex;
        }

        equippedWeapons.Add(entry);
        int newIndex = equippedWeapons.Count - 1;
        EquipIndex(newIndex);
        return newIndex;
    }

    public void RemoveWeapon(WeaponDefinitionSO def)
    {
        if (def == null) return;
        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry)) return;
        RemoveEntry(entry);
    }

    public void RemoveWeaponAt(int index)
    {
        if (index < 0 || index >= equippedWeapons.Count) return;
        RemoveEntry(equippedWeapons[index]);
    }

    void RemoveEntry(WeaponEntry entry)
    {
        int index = equippedWeapons.IndexOf(entry);
        if (index < 0) return;

        bool wasActive = index == activeIndex;
        equippedWeapons.RemoveAt(index);

        if (entry.weaponRoot != null)
            entry.weaponRoot.SetActive(false);

        if (equippedWeapons.Count == 0)
        {
            activeIndex = -1;
            return;
        }

        if (wasActive)
        {
            int newIndex = Mathf.Clamp(index, 0, equippedWeapons.Count - 1);
            EquipIndex(newIndex);
        }
        else if (index < activeIndex)
        {
            activeIndex--;
        }
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

        if (activeIndex >= 0 && activeIndex < equippedWeapons.Count && equippedWeapons[activeIndex] == entry)
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

    public int ActiveIndex => activeIndex;
    public int EquippedCount => equippedWeapons.Count;

    public WeaponEntry GetEquippedAt(int index)
    {
        if (index < 0 || index >= equippedWeapons.Count) return null;
        return equippedWeapons[index];
    }

    public bool HasWeapon(WeaponDefinitionSO def)
    {
        if (def == null) return false;

        foreach (WeaponEntry entry in equippedWeapons)
        {
            if (entry != null && entry.definition == def)
                return true;
        }

        return false;
    }

    public WeaponBase GetActiveWeaponBase()
    {
        if (activeIndex < 0 || activeIndex >= equippedWeapons.Count) return null;
        return equippedWeapons[activeIndex]?.Primary;
    }

    public List<WeaponBase> GetActiveWeaponBases()
    {
        if (activeIndex < 0 || activeIndex >= equippedWeapons.Count) return null;
        return equippedWeapons[activeIndex]?.weaponBases;
    }

    public int GetLevel(WeaponDefinitionSO def)
    {
        return weaponLookup.TryGetValue(def, out WeaponEntry entry) ? entry.level : 0;
    }

    public bool EquipOffHand(OffHandDefinitionSO def)
    {
        if (def == null) return false;

        if (!offHandLookup.TryGetValue(def, out OffHandEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot equip off-hand, no entry for {def.offHandName}.");
            return false;
        }

        SetActiveOffHandEntry(entry);
        return true;
    }

    void SetActiveOffHandEntry(OffHandEntry entry)
    {
        if (entry?.offHandRoot == null) return;

        if (activeOffHand != null && activeOffHand.offHandRoot != null)
        {
            activeOffHand.Primary?.OnUnequip();
            activeOffHand.offHandRoot.SetActive(false);
        }

        activeOffHand = entry;
        entry.offHandRoot.SetActive(true);
        entry.Primary?.OnEquip();
    }

    public void UnequipOffHand()
    {
        if (activeOffHand == null) return;

        activeOffHand.Primary?.OnUnequip();

        if (activeOffHand.offHandRoot != null)
            activeOffHand.offHandRoot.SetActive(false);

        activeOffHand = null;
    }

    public bool HasOffHandEquipped => activeOffHand != null;

    public bool HasOffHand(OffHandDefinitionSO def)
    {
        return activeOffHand != null && activeOffHand.definition == def;
    }

    public OffHandBase GetActiveOffHandBase()
    {
        return activeOffHand?.Primary;
    }

    public OffHandEntry GetActiveOffHandEntry()
    {
        return activeOffHand;
    }
}