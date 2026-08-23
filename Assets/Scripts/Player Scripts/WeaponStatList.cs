using UnityEngine;
using TMPro;
using System.Text;
public class WeaponStatList : MonoBehaviour
{
    /*[Header("References")]
    public WeaponInventory weaponInventory;
    public TMP_Text statListText;
    StringBuilder sb = new StringBuilder();
    void Update()
    {
        if (weaponInventory == null || statListText == null) return;
        WeaponBase weapon = weaponInventory.GetActiveWeaponBase();
        if (weapon == null || weapon.weaponDefinition == null)
        {
            statListText.text = "";
            return;
        }
        WeaponDefinitionSO def = weapon.weaponDefinition;
        sb.Clear();
        sb.AppendLine($"{def.weaponName} (Lv {def.level}/{def.maxLevel})");
        sb.AppendLine($"XP: {def.currentXP:F0}/{def.GetXPToNextLevel():F0}");
        sb.AppendLine($"Damage: {def.damage}");
        sb.AppendLine($"RPM: {def.rpm:F0}");
        sb.AppendLine($"Range: {def.range:F0}");
        sb.AppendLine($"Crit Chance (weapon): {def.critChance * 100f:F1}%");
        sb.AppendLine($"Crit Chance (total): {weapon.critChance * 100f:F1}%");
        sb.AppendLine($"Crit Mult (weapon): {def.critMultiplier:F2}x");
        sb.AppendLine($"Crit Mult (total): {weapon.critMultiplier:F2}x");
        statListText.text = sb.ToString();
    }*/
}
