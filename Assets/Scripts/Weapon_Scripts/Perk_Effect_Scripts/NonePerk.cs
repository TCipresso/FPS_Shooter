using UnityEngine;

[CreateAssetMenu(menuName = "Bloodsport/Perks/None")]
public class NonePerk : WeaponPerkSO
{
    public override void OnEquip(WeaponBase weapon, PlayerFpsController controller) { }
    public override void OnUnequip(WeaponBase weapon, PlayerFpsController controller) { }
}