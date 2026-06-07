using UnityEngine;

public enum BodySlotType { Arm, Head, Chest, Leg }

public abstract class BodyEnhancementSO : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    public Sprite icon;
    public BodySlotType slotType;

    [Header("Cooldown")]
    public float cooldown = 0f;

    public virtual bool IsPassive => false;

    public virtual void OnEquip(BodyEnhancementContext ctx) { }
    public virtual void OnUnequip(BodyEnhancementContext ctx) { }
    public virtual void OnUpdate(BodyEnhancementContext ctx, ref float cooldownTimer) { }
    public virtual void ApplyPassive(BodyEnhancementContext ctx) { }
}