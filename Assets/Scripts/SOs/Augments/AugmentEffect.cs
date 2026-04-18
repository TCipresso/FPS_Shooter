using UnityEngine;

public abstract class AugmentEffect : ScriptableObject
{
    // Every effect must implement this method
    public abstract void ApplyEffect(GameObject player);
}
