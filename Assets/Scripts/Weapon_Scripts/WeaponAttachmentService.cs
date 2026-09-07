using System.Collections.Generic;
using UnityEngine;

// Applies a WeaponEntry's equipped attachments to its per-run runtimeDefinition + visuals.
// Stack-aware: multiple copies of the same attachment SO are counted, and each modifier row
// is resolved through StackResolver (per its own StackMode) - not applied N times.
public static class WeaponAttachmentService
{
    static readonly Dictionary<WeaponAttachmentSO, int> counts = new Dictionary<WeaponAttachmentSO, int>();

    // Full apply - assumes runtimeDefinition is fresh (called after CreateRuntimeDefinition).
    public static void Apply(WeaponEntry entry)
    {
        if (entry == null || entry.runtimeDefinition == null)
            return;

        WeaponAttachmentPoints points = ResolvePoints(entry);
        points?.ResetAll();

        if (entry.attachments == null || entry.attachments.Count == 0)
            return;

        WeaponType category = entry.definition != null ? entry.definition.category : default;

        counts.Clear();
        for (int i = 0; i < entry.attachments.Count; i++)
        {
            WeaponAttachmentSO a = entry.attachments[i];
            if (a == null) continue;
            counts.TryGetValue(a, out int c);
            counts[a] = c + 1;
        }

        foreach (KeyValuePair<WeaponAttachmentSO, int> kv in counts)
        {
            WeaponAttachmentSO attachment = kv.Key;
            int count = kv.Value;

            if (!attachment.FitsCategory(category))
            {
                Debug.LogWarning($"[Attachments] '{attachment.displayName}' isn't compatible with {category} - stats applied anyway.");
            }

            if (points != null && attachment.visualSlots != null)
                foreach (string slot in attachment.visualSlots)
                    points.SetSlot(slot, true);

            foreach (AttachmentStatMod mod in attachment.statMods)
            {
                float total = StackResolver.Resolve(mod.value, count, mod.stackMode, mod.stackParam);
                WeaponStatMath.Apply(entry.runtimeDefinition, mod.stat, mod.scaling, total);
            }
        }

        if (entry.Primary != null)
            entry.Primary.Refill();
    }

    // Grant ONE attachment at runtime (loot chest). Applies only the DELTA from the previous
    // stack count to the new one, so diminishing / capped modes resolve correctly without a
    // full rebuild of runtimeDefinition.
    public static void GrantOne(WeaponEntry entry, WeaponAttachmentSO attachment)
    {
        if (entry == null || attachment == null || entry.runtimeDefinition == null)
        {
            Debug.LogWarning("[Attachments] GrantOne: missing entry / attachment / runtimeDefinition.");
            return;
        }

        if (entry.attachments == null)
            entry.attachments = new List<WeaponAttachmentSO>();

        int oldCount = 0;
        for (int i = 0; i < entry.attachments.Count; i++)
            if (entry.attachments[i] == attachment) oldCount++;

        entry.attachments.Add(attachment);
        int newCount = oldCount + 1;

        WeaponType category = entry.definition != null ? entry.definition.category : default;
        if (!attachment.FitsCategory(category))
            Debug.LogWarning($"[Attachments] '{attachment.displayName}' isn't compatible with {category} - applying anyway.");

        WeaponDefinitionSO def = entry.runtimeDefinition;

        foreach (AttachmentStatMod mod in attachment.statMods)
        {
            float before = StackResolver.Resolve(mod.value, oldCount, mod.stackMode, mod.stackParam);
            float after = StackResolver.Resolve(mod.value, newCount, mod.stackMode, mod.stackParam);

            // CritChance is always additive in WeaponStatMath regardless of scaling.
            bool additive = mod.scaling == UpgradeScalingType.Flat || mod.stat == WeaponUpgradeStatType.CritChance;

            if (additive)
            {
                WeaponStatMath.Apply(def, mod.stat, UpgradeScalingType.Flat, after - before);
            }
            else
            {
                // move the stat from x(1+before) to x(1+after): multiply by (1+after)/(1+before)
                float deltaFactor = (1f + after) / (1f + before) - 1f;
                WeaponStatMath.Apply(def, mod.stat, UpgradeScalingType.Percentage, deltaFactor);
            }
        }

        WeaponAttachmentPoints points = ResolvePoints(entry);
        if (points != null && attachment.visualSlots != null)
            foreach (string slot in attachment.visualSlots)
                points.SetSlot(slot, true);

        if (entry.Primary != null)
            entry.Primary.Refill();

        Debug.Log($"[Attachments] Granted '{attachment.displayName}' x{newCount} to {(entry.definition != null ? entry.definition.weaponName : "weapon")}.");
    }

    static WeaponAttachmentPoints ResolvePoints(WeaponEntry entry)
    {
        return entry.weaponRoot != null
            ? entry.weaponRoot.GetComponentInChildren<WeaponAttachmentPoints>(true)
            : null;
    }
}
