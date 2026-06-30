using UnityEngine;

public abstract class PowerEffectSO : ScriptableObject
{
    public abstract void OnApply(PlayerStats stats);
    public abstract void OnRemove(PlayerStats stats);
}