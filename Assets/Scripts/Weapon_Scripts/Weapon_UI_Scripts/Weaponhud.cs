using UnityEngine;
using TMPro;

public class WeaponHUD : MonoBehaviour
{
    [Header("References")]
    public WeaponInventory weaponInventory;

    [Header("Ammo UI")]
    public TextMeshProUGUI magText;
    public TextMeshProUGUI reserveText;

    void Update()
    {
        WeaponBase activeWeapon = weaponInventory.GetActiveWeaponBase();

        if (activeWeapon == null)
        {
            magText.text = "--";
            reserveText.text = "--";
            return;
        }

        magText.text = activeWeapon.currentMag.ToString();
        reserveText.text = activeWeapon.reserveAmmo.ToString();
    }
}