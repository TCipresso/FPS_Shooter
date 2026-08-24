using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponEntry
{
    public WeaponDefinitionSO definition;

    public GameObject weaponRoot;

    public List<WeaponBase> weaponBases = new List<WeaponBase>();

    [System.NonSerialized]
    public WeaponDefinitionSO runtimeDefinition;

    public WeaponBase Primary => weaponBases.Count > 0 ? weaponBases[0] : null;

    public WeaponDefinitionSO RuntimeDefinition => runtimeDefinition != null ? runtimeDefinition : definition;
}