using UnityEngine;

public abstract class WeaponPerkSO : ScriptableObject
{
    public string perkName;

    public abstract void OnEquip(WeaponBase weapon, PlayerFpsController controller);
    public abstract void OnUnequip(WeaponBase weapon, PlayerFpsController controller);
}