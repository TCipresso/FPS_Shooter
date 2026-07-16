using UnityEngine;
public abstract class TokenEffectSO : ScriptableObject
{
    public abstract void OnApply(PlayerStats stats);
    public abstract void OnRemove(PlayerStats stats);
}