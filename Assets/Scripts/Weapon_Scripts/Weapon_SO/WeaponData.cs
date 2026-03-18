using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Info")]
    public string weaponName = "Weapon";
    public GameObject prefab;

    [Header("Spawn Offset")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
}