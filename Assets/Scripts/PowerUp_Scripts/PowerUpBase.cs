using UnityEngine;

public enum PowerUpType
{
    Instant,
    Duration
}

public enum PowerUpActivation
{
    WalkOn,
    Manual
}

public abstract class PowerUpBase : MonoBehaviour
{
    [Header("Power Up Settings")]
    public string powerUpName = "Power Up";
    public PowerUpType powerUpType = PowerUpType.Instant;
    public PowerUpActivation activation = PowerUpActivation.WalkOn;

    [Header("Duration Settings")]
    [Tooltip("Only used if PowerUpType is Duration")]
    public float duration = 10f;

    [Header("Event")]
    public PowerUpEvent powerUpEvent;

    protected PlayerStats playerStats;
    protected WeaponInventory weaponInventory;
    PowerUpEventListener eventListener;

    protected virtual void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        weaponInventory = FindFirstObjectByType<WeaponInventory>();
        eventListener = FindFirstObjectByType<PowerUpEventListener>();

        if (eventListener == null)
            Debug.LogWarning("[PowerUpBase] No PowerUpEventListener found in scene!");
    }

    void OnTriggerEnter(Collider other)
    {
        if (activation != PowerUpActivation.WalkOn) return;
        if (!other.CompareTag("Player")) return;

        Activate();
        Destroy(gameObject);
    }

    public void Activate()
    {
        // Fire the event
        if (eventListener != null && powerUpEvent != null)
            eventListener.OnPowerUpActivated(powerUpEvent);

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

    protected abstract void ApplyEffect();
    protected virtual void RemoveEffect() { }
}