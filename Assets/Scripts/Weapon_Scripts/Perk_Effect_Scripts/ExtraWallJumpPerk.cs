using UnityEngine;

[CreateAssetMenu(menuName = "Bloodsport/Perks/ExtraWallJump")]
public class ExtraWallJumpPerk : WeaponPerkSO
{
    public override void OnEquip(WeaponBase weapon, PlayerFpsController controller)
        => controller.WallJumpCount++;

    public override void OnUnequip(WeaponBase weapon, PlayerFpsController controller)
        => controller.WallJumpCount--;
}