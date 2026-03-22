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

    private List<GameObject> equippedWeapons = new List<GameObject>();
    private List<WeaponData> equippedData = new List<WeaponData>();
    private int activeSlot = 0;

    void OnEnable()
    {
        if (fireAction != null) fireAction.action.Enable();
        if (reloadAction != null) reloadAction.action.Enable();
    }

    void OnDisable()
    {
        if (fireAction != null) fireAction.action.Disable();
        if (reloadAction != null) reloadAction.action.Disable();
    }

    void Update()
    {
        if (fireAction != null && fireAction.action.WasPressedThisFrame())
            FireActiveWeapon();

        if (reloadAction != null && reloadAction.action.WasPressedThisFrame())
            ReloadActiveWeapon();
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
        return instance;
    }

    public void SetActiveSlot(int slot)
    {
        if (slot < 0 || slot >= equippedWeapons.Count) return;

        for (int i = 0; i < equippedWeapons.Count; i++)
            equippedWeapons[i].SetActive(false);

        equippedWeapons[slot].SetActive(true);
        activeSlot = slot;

        Debug.Log($"[WeaponInventory] Equipped slot {slot}: {equippedData[slot].weaponName}.");
    }

    public void SwitchToSlot(int slot) => SetActiveSlot(slot);

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