using System.Collections.Generic;
using UnityEngine;
public class SlotMachine : Buyable
{
    [Header("Outcome Weights")]
    public float statChance = 1f;
    public float tokenChance = 1f;
    public float nothingChance = 1f;

    [Header("Stat Roll")]
    public List<PlayerStatType> possibleStats;
    public float minAmount = 1f;
    public float maxAmount = 5f;

    [Header("Token Roll")]
    public List<TokenDefinitionSO> possibleTokens;

    protected override void OnPurchase(PlayerStats stats)
    {
        float total = statChance + tokenChance + nothingChance;
        float roll = Random.Range(0f, total);

        if (roll < statChance)
        {
            RollStat(stats);
        }
        else if (roll < statChance + tokenChance)
        {
            RollToken(stats);
        }
        else
        {
            Debug.Log("[SlotMachine] No prize this time.");
        }
    }

    void RollStat(PlayerStats stats)
    {
        if (possibleStats == null || possibleStats.Count == 0) return;
        PlayerStatType type = possibleStats[Random.Range(0, possibleStats.Count)];
        float amount = Random.Range(minAmount, maxAmount);
        StatEffectApplier.Apply(stats, type, amount);
        Debug.Log($"[SlotMachine] Gave {amount:F2} of {type}");
    }

    void RollToken(PlayerStats stats)
    {
        if (possibleTokens == null || possibleTokens.Count == 0) return;
        TokenDefinitionSO token = possibleTokens[Random.Range(0, possibleTokens.Count)];
        TokenInventory inventory = stats.GetComponent<TokenInventory>();
        if (inventory != null)
        {
            inventory.AddToken(token);
            Debug.Log($"[SlotMachine] Gave token: {token.tokenName}");
        }
        else
        {
            Debug.LogWarning("[SlotMachine] No TokenInventory found on player.");
        }
    }
}