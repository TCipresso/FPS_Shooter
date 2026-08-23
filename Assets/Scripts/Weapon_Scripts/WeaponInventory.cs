using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : MonoBehaviour
{
    [Header("References")]
    public Transform weaponHolder;
    public PlayerStats playerStats;
    public FPSInput input;

    [Header("Input")]
    [Tooltip("Fires the active right-hand item. Typically left click / Attack.")]
    public InputActionReference rightFireAction;
    [Tooltip("Fires the active left-hand item. Typically right click.")]
    public InputActionReference leftFireAction;
    public InputActionReference leftSwapAction;
    public InputActionReference rightSwapAction;

    [Header("Catalog (every pre-placed left/right item on the player)")]
    public List<HandCatalogEntry> catalog = new List<HandCatalogEntry>();

    [Header("Loadouts (catalog indices, -1 = empty). Hard cap: 2 per hand.")]
    public HandLoadout leftHand = new HandLoadout();
    public HandLoadout rightHand = new HandLoadout();

    InputAction resolvedRightFire;
    InputAction resolvedLeftFire;
    InputAction resolvedLeftSwap;
    InputAction resolvedRightSwap;

    void Awake()
    {
        resolvedRightFire = ResolveAction(rightFireAction, "Attack");
        resolvedLeftFire = ResolveAction(leftFireAction, "LeftAttack");
        resolvedLeftSwap = ResolveAction(leftSwapAction, "LeftSwap");
        resolvedRightSwap = ResolveAction(rightSwapAction, "RightSwap");
        resolvedRightFire?.Enable();
        resolvedLeftFire?.Enable();
        resolvedLeftSwap?.Enable();
        resolvedRightSwap?.Enable();
    }

    void OnDisable()
    {
        resolvedRightFire?.Disable();
        resolvedLeftFire?.Disable();
        resolvedLeftSwap?.Disable();
        resolvedRightSwap?.Disable();
    }

    void Start()
    {
        DisableAllCatalogRoots();
        EnsureWeaponPools();
        ResolveStartingActiveSlot(leftHand, HandSide.Left);
        ResolveStartingActiveSlot(rightHand, HandSide.Right);
        EnableActive(leftHand, HandSide.Left);
        EnableActive(rightHand, HandSide.Right);
    }

    void Update()
    {
        if (WasPressed(resolvedLeftSwap))
            SwapHand(leftHand, HandSide.Left);
        if (WasPressed(resolvedRightSwap))
            SwapHand(rightHand, HandSide.Right);

        TickFire(resolvedRightFire, HandSide.Right);
        TickFire(resolvedLeftFire, HandSide.Left);
    }

    public void SwapLeft()
    {
        SwapHand(leftHand, HandSide.Left);
    }

    public void SwapRight()
    {
        SwapHand(rightHand, HandSide.Right);
    }

    public void SwapWeapon()
    {
        SwapRight();
    }

    void SwapHand(HandLoadout hand, HandSide side)
    {
        if (hand == null) return;
        int otherSlot = 1 - hand.activeSlot;
        if (!IsSlotUsable(hand, hand.activeSlot, side)) return;
        if (!IsSlotUsable(hand, otherSlot, side)) return;

        DisableActive(hand, side);
        hand.activeSlot = otherSlot;
        EnableActive(hand, side);
    }

    void TickFire(InputAction action, HandSide side)
    {
        if (action == null) return;
        WeaponBase weapon = GetActiveWeapon(side);
        if (weapon == null) return;

        bool shouldFire = weapon.isAutomatic
            ? action.IsPressed()
            : action.WasPressedThisFrame();

        if (shouldFire)
            weapon.Shoot();
        else if (action.WasReleasedThisFrame())
            weapon.StopRecoil();
    }

    void ResolveStartingActiveSlot(HandLoadout hand, HandSide side)
    {
        if (IsSlotUsable(hand, hand.activeSlot, side)) return;
        if (IsSlotUsable(hand, 0, side))
        {
            hand.activeSlot = 0;
            return;
        }
        if (IsSlotUsable(hand, 1, side))
        {
            hand.activeSlot = 1;
            return;
        }
        hand.activeSlot = 0;
    }

    bool IsSlotUsable(HandLoadout hand, int slotIndex, HandSide side)
    {
        if (hand == null) return false;
        return IsCatalogUsable(hand.GetSlot(slotIndex), side);
    }

    bool IsCatalogUsable(int catalogIndex, HandSide side)
    {
        HandCatalogEntry entry = GetCatalogEntry(catalogIndex);
        return entry != null && entry.HasSide(side);
    }

    public HandCatalogEntry GetCatalogEntry(int index)
    {
        if (index < 0 || index >= catalog.Count) return null;
        return catalog[index];
    }

    public int FindCatalogIndex(WeaponDefinitionSO def)
    {
        if (def == null) return -1;
        for (int i = 0; i < catalog.Count; i++)
        {
            if (catalog[i] != null && catalog[i].weaponDefinition == def)
                return i;
        }
        return -1;
    }

    public int FindCatalogIndex(OffHandDefinitionSO def)
    {
        if (def == null) return -1;
        for (int i = 0; i < catalog.Count; i++)
        {
            if (catalog[i] != null && catalog[i].offHandDefinition == def)
                return i;
        }
        return -1;
    }

    public bool TryAssignToHand(int catalogIndex, HandSide side)
    {
        if (!IsCatalogUsable(catalogIndex, side)) return false;
        HandLoadout hand = GetHand(side);
        if (HandContains(hand, catalogIndex)) return true;

        int empty = FirstEmptySlot(hand);
        if (empty < 0) return false;

        bool wasEmpty = !IsSlotUsable(hand, hand.activeSlot, side);
        hand.SetSlot(empty, catalogIndex);
        if (wasEmpty)
        {
            hand.activeSlot = empty;
            EnableActive(hand, side);
        }
        return true;
    }

    public int AddWeapon(WeaponDefinitionSO def)
    {
        int catalogIndex = FindCatalogIndex(def);
        if (catalogIndex < 0)
        {
            Debug.LogWarning($"[WeaponInventory] Cannot add weapon, no catalog entry for {def?.weaponName}.");
            return -1;
        }
        return AddCatalogIndex(catalogIndex);
    }

    public int AddWeaponByIndex(int catalogIndex)
    {
        if (catalogIndex < 0 || catalogIndex >= catalog.Count)
        {
            Debug.LogWarning($"[WeaponInventory] AddWeaponByIndex: index {catalogIndex} out of range.");
            return -1;
        }
        return AddCatalogIndex(catalogIndex);
    }

    int AddCatalogIndex(int catalogIndex)
    {
        HandCatalogEntry entry = GetCatalogEntry(catalogIndex);
        if (entry == null) return -1;

        if (entry.HasSide(HandSide.Right) && TryAssignToHand(catalogIndex, HandSide.Right))
            return catalogIndex;
        if (entry.HasSide(HandSide.Left) && TryAssignToHand(catalogIndex, HandSide.Left))
            return catalogIndex;

        Debug.LogWarning("[WeaponInventory] No empty compatible hand slot.");
        return -1;
    }

    public void RemoveWeapon(WeaponDefinitionSO def)
    {
        int catalogIndex = FindCatalogIndex(def);
        if (catalogIndex < 0) return;
        ClearCatalogFromHands(catalogIndex);
    }

    public void RemoveWeaponAt(int catalogIndex)
    {
        ClearCatalogFromHands(catalogIndex);
    }

    public bool EquipOffHand(OffHandDefinitionSO def)
    {
        int catalogIndex = FindCatalogIndex(def);
        if (catalogIndex < 0)
        {
            Debug.LogWarning($"[WeaponInventory] Cannot equip off-hand, no catalog entry for {def?.offHandName}.");
            return false;
        }
        return AddCatalogIndex(catalogIndex) >= 0;
    }

    public void UnequipOffHand()
    {
        UnequipOffHand(GetActiveOffHandBase());
    }

    public void UnequipOffHand(OffHandBase instance)
    {
        if (instance == null) return;
        TryUnequipInstance(leftHand, HandSide.Left, instance);
        TryUnequipInstance(rightHand, HandSide.Right, instance);
    }

    void TryUnequipInstance(HandLoadout hand, HandSide side, OffHandBase instance)
    {
        for (int i = 0; i < HandLoadout.SlotCount; i++)
        {
            HandCatalogEntry entry = GetCatalogEntry(hand.GetSlot(i));
            if (entry == null || entry.GetOffHand(side) != instance) continue;
            DisableSlot(hand.GetSlot(i), side);
            hand.SetSlot(i, -1);
            if (hand.activeSlot == i)
                EquipFirstUsable(hand, side);
            return;
        }
    }

    void ClearCatalogFromHands(int catalogIndex)
    {
        ClearCatalogFromHand(leftHand, HandSide.Left, catalogIndex);
        ClearCatalogFromHand(rightHand, HandSide.Right, catalogIndex);
    }

    void ClearCatalogFromHand(HandLoadout hand, HandSide side, int catalogIndex)
    {
        bool clearedActive = false;
        for (int i = 0; i < HandLoadout.SlotCount; i++)
        {
            if (hand.GetSlot(i) != catalogIndex) continue;
            if (hand.activeSlot == i)
                DisableSlot(catalogIndex, side);
            hand.SetSlot(i, -1);
            if (hand.activeSlot == i)
                clearedActive = true;
        }
        if (clearedActive)
            EquipFirstUsable(hand, side);
    }

    void EquipFirstUsable(HandLoadout hand, HandSide side)
    {
        for (int i = 0; i < HandLoadout.SlotCount; i++)
        {
            if (!IsSlotUsable(hand, i, side)) continue;
            hand.activeSlot = i;
            EnableActive(hand, side);
            return;
        }
    }

    void DisableAllCatalogRoots()
    {
        for (int i = 0; i < catalog.Count; i++)
        {
            HandCatalogEntry entry = catalog[i];
            if (entry == null) continue;
            if (entry.leftRoot != null)
                entry.leftRoot.SetActive(false);
            if (entry.rightRoot != null)
                entry.rightRoot.SetActive(false);
        }
    }

    void EnsureWeaponPools()
    {
        for (int i = 0; i < catalog.Count; i++)
        {
            HandCatalogEntry entry = catalog[i];
            if (entry == null) continue;
            EnsureWeaponPool(entry.leftWeapon);
            EnsureWeaponPool(entry.rightWeapon);
        }
    }

    void EnsureWeaponPool(WeaponBase wb)
    {
        if (wb == null || wb.weaponDefinition == null) return;
        WeaponDefinitionSO def = wb.weaponDefinition;
        if (BulletPool.Instance != null && def.trailPrefab != null)
            BulletPool.Instance.EnsurePoolSize(def.trailPoolKey, def.trailPrefab.gameObject, def.trailPoolSize);
        if (ProjectilePool.Instance != null && def.bulletType == BulletType.Projectile && def.projectilePrefab != null)
            ProjectilePool.Instance.EnsurePoolSize(def.projectilePrefab, 8);
    }

    void EnableActive(HandLoadout hand, HandSide side)
    {
        if (!IsSlotUsable(hand, hand.activeSlot, side)) return;
        EnableSlot(hand.GetSlot(hand.activeSlot), side);
    }

    void DisableActive(HandLoadout hand, HandSide side)
    {
        if (!IsSlotUsable(hand, hand.activeSlot, side)) return;
        DisableSlot(hand.GetSlot(hand.activeSlot), side);
    }

    void EnableSlot(int catalogIndex, HandSide side)
    {
        HandCatalogEntry entry = GetCatalogEntry(catalogIndex);
        if (entry == null || !entry.HasSide(side)) return;

        if (entry.GetRoot(side) != null)
            entry.GetRoot(side).SetActive(true);

        WeaponBase weapon = entry.GetWeapon(side);
        if (weapon != null)
        {
            ApplyLevel(weapon, entry.weaponDefinition);
            weapon.LoadRecoilValues();
        }

        entry.GetOffHand(side)?.OnEquip();
    }

    void DisableSlot(int catalogIndex, HandSide side)
    {
        HandCatalogEntry entry = GetCatalogEntry(catalogIndex);
        if (entry == null) return;
        entry.GetOffHand(side)?.OnUnequip();
        if (entry.GetRoot(side) != null)
            entry.GetRoot(side).SetActive(false);
    }

    void ApplyLevel(WeaponBase weapon, WeaponDefinitionSO def)
    {
        if (weapon == null || def == null) return;
        weapon.ApplyLevel(def);
    }

    public void LevelUpWeapon(WeaponDefinitionSO def)
    {
        int catalogIndex = FindCatalogIndex(def);
        HandCatalogEntry entry = GetCatalogEntry(catalogIndex);
        if (entry == null)
        {
            Debug.LogWarning($"[WeaponInventory] Cannot level up, no catalog entry for {def?.weaponName}.");
            return;
        }

        WeaponDefinitionSO liveDef = entry.rightWeapon != null && entry.rightWeapon.weaponDefinition != null
            ? entry.rightWeapon.weaponDefinition
            : entry.leftWeapon != null ? entry.leftWeapon.weaponDefinition : null;
        if (liveDef == null) return;

        liveDef.level = Mathf.Min(liveDef.level + 1, liveDef.maxLevel);
        if (entry.leftWeapon != null && entry.leftWeapon.weaponDefinition != null)
            entry.leftWeapon.weaponDefinition.level = liveDef.level;
        if (entry.rightWeapon != null && entry.rightWeapon.weaponDefinition != null)
            entry.rightWeapon.weaponDefinition.level = liveDef.level;

        entry.leftWeapon?.RefreshWeaponSkin();
        entry.rightWeapon?.RefreshWeaponSkin();
    }

    public bool HasWeapon(WeaponDefinitionSO def)
    {
        int catalogIndex = FindCatalogIndex(def);
        if (catalogIndex < 0) return false;
        return HandContains(leftHand, catalogIndex) || HandContains(rightHand, catalogIndex);
    }

    public bool HasOffHandEquipped => GetActiveOffHandBase() != null;

    public bool HasOffHand(OffHandDefinitionSO def)
    {
        int catalogIndex = FindCatalogIndex(def);
        if (catalogIndex < 0) return false;
        return IsActiveCatalog(leftHand, HandSide.Left, catalogIndex)
            || IsActiveCatalog(rightHand, HandSide.Right, catalogIndex);
    }

    public WeaponBase GetActiveWeapon(HandSide side)
    {
        HandLoadout hand = GetHand(side);
        HandCatalogEntry entry = GetCatalogEntry(hand.GetSlot(hand.activeSlot));
        return entry != null ? entry.GetWeapon(side) : null;
    }

    public OffHandBase GetActiveOffHand(HandSide side)
    {
        HandLoadout hand = GetHand(side);
        HandCatalogEntry entry = GetCatalogEntry(hand.GetSlot(hand.activeSlot));
        return entry != null ? entry.GetOffHand(side) : null;
    }

    public WeaponBase GetActiveWeaponBase()
    {
        return GetActiveWeapon(HandSide.Right);
    }

    public List<WeaponBase> GetActiveWeaponBases()
    {
        List<WeaponBase> bases = new List<WeaponBase>(2);
        WeaponBase right = GetActiveWeapon(HandSide.Right);
        WeaponBase left = GetActiveWeapon(HandSide.Left);
        if (right != null) bases.Add(right);
        if (left != null) bases.Add(left);
        return bases;
    }

    public OffHandBase GetActiveOffHandBase()
    {
        OffHandBase left = GetActiveOffHand(HandSide.Left);
        if (left != null) return left;
        return GetActiveOffHand(HandSide.Right);
    }

    public int GetLevel(WeaponDefinitionSO def)
    {
        int catalogIndex = FindCatalogIndex(def);
        HandCatalogEntry entry = GetCatalogEntry(catalogIndex);
        if (entry == null) return 0;
        WeaponBase weapon = entry.rightWeapon != null ? entry.rightWeapon : entry.leftWeapon;
        return weapon != null && weapon.weaponDefinition != null ? weapon.weaponDefinition.level : 0;
    }

    public HandLoadout GetHand(HandSide side)
    {
        return side == HandSide.Left ? leftHand : rightHand;
    }

    bool HandContains(HandLoadout hand, int catalogIndex)
    {
        if (hand == null || catalogIndex < 0) return false;
        return hand.slot0 == catalogIndex || hand.slot1 == catalogIndex;
    }

    bool IsActiveCatalog(HandLoadout hand, HandSide side, int catalogIndex)
    {
        return IsSlotUsable(hand, hand.activeSlot, side) && hand.GetSlot(hand.activeSlot) == catalogIndex;
    }

    int FirstEmptySlot(HandLoadout hand)
    {
        if (hand.slot0 < 0) return 0;
        if (hand.slot1 < 0) return 1;
        return -1;
    }

    InputAction ResolveAction(InputActionReference pref, string fallbackName)
    {
        if (pref != null && pref.action != null)
            return pref.action;

        InputActionAsset asset = null;
        if (rightFireAction != null) asset = rightFireAction.asset;
        else if (leftFireAction != null) asset = leftFireAction.asset;
        else if (leftSwapAction != null) asset = leftSwapAction.asset;
        else if (rightSwapAction != null) asset = rightSwapAction.asset;
        if (asset == null) return null;

        return asset.FindAction(fallbackName, false);
    }

    static bool WasPressed(InputAction action)
    {
        return action != null && action.WasPressedThisFrame();
    }
}
