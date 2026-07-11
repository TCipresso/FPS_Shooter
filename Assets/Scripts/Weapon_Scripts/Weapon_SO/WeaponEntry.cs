using UnityEngine;

[System.Serializable]
public class WeaponEntry
{
    [Tooltip("Check this on exactly one entry - the weapon that starts equipped.")]
    public bool isDefaultBase;

    public WeaponDefinitionSO definition;

    [Tooltip("The root GameObject of the weapon (parent of everything - mesh, muzzle, WeaponBase). This is what gets enabled/disabled.")]
    public GameObject weaponRoot;

    [Tooltip("The WeaponBase component, wherever it lives in this weapon's hierarchy.")]
    public WeaponBase weaponBase;

    [HideInInspector] public int baseLevel = 1;
    [HideInInspector] public int currentLevel = 1;

    [System.NonSerialized] public Material[] originalMaterials;
}