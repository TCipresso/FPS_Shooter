using System.Collections.Generic;
using UnityEngine;

public class TokenInventory : MonoBehaviour
{
    public List<TokenDefinitionSO> activeTokens = new List<TokenDefinitionSO>();
    PlayerStats stats;
    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }
    public void AddToken(TokenDefinitionSO token)
    {
        if (token == null || activeTokens.Contains(token)) return;
        activeTokens.Add(token);
        token.effect?.OnApply(stats);
        Debug.Log($"[TokenInventory] Added token: {token.tokenName}");
    }
    public void RemoveToken(TokenDefinitionSO token)
    {
        if (token == null || !activeTokens.Contains(token)) return;
        token.effect?.OnRemove(stats);
        activeTokens.Remove(token);
    }
}