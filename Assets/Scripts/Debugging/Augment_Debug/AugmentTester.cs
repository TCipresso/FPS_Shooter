using System.Collections.Generic;
using UnityEngine;

public class AugmentTester : MonoBehaviour
{
    [Header("Who receives the augments")]
    public GameObject player;                  // your player (has CarStats/PlayerController)

    [Header("Augments to test")]
    public List<AugmentData> augments = new List<AugmentData>();

    [Header("Options")]
    public bool logToConsole = true;

    // Apply ALL augments in the list (hook this to a UI Button)
    public void ApplyAll()
    {
        if (!player)
        {
            Debug.LogWarning("[AugmentTester] No player assigned.");
            return;
        }

        foreach (var a in augments)
        {
            if (a == null) continue;
            a.Apply(player);
            if (logToConsole) Debug.Log($"[AugmentTester] Applied: {a.augmentName}");
        }
    }

    // Apply a single augment by index (you can hook multiple buttons to this with different args)
    public void ApplyIndex(int index)
    {
        if (!player)
        {
            Debug.LogWarning("[AugmentTester] No player assigned.");
            return;
        }
        if (index < 0 || index >= augments.Count)
        {
            Debug.LogWarning($"[AugmentTester] Index {index} out of range.");
            return;
        }

        var a = augments[index];
        if (a == null) return;

        a.Apply(player);
        if (logToConsole) Debug.Log($"[AugmentTester] Applied: {a.augmentName} (index {index})");
    }

    // Apply one random augment from the list (optional extra)
    public void ApplyRandom()
    {
        if (!player || augments.Count == 0) return;

        int tries = 0;
        AugmentData pick = null;
        // avoid nulls if there are gaps
        while (tries < 10 && pick == null)
        {
            pick = augments[Random.Range(0, augments.Count)];
            tries++;
        }
        if (pick == null) return;

        pick.Apply(player);
        if (logToConsole) Debug.Log($"[AugmentTester] Applied RANDOM: {pick.augmentName}");
    }

    // Handy Inspector context-menu actions
    [ContextMenu("Apply All (Context)")]
    void ContextApplyAll() => ApplyAll();

    [ContextMenu("Apply Random (Context)")]
    void ContextApplyRandom() => ApplyRandom();
}
