using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AttachmentStatMod
{
    public WeaponUpgradeStatType stat;   // shared enum with the level-up engine
    public UpgradeScalingType scaling;   // Percentage = x(1+v), Flat = +v
    public float value;                  // fixed - attachments are curated, not rolled
}

// A curated weapon attachment (your "Risk of Rain item" for guns). Grants stat buffs and
// toggles pre-placed visual parts on the weapon. Non-stat abilities come later via an
// effects list; this is the baseline.
[CreateAssetMenu(fileName = "NewWeaponAttachment", menuName = "Zarcade/Weapon Attachment")]
public class WeaponAttachmentSO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Attachment";
    public Sprite icon;
    [TextArea] public string description;

    [Header("Compatibility")]
    [Tooltip("Weapon categories this can attach to. Empty = fits any weapon.")]
    public WeaponType[] compatibleCategories;

    [Header("Visuals")]
    [Tooltip("Slot keys to enable on the weapon's WeaponAttachmentPoints (e.g. \"optic\", \"suppressor\").")]
    public string[] visualSlots;

    [Header("Stat Buffs")]
    public List<AttachmentStatMod> statMods = new List<AttachmentStatMod>();

    public bool FitsCategory(WeaponType category)
    {
        if (compatibleCategories == null || compatibleCategories.Length == 0)
            return true;
        for (int i = 0; i < compatibleCategories.Length; i++)
            if (compatibleCategories[i] == category)
                return true;
        return false;
    }
}
