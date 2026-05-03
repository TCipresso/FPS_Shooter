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
    public InputActionReference reloadAction;
    public InputActionReference scrollAction;
    public InputActionReference slot1Action;
    public InputActionReference slot2Action;

    public List<GameObject> equippedWeapons = new List<GameObject>();
    public List<WeaponData> equippedData = new List<WeaponData>();
    public List<WeaponUpgradeData> equippedUpgradeData = new List<WeaponUpgradeData>();
    public List<int> weaponLevels = new List<int>();

    private int activeSlot = 0;
    private bool fireHeldLastFrame = false;

    void OnEnable()
    {
        if (fireAction != null) fireAction.action.Enable();
        if (reloadAction != null) reloadAction.action.Enable();
        if (scrollAction != null) scrollAction.action.Enable();
        if (slot1Action != null) slot1Action.action.Enable();
        if (slot2Action != null) slot2Action.action.Enable();
    }

    void OnDisable()
    {
        if (fireAction != null) fireAction.action.Disable();
        if (reloadAction != null) reloadAction.action.Disable();
        if (scrollAction != null) scrollAction.action.Disable();
        if (slot1Action != null) slot1Action.action.Disable();
        if (slot2Action != null) slot2Action.action.Disable();
    }

    void Update()
    {
        if (fireAction != null)
        {
            WeaponBase active = GetActiveWeaponBase();

            bool shouldFire = false;
            if (active != null)
            {
                if (active.isReloading)
                {
                    // Force a fresh press after reload completes
                    fireHeldLastFrame = true;
                    shouldFire = false;
                }
                else if (active.isAutomatic)
                {
                    bool isPressed = fireAction.action.IsPressed();
                    // Require releasing and repressing after reload
                    if (fireHeldLastFrame && isPressed)
                        shouldFire = false;
                    else
                    {
                        shouldFire = isPressed;
                        fireHeldLastFrame = false;
                    }
                }
                else
                {
                    shouldFire = fireAction.action.WasPressedThisFrame();
                    fireHeldLastFrame = false;
                }
            }

            if (shouldFire)
                FireActiveWeapon();
            else
                active?.fpsLook?.StopRecoil();
        }

        if (reloadAction != null && reloadAction.action.WasPressedThisFrame())
            ReloadActiveWeapon();

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

    void ReloadActiveWeapon()
    {
        WeaponBase weapon = GetActiveWeaponBase();
        if (weapon == null) return;
        weapon.Reload();
    }

    public void MaxAmmo()
    {
        foreach (GameObject w in equippedWeapons)
        {
            WeaponBase wb = w.GetComponentInChildren<WeaponBase>();
            if (wb != null) wb.Refill();
        }
        Debug.Log("[WeaponInventory] Max ammo applied to all weapons.");
    }

    public void PartialAmmoRefill(float percent)
    {
        foreach (GameObject w in equippedWeapons)
        {
            WeaponBase wb = w.GetComponentInChildren<WeaponBase>();
            if (wb == null) continue;
            int amount = Mathf.RoundToInt(wb.maxReserve * percent);
            wb.reserveAmmo = Mathf.Min(wb.reserveAmmo + amount, wb.maxReserve);
        }
        Debug.Log($"[WeaponInventory] Partial refill {percent * 100}% applied to all weapons.");
    }

    public bool TryAddWeapon(WeaponData data, WeaponUpgradeData upgradeData = null)
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
            AddWeaponToSlot(data, upgradeData);
            return true;
        }

        Debug.Log($"[WeaponInventory] Inventory full. Swapping slot {activeSlot} with {data.weaponName}.");
        SwapWeapon(data, activeSlot, upgradeData);
        return true;
    }

    void AddWeaponToSlot(WeaponData data, WeaponUpgradeData upgradeData = null)
    {
        GameObject instance = InstantiateWeapon(data);
        equippedWeapons.Add(instance);
        equippedData.Add(data);
        equippedUpgradeData.Add(upgradeData);
        weaponLevels.Add(1);

        int newSlot = equippedWeapons.Count - 1;
        SetActiveSlot(newSlot);

        Debug.Log($"[WeaponInventory] Added {data.weaponName} to slot {newSlot}.");
    }

    void SwapWeapon(WeaponData data, int slot, WeaponUpgradeData upgradeData = null)
    {
        Destroy(equippedWeapons[slot]);
        equippedWeapons.RemoveAt(slot);
        equippedData.RemoveAt(slot);
        equippedUpgradeData.RemoveAt(slot);
        weaponLevels.RemoveAt(slot);

        GameObject instance = InstantiateWeapon(data);
        equippedWeapons.Insert(slot, instance);
        equippedData.Insert(slot, data);
        equippedUpgradeData.Insert(slot, upgradeData);
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
        if (wb != null && BulletPool.Instance != null && wb.trailPrefab != null)
            BulletPool.Instance.EnsurePoolSize(wb.trailPoolKey, wb.trailPrefab.gameObject, wb.trailPoolSize);

        if (wb != null && playerStats != null)
        {
            wb.ApplyExtraMagazine(playerStats.extraMagazine);
            wb.currentMag = wb.maxMag;
        }

        return instance;
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
                wb.ApplyExtraMagazine(playerStats.extraMagazine);
                wb.critChance = playerStats.critChance;
                wb.critMultiplier = playerStats.critMultiplier;
            }
        }

        if (ikHandler != null)
            ikHandler.UpdateIKTargets(equippedWeapons[slot]);

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

    public WeaponUpgradeData GetWeaponUpgradeData(int slot)
    {
        if (slot < 0 || slot >= equippedUpgradeData.Count) return null;
        return equippedUpgradeData[slot];
    }
}