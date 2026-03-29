using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : MonoBehaviour
{
    [Header("References")]
    public Transform weaponHolder;


    [Header("Inventory Settings")]
    public int maxSlots = 2;

    [Header("Input")]
    public InputActionReference fireAction;
    public InputActionReference reloadAction;
    public InputActionReference scrollAction;
    public InputActionReference slot1Action;
    public InputActionReference slot2Action;

    private List<GameObject> equippedWeapons = new List<GameObject>();
    private List<WeaponData> equippedData = new List<WeaponData>();
    private int activeSlot = 0;

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
            bool shouldFire = active != null && active.isAutomatic
                ? fireAction.action.IsPressed()
                : fireAction.action.WasPressedThisFrame();

            if (shouldFire) FireActiveWeapon();
        }

        if (reloadAction != null && reloadAction.action.WasPressedThisFrame())
            ReloadActiveWeapon();

        // Scroll to swap
        if (scrollAction != null)
        {
            float scroll = scrollAction.action.ReadValue<float>();
            if (scroll > 0f) CycleSlot(-1);
            else if (scroll < 0f) CycleSlot(1);
        }

        // Number keys
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
            WeaponBase wb = w.GetComponent<WeaponBase>();
            if (wb != null) wb.Refill();
        }

        Debug.Log("[WeaponInventory] Max ammo applied to all weapons.");
    }

    public void PartialAmmoRefill(float percent)
    {
        foreach (GameObject w in equippedWeapons)
        {
            WeaponBase wb = w.GetComponent<WeaponBase>();
            if (wb == null) continue;

            int amount = Mathf.RoundToInt(wb.maxReserve * percent);
            wb.reserveAmmo = Mathf.Min(wb.reserveAmmo + amount, wb.maxReserve);
        }

        Debug.Log($"[WeaponInventory] Partial refill {percent * 100}% applied to all weapons.");
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

        int newSlot = equippedWeapons.Count - 1;
        SetActiveSlot(newSlot);

        Debug.Log($"[WeaponInventory] Added {data.weaponName} to slot {newSlot}.");
    }

    void SwapWeapon(WeaponData data, int slot)
    {
        Destroy(equippedWeapons[slot]);
        equippedWeapons.RemoveAt(slot);
        equippedData.RemoveAt(slot);

        GameObject instance = InstantiateWeapon(data);
        equippedWeapons.Insert(slot, instance);
        equippedData.Insert(slot, data);

        SetActiveSlot(slot);

        Debug.Log($"[WeaponInventory] Swapped slot {slot} to {data.weaponName}.");
    }

    GameObject InstantiateWeapon(WeaponData data)
    {
        GameObject instance = Instantiate(data.prefab, weaponHolder);
        instance.transform.localPosition = data.positionOffset;
        instance.transform.localRotation = Quaternion.Euler(data.rotationOffset);
        instance.SetActive(false);

        // Tell the pool how many trails this weapon needs
        WeaponBase wb = instance.GetComponent<WeaponBase>();
        if (wb != null && BulletPool.Instance != null && wb.trailPrefab != null)
            BulletPool.Instance.EnsurePoolSize(wb.trailPoolKey, wb.trailPrefab.gameObject, wb.trailPoolSize);

        return instance;
    }

    public void SetActiveSlot(int slot)
    {
        if (slot < 0 || slot >= equippedWeapons.Count) return;

        for (int i = 0; i < equippedWeapons.Count; i++)
            equippedWeapons[i].SetActive(false);

        equippedWeapons[slot].SetActive(true);
        activeSlot = slot;

        // Push this weapon's recoil values to WeaponRecoil
        WeaponBase wb = equippedWeapons[slot].GetComponent<WeaponBase>();
        if (wb != null) wb.LoadRecoilValues();

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
        return equippedWeapons[activeSlot].GetComponent<WeaponBase>();
    }

    public WeaponData GetActiveWeaponData()
    {
        if (equippedData.Count == 0) return null;
        return equippedData[activeSlot];
    }
}