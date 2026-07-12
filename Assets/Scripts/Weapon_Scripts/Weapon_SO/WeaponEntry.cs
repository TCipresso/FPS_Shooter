using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponEntry
{
    [Tooltip("Check this on exactly one entry - the weapon that starts equipped.")]
    public bool isDefaultBase;

    public WeaponDefinitionSO definition;

    [Tooltip("The root GameObject of the weapon (parent of everything under it - mesh, muzzle, WeaponBase). This is what gets enabled/disabled.")]
    public GameObject weaponRoot;

    [Tooltip("Usually just one WeaponBase. For dual-wield weapons (e.g. Mac-10 left + right), add both here - level, stats, and skin apply to all of them together.")]
    public List<WeaponBase> weaponBases = new List<WeaponBase>();

    [HideInInspector] public int baseLevel = 1;
    [HideInInspector] public int currentLevel = 1;

    public WeaponBase Primary => weaponBases.Count > 0 ? weaponBases[0] : null;
}