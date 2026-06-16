using UnityEngine;
using System.Collections.Generic;

public abstract class GadgetBase : MonoBehaviour
{
    [HideInInspector] public WeaponInstance currentInstance;

    [Header("Animation")]
    public Animator animator;

    protected PlayerFpsController fpsController;
    protected PlayerStats playerStats;

    protected virtual void Awake()
    {
        fpsController = FindFirstObjectByType<PlayerFpsController>();
        playerStats = FindFirstObjectByType<PlayerStats>();
    }

    protected virtual void OnEnable()
    {
    }

    protected virtual void Update()
    {
        OnHeld();
    }

    // Called every frame while gadget is in hand
    public abstract void OnHeld();

    // Called on right click
    public abstract void Activate();

    public void Equip(WeaponInstance instance)
    {
        if (instance == null || instance.definition == null) return;
        currentInstance = instance;
        ApplyPerks(instance.rolledPerks);
        Debug.Log($"[GadgetBase] Equipped {instance.definition.weaponName} ({instance.rarity})");
    }

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