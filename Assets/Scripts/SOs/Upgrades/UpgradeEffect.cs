using UnityEngine;

public abstract class UpgradeEffect : ScriptableObject
{
    // Every upgrade must implement how it affects the player
    public abstract void ApplyUpgrade(GameObject player, float value);
}
