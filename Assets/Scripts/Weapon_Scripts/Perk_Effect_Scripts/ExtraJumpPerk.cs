using UnityEngine;

[CreateAssetMenu(menuName = "Bloodsport/Perks/ExtraJump")]
public class ExtraJumpPerk : WeaponPerkSO
{
    public override void OnEquip(WeaponBase weapon, PlayerFpsController controller)
        => controller.JumpCount++;

    public override void OnUnequip(WeaponBase weapon, PlayerFpsController controller)
        => controller.JumpCount--;
}