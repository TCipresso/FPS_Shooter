using UnityEngine;
using TMPro;

public class WeaponHUD : MonoBehaviour
{
    [Header("References")]
    public WeaponInventory weaponInventory;

    [Header("Left Hand Ammo")]
    public TextMeshProUGUI leftMagText;
    public TextMeshProUGUI leftReserveText;

    [Header("Right Hand Ammo")]
    public TextMeshProUGUI rightMagText;
    public TextMeshProUGUI rightReserveText;

    void Update()
    {
        UpdateSlot(0, leftMagText, leftReserveText);
        UpdateSlot(1, rightMagText, rightReserveText);
    }

    void UpdateSlot(int slot, TextMeshProUGUI magText, TextMeshProUGUI reserveText)
    {
        GameObject weaponGO = weaponInventory.equippedWeapons[slot];
        if (weaponGO == null)
        {
            magText.text = "--";
            reserveText.text = "--";
            return;
        }

        WeaponBase wb = weaponGO.GetComponentInChildren<WeaponBase>();
        if (wb == null)
        {
            magText.text = "--";
            reserveText.text = "--";
            return;
        }

        magText.text = wb.currentMag.ToString();
        reserveText.text = wb.reserveAmmo.ToString();
    }
}