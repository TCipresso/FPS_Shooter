using UnityEngine;

public abstract class GadgetBase : MonoBehaviour
{
    [Header("Info")]
    public string gadgetName = "Gadget";

    [Header("Cooldown")]
    public float cooldown = 5f;
    protected float lastUsedTime = -Mathf.Infinity;

    public bool IsReady => Time.time >= lastUsedTime + cooldown;
    public float CooldownRemaining => Mathf.Max(0f, (lastUsedTime + cooldown) - Time.time);
    public float CooldownProgress => Mathf.Clamp01((Time.time - lastUsedTime) / cooldown);

    protected PlayerStats playerStats;
    protected PlayerFpsController fpsController;

    protected virtual void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        fpsController = FindFirstObjectByType<PlayerFpsController>();
    }

    public void TryUse()
    {
        if (!IsReady)
        {
            Debug.Log($"[GadgetBase] {gadgetName} on cooldown. {CooldownRemaining:F1}s remaining.");
            return;
        }

        OnUse();
        lastUsedTime = Time.time;
        Debug.Log($"[GadgetBase] {gadgetName} used.");
    }

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }

    protected abstract void OnUse();
}