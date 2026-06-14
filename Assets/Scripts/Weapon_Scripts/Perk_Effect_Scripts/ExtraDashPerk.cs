using UnityEngine;

[CreateAssetMenu(menuName = "Bloodsport/Perks/ExtraDash")]
public class ExtraDashPerk : WeaponPerkSO
{
    public override void OnEquip(WeaponBase weapon, PlayerFpsController controller)
        => controller.DashCharges++;

    public override void OnUnequip(WeaponBase weapon, PlayerFpsController controller)
        => controller.DashCharges--;
}