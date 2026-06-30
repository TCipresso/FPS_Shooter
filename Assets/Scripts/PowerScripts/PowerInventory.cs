using System.Collections.Generic;
using UnityEngine;

public class PowerInventory : MonoBehaviour
{
    public List<PowerDefinitionSO> activePowers = new List<PowerDefinitionSO>();
    PlayerStats stats;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    public void AddPower(PowerDefinitionSO power)
    {
        if (power == null || activePowers.Contains(power)) return;

        activePowers.Add(power);
        power.effect?.OnApply(stats);

        Debug.Log($"[PowerInventory] Added power: {power.powerName}");
    }

    public void RemovePower(PowerDefinitionSO power)
    {
        if (power == null || !activePowers.Contains(power)) return;

        power.effect?.OnRemove(stats);
        activePowers.Remove(power);
    }
}