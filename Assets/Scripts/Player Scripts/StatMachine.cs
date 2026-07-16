using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class StatMachine : Buyable
{
    [Header("Stat Settings")]
    public List<PlayerStatType> possibleStats;
    public float minAmount = 1f;
    public float maxAmount = 5f;

    [Header("Display")]
    public TMP_Text statNameText;

    public PlayerStatType statType { get; private set; }

    void OnEnable()
    {
        RollStatType();
    }

    void RollStatType()
    {
        if (possibleStats == null || possibleStats.Count == 0) return;
        statType = possibleStats[Random.Range(0, possibleStats.Count)];
        if (statNameText != null) statNameText.text = statType.ToString();
    }

    protected override void OnPurchase(PlayerStats stats)
    {
        float amount = Random.Range(minAmount, maxAmount);
        StatEffectApplier.Apply(stats, statType, amount);
        Debug.Log($"[StatMachine] Gave {amount:F2} of {statType}");
    }
}