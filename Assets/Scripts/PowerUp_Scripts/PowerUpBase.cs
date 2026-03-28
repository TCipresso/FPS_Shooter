using UnityEngine;

public enum PowerUpType
{
    Instant,
    Duration
}

public abstract class PowerUpBase : MonoBehaviour
{
    [Header("Power Up Settings")]
    public string powerUpName = "Power Up";
    public PowerUpType powerUpType = PowerUpType.Instant;

    [Header("Duration Settings")]
    [Tooltip("Only used if PowerUpType is Duration")]
    public float duration = 10f;

    protected PlayerStats playerStats;
    protected WeaponInventory weaponInventory;

    protected virtual void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        weaponInventory = FindFirstObjectByType<WeaponInventory>();
    }

    public void Activate()
    {
        if (powerUpType == PowerUpType.Instant)
        {
            ApplyEffect();
            Debug.Log($"[PowerUp] {powerUpName} activated instantly.");
        }
        else
        {
            StartCoroutine(RunDuration());
            Debug.Log($"[PowerUp] {powerUpName} activated for {duration} seconds.");
        }
    }

    System.Collections.IEnumerator RunDuration()
    {
        ApplyEffect();
        yield return new WaitForSeconds(duration);
        RemoveEffect();
        Debug.Log($"[PowerUp] {powerUpName} expired.");
    }

    // Apply the power up effect
    protected abstract void ApplyEffect();

    // Only needed for duration based power ups
    protected virtual void RemoveEffect() { }
}