using UnityEngine;


public abstract class PickupEffectSO : ScriptableObject
{
    [Header("Pickup Feedback")]
    public string displayName = "Pickup";
    public AudioClip pickupSFX;
    public GameObject pickupVFXPrefab;

    public abstract void Apply(GameObject player);
}