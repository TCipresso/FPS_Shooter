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
    public InputActionReference nextWeaponAction;
    public InputActionReference previousWeaponAction;
    [Header("Starting Loadout")]
    public List<WeaponDefinitionSO> startingWeapons = new List<WeaponDefinitionSO>();
    public OffHandDefinitionSO startingOffHand;
    [Header("Weapons (drag pre-placed, disabled weapon children here)")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();
    [Header("Off-Hand (drag pre-placed, disabled off-hand children here)")]
    public List<OffHandEntry> offHandItems = new List<OffHandEntry>();
    private readonly List<WeaponEntry> equippedWeapons = new List<WeaponEntry>();
    private Dictionary<WeaponDefinitionSO, WeaponEntry> weaponLookup = new Dictionary<WeaponDefinitionSO, WeaponEntry>();
    private int activeIndex = -1;
    private OffHandEntry activeOffHand;
    private Dictionary<OffHandDefinitionSO, OffHandEntry> offHandLookup = new Dictionary<OffHandDefinitionSO, OffHandEntry>();
    void Awake()
    {
        weaponLookup.Clear();
        foreach (WeaponEntry entry in weapons)
        {
            if (entry == null || entry.weaponRoot == null || entry.definition == null
                || entry.weaponBases == null || entry.weaponBases.Count == 0)
            {
                Debug.LogWarning("[WeaponInventory] Skipping invalid WeaponEntry.");
                continue;
            }
            weaponLookup[entry.definition] = entry;
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
        if (nextWeaponAction != null) nextWeaponAction.action.Enable();
        if (previousWeaponAction != null) previousWeaponAction.action.Enable();
    }
    void OnDisable()
    {
        if (fireAction != null) fireAction.action.Disable();
        if (nextWeaponAction != null) nextWeaponAction.action.Disable();
        if (previousWeaponAction != null) previousWeaponAction.action.Disable();
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
                if (BulletPool.Instance != null && wb.weaponDefinition != null && wb.weaponDefinition.trailPrefab != null)
                    BulletPool.Instance.EnsurePoolSize(wb.weaponDefinition.trailPoolKey, wb.weaponDefinition.trailPrefab.gameObject, wb.weaponDefinition.trailPoolSize);
                if (ProjectilePool.Instance != null && wb.weaponDefinition != null
                    && wb.weaponDefinition.bulletType == BulletType.Projectile && wb.weaponDefinition.projectilePrefab != null)
                    ProjectilePool.Instance.EnsurePoolSize(wb.weaponDefinition.projectilePrefab, 8);
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
        if (nextWeaponAction != null && nextWeaponAction.action.WasPressedThisFrame())
            SwapNext();
        if (previousWeaponAction != null && previousWeaponAction.action.WasPressedThisFrame())
            SwapPrevious();
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
        SwapNext();
    }
    public void SwapNext()
    {
        if (equippedWeapons.Count <= 1) return;
        int next = (activeIndex + 1) % equippedWeapons.Count;
        EquipIndex(next);
    }
    public void SwapPrevious()
    {
        if (equippedWeapons.Count <= 1) return;
        int previous = (activeIndex - 1 + equippedWeapons.Count) % equippedWeapons.Count;
        EquipIndex(previous);
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
        WeaponDefinitionSO liveDef = entry.Primary != null ? entry.Primary.weaponDefinition : null;
        if (liveDef == null) return;
        liveDef.level = Mathf.Min(liveDef.level + 1, liveDef.maxLevel);
        foreach (WeaponBase wb in entry.weaponBases)
            wb?.RefreshWeaponSkin();
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
            wb.ApplyLevel(entry.definition);
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
        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry)) return 0;
        return entry.Primary != null && entry.Primary.weaponDefinition != null ? entry.Primary.weaponDefinition.level : 0;
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