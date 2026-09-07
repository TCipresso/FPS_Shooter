using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponEntry
{
    public WeaponDefinitionSO definition;

    public GameObject weaponRoot;

    public List<WeaponBase> weaponBases = new List<WeaponBase>();

    [Header("Attachments (per weapon)")]
    [Tooltip("Curated attachments equipped on this weapon. Applied to runtimeDefinition on setup.")]
    public List<WeaponAttachmentSO> attachments = new List<WeaponAttachmentSO>();

    [System.NonSerialized]
    public WeaponDefinitionSO runtimeDefinition;

    public WeaponBase Primary => weaponBases.Count > 0 ? weaponBases[0] : null;

    public WeaponDefinitionSO RuntimeDefinition => runtimeDefinition != null ? runtimeDefinition : definition;
}