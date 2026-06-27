using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : MonoBehaviour
{
    [Header("References")]
    public Transform weaponHolder;
    public PlayerStats playerStats;

    [Header("Inventory Settings")]
    public int maxSlots = 2;

    [Header("IK")]
    public IKWeaponHandler ikHandler;

    [Header("Input")]
    public InputActionReference fireAction;
    public InputActionReference scrollAction;
    public InputActionReference slot1Action;
    public InputActionReference slot2Action;

    [Header("Default Loadout")]
    public List<WeaponDefinitionSO> defaultWeapons = new List<WeaponDefinitionSO>();

    public List<GameObject> equippedWeapons = new List<GameObject>();
    public List<WeaponData> equippedData = new List<WeaponData>();
    public List<int> weaponLevels = new List<int>();

    private int activeSlot = 0;

    void Start()
    {
        foreach (WeaponDefinitionSO def in defaultWeapons)
        {
            if (def != null)
                TryAddWeaponInstance(new WeaponInstance(def, WeaponRarity.Common));
        }
    }

    void OnEnable()
    {
        if (fireAction != null) fireAction.action.Enable();
        if (scrollAction != null) scrollAction.action.Enable();
        if (slot1Action != null) slot1Action.action.Enable();
        if (slot2Action != null) slot2Action.action.Enable();
    }

    void OnDisable()
    {
        if (fireAction != null) fireAction.action.Disable();
        if (scrollAction != null) scrollAction.action.Disable();
        if (slot1Action != null) slot1Action.action.Disable();
        if (slot2Action != null) slot2Action.action.Disable();
    }

    void Update()
    {
        if (fireAction != null)
        {
            WeaponBase active = GetActiveWeaponBase();
            bool shouldFire = active != null && active.isAutomatic
                ? fireAction.action.IsPressed()
                : fireAction.action.WasPressedThisFrame();

            if (shouldFire)
                FireActiveWeapon();
            else if (fireAction.action.WasReleasedThisFrame())
                GetActiveWeaponBase()?.StopRecoil();
        }

        if (scrollAction != null)
        {
            float scroll = scrollAction.action.ReadValue<float>();
            if (scroll > 0f) CycleSlot(-1);
            else if (scroll < 0f) CycleSlot(1);
        }

        if (slot1Action != null && slot1Action.action.WasPressedThisFrame()) SetActiveSlot(0);
        if (slot2Action != null && slot2Action.action.WasPressedThisFrame()) SetActiveSlot(1);
    }

    void FireActiveWeapon()
    {
        WeaponBase weapon = GetActiveWeaponBase();
        if (weapon == null) return;
        weapon.Shoot();
    }

    public bool TryAddWeapon(WeaponData data)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogWarning("[WeaponInventory] WeaponData or prefab is null.");
            return false;
        }

        if (equippedData.Contains(data))
        {
            Debug.Log($"[WeaponInventory] Already carrying {data.weaponName}.");
            return false;
        }

        if (equippedWeapons.Count < maxSlots)
        {
            AddWeaponToSlot(data);
            return true;
        }

        Debug.Log($"[WeaponInventory] Inventory full. Swapping slot {activeSlot} with {data.weaponName}.");
        SwapWeapon(data, activeSlot);
        return true;
    }

    void AddWeaponToSlot(WeaponData data)
    {
        GameObject instance = InstantiateWeapon(data);
        equippedWeapons.Add(instance);
        equippedData.Add(data);
        weaponLevels.Add(1);

        int newSlot = equippedWeapons.Count - 1;
        SetActiveSlot(newSlot);

        Debug.Log($"[WeaponInventory] Added {data.weaponName} to slot {newSlot}.");
    }

    void SwapWeapon(WeaponData data, int slot)
    {
        Destroy(equippedWeapons[slot]);
        equippedWeapons.RemoveAt(slot);
        equippedData.RemoveAt(slot);
        weaponLevels.RemoveAt(slot);

        GameObject instance = InstantiateWeapon(data);
        equippedWeapons.Insert(slot, instance);
        equippedData.Insert(slot, data);
        weaponLevels.Insert(slot, 1);

        SetActiveSlot(slot);

        Debug.Log($"[WeaponInventory] Swapped slot {slot} to {data.weaponName}.");
    }

    public void UpgradeWeaponInSlot(int slot, WeaponData newWeaponData)
    {
        if (slot < 0 || slot >= equippedWeapons.Count) return;
        if (weaponLevels[slot] >= 10)
        {
            Debug.LogWarning($"[WeaponInventory] Slot {slot} already at max level.");
            return;
        }

        weaponLevels[slot]++;
        int newLevel = weaponLevels[slot];

        Destroy(equippedWeapons[slot]);

        GameObject instance = InstantiateWeapon(newWeaponData);
        equippedWeapons[slot] = instance;
        equippedData[slot] = newWeaponData;

        SetActiveSlot(slot);

        Debug.Log($"[WeaponInventory] Upgraded slot {slot} to level {newLevel}.");
    }

    GameObject InstantiateWeapon(WeaponData data)
    {
        GameObject instance = Instantiate(data.prefab, weaponHolder);
        instance.transform.localPosition = data.positionOffset;
        instance.transform.localRotation = Quaternion.Euler(data.rotationOffset);
        instance.SetActive(false);

        WeaponBase wb = instance.GetComponentInChildren<WeaponBase>();
        if (wb != null && BulletPool.Instance != null && wb.bulletData != null && wb.bulletData.trailPrefab != null)
            BulletPool.Instance.EnsurePoolSize(wb.bulletData.trailPoolKey, wb.bulletData.trailPrefab.gameObject, wb.bulletData.trailPoolSize);

        if (wb != null && playerStats != null)
            wb.ApplyAttackSpeed(playerStats.attackSpeed);

        return instance;
    }

    public void TryAddWeaponInstance(WeaponInstance instance)
    {
        if (instance == null || instance.definition == null || instance.definition.prefab == null)
        {
            Debug.LogWarning("[WeaponInventory] Invalid WeaponInstance.");
            return;
        }

        if (equippedWeapons.Count < maxSlots)
            AddWeaponInstanceToSlot(instance);
        else
            SwapWeaponInstance(instance, activeSlot);
    }

    void AddWeaponInstanceToSlot(WeaponInstance instance)
    {
        GameObject go = InstantiateWeaponInstance(instance);
        equippedWeapons.Add(go);
        equippedData.Add(null);
        weaponLevels.Add(1);

        int newSlot = equippedWeapons.Count - 1;
        SetActiveSlot(newSlot);

        Debug.Log($"[WeaponInventory] Picked up {instance.definition.weaponName} ({instance.rarity})");
    }

    void SwapWeaponInstance(WeaponInstance instance, int slot)
    {
        Destroy(equippedWeapons[slot]);
        equippedWeapons.RemoveAt(slot);
        equippedData.RemoveAt(slot);
        weaponLevels.RemoveAt(slot);

        GameObject go = InstantiateWeaponInstance(instance);
        equippedWeapons.Insert(slot, go);
        equippedData.Insert(slot, null);
        weaponLevels.Insert(slot, 1);

        SetActiveSlot(slot);

        Debug.Log($"[WeaponInventory] Swapped slot {slot} to {instance.definition.weaponName} ({instance.rarity})");
    }

    GameObject InstantiateWeaponInstance(WeaponInstance instance)
    {
        WeaponDefinitionSO def = instance.definition;
        GameObject go = Instantiate(def.prefab, weaponHolder);
        go.transform.localPosition = def.positionOffset;
        go.transform.localRotation = Quaternion.Euler(def.rotationOffset);
        go.SetActive(false);

        WeaponBase wb = go.GetComponentInChildren<WeaponBase>();
        if (wb != null)
        {
            wb.Equip(instance);

            if (BulletPool.Instance != null && def.bulletData != null && def.bulletData.trailPrefab != null)
                BulletPool.Instance.EnsurePoolSize(def.bulletData.trailPoolKey, def.bulletData.trailPrefab.gameObject, def.bulletData.trailPoolSize);

            if (playerStats != null)
                wb.ApplyAttackSpeed(playerStats.attackSpeed);
        }

        return go;
    }

    public void SetActiveSlot(int slot)
    {
        if (slot < 0 || slot >= equippedWeapons.Count) return;

        for (int i = 0; i < equippedWeapons.Count; i++)
            equippedWeapons[i].SetActive(false);

        equippedWeapons[slot].SetActive(true);
        activeSlot = slot;

        WeaponBase wb = equippedWeapons[slot].GetComponentInChildren<WeaponBase>();
        if (wb != null)
        {
            wb.LoadRecoilValues();
            if (playerStats != null)
            {
                wb.ApplyAttackSpeed(playerStats.attackSpeed);
                wb.critChance = playerStats.critChance;
                wb.critMultiplier = playerStats.critMultiplier;
            }
        }

        if (ikHandler != null)
            ikHandler.UpdateIKTargets(equippedWeapons[slot]);

        if (equippedData[slot] != null)
            Debug.Log($"[WeaponInventory] Equipped slot {slot}: {equippedData[slot].weaponName}.");
    }

    public void SwitchToSlot(int slot) => SetActiveSlot(slot);

    void CycleSlot(int direction)
    {
        if (equippedWeapons.Count <= 1) return;
        int newSlot = (activeSlot + direction + equippedWeapons.Count) % equippedWeapons.Count;
        SetActiveSlot(newSlot);
    }

    public WeaponBase GetActiveWeaponBase()
    {
        if (equippedWeapons.Count == 0) return null;
        return equippedWeapons[activeSlot].GetComponentInChildren<WeaponBase>();
    }

    public WeaponData GetActiveWeaponData()
    {
        if (equippedData.Count == 0) return null;
        return equippedData[activeSlot];
    }

    public int GetWeaponLevel(int slot)
    {
        if (slot < 0 || slot >= weaponLevels.Count) return 0;
        return weaponLevels[slot];
    }

    public WeaponUpgradeData GetWeaponUpgradeData(int slot) => null;
}