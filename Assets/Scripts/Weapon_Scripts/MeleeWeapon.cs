using UnityEngine;

public class MeleeWeapon : WeaponBase
{
    public override void Shoot()
    {
        // Melee attacks/parries are driven by PlayMeleeAttack()/PlayParry(),
        // dispatched from WeaponInventory via the dedicated melee/parry inputs.
    }
}
