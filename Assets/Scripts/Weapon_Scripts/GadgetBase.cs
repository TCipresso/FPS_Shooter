using UnityEngine;
using System.Collections.Generic;

public abstract class GadgetBase : MonoBehaviour
{
    [Header("Hand")]
    public bool isRightHand = false;

    [Header("Info")]
    public string gadgetName = "Gadget";

    [Header("Cooldown")]
    public float cooldown = 10f;
    [HideInInspector] public float baseCooldown;

    [Header("Stats")]
    public float duration = 5f;
    public float potency = 1f;

    [HideInInspector] public GadgetInstance currentInstance;

    private float lastUsedTime = -Mathf.Infinity;

    public bool IsReady => Time.time >= lastUsedTime + cooldown;
    public float CooldownRemaining => Mathf.Max(0f, (lastUsedTime + cooldown) - Time.time);
    public float CooldownProgress => Mathf.Clamp01((Time.time - lastUsedTime) / cooldown);

    protected PlayerStats playerStats;
    protected PlayerFpsController fpsController;

    protected virtual void Awake()
    {
        baseCooldown = cooldown;
        playerStats = FindFirstObjectByType<PlayerStats>();
        fpsController = FindFirstObjectByType<PlayerFpsController>();
    }

    public void Equip(GadgetInstance instance)
    {
        if (instance == null || instance.definition == null) return;

        currentInstance = instance;
        gadgetName = instance.definition.gadgetName;
        cooldown = instance.finalCooldown;
        baseCooldown = instance.finalCooldown;
        duration = instance.finalDuration;
        potency = instance.finalPotency;

        Debug.Log($"[GadgetBase] Equipped {gadgetName} | Rarity: {instance.rarity} | Cooldown: {cooldown:F1}s | Potency: {potency:F2}");
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

    public void ApplyPerks(List<WeaponPerkSO> perks)
    {
        if (perks == null) return;
        foreach (WeaponPerkSO perk in perks)
            perk?.OnEquip(null, fpsController);
    }

    public void RemovePerks(List<WeaponPerkSO> perks)
    {
        if (perks == null) return;
        foreach (WeaponPerkSO perk in perks)
            perk?.OnUnequip(null, fpsController);
    }
}