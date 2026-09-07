using UnityEngine;

// Applies a WeaponEntry's equipped attachments to its per-run runtimeDefinition + visuals.
// Called from WeaponInventory after the runtime clone is created. Reusable later for a
// runtime rebuild when attachments can be swapped mid-run.
public static class WeaponAttachmentService
{
    public static void Apply(WeaponEntry entry)
    {
        if (entry == null || entry.runtimeDefinition == null)
            return;

        WeaponAttachmentPoints points = entry.weaponRoot != null
            ? entry.weaponRoot.GetComponentInChildren<WeaponAttachmentPoints>(true)
            : null;

        points?.ResetAll();

        if (entry.attachments == null || entry.attachments.Count == 0)
            return;

        WeaponType category = entry.definition != null ? entry.definition.category : default;

        foreach (WeaponAttachmentSO attachment in entry.attachments)
        {
            if (attachment == null)
                continue;

            if (!attachment.FitsCategory(category))
            {
                Debug.LogWarning($"[Attachments] '{attachment.displayName}' is not compatible with {category} ({entry.definition?.weaponName}). Skipped.");
                continue;
            }

            if (points != null && attachment.visualSlots != null)
                foreach (string slot in attachment.visualSlots)
                    points.SetSlot(slot, true);

            foreach (AttachmentStatMod mod in attachment.statMods)
                WeaponStatMath.Apply(entry.runtimeDefinition, mod.stat, mod.scaling, mod.value);
        }
    }
}
