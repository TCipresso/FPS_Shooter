using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    [Header("References")]
    public Transform weaponHolder;

    [Header("Inventory Settings")]
    public int maxSlots = 2;

    // Currently equipped weapon instances
    private List<GameObject> equippedWeapons = new List<GameObject>();
    private List<WeaponData> equippedData = new List<WeaponData>();
    private int activeSlot = 0;

    public bool TryAddWeapon(WeaponData data)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogWarning("[WeaponInventory] WeaponData or prefab is null.");
            return false;
        }

        // Check if already carrying this weapon
        if (equippedData.Contains(data))
        {
            Debug.Log($"[WeaponInventory] Already carrying {data.weaponName}.");
            return false;
        }

        // Open slot available
        if (equippedWeapons.Count < maxSlots)
        {
            AddWeaponToSlot(data);
            return true;
        }

        // Full — swap with currently active slot
        Debug.Log($"[WeaponInventory] Inventory full. Swapping slot {activeSlot} with {data.weaponName}.");
        SwapWeapon(data, activeSlot);
        return true;
    }

    void AddWeaponToSlot(WeaponData data)
    {
        GameObject instance = InstantiateWeapon(data);
        equippedWeapons.Add(instance);
        equippedData.Add(data);

        // Equip the new slot, deactivate others
        int newSlot = equippedWeapons.Count - 1;
        SetActiveSlot(newSlot);

        Debug.Log($"[WeaponInventory] Added {data.weaponName} to slot {newSlot}.");
    }

    void SwapWeapon(WeaponData data, int slot)
    {
        // Destroy old weapon in slot
        Destroy(equippedWeapons[slot]);
        equippedWeapons.RemoveAt(slot);
        equippedData.RemoveAt(slot);

        // Instantiate new one
        GameObject instance = InstantiateWeapon(data);
        equippedWeapons.Insert(slot, instance);
        equippedData.Insert(slot, data);

        SetActiveSlot(slot);

        Debug.Log($"[WeaponInventory] Swapped slot {slot} to {data.weaponName}.");
    }

    GameObject InstantiateWeapon(WeaponData data)
    {
        GameObject instance = Instantiate(data.prefab, weaponHolder);

        // Apply offset from WeaponData
        instance.transform.localPosition = data.positionOffset;
        instance.transform.localRotation = Quaternion.Euler(data.rotationOffset);

        instance.SetActive(false);
        return instance;
    }

    public void SetActiveSlot(int slot)
    {
        if (slot < 0 || slot >= equippedWeapons.Count) return;

        // Deactivate all
        for (int i = 0; i < equippedWeapons.Count; i++)
            equippedWeapons[i].SetActive(false);

        // Activate selected
        equippedWeapons[slot].SetActive(true);
        activeSlot = slot;

        Debug.Log($"[WeaponInventory] Equipped slot {slot}: {equippedData[slot].weaponName}.");
    }

    public void SwitchToSlot(int slot) => SetActiveSlot(slot);

    public WeaponData GetActiveWeaponData()
    {
        if (equippedData.Count == 0) return null;
        return equippedData[activeSlot];
    }
}