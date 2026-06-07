using UnityEngine;

[System.Serializable]
public class EnhancementSlot
{
    public BodySlotType slotType;
    public BodyEnhancementSO enhancement;
    [HideInInspector] public float cooldownTimer;

    public float CooldownFraction =>
        enhancement != null && enhancement.cooldown > 0f
            ? Mathf.Clamp01(cooldownTimer / enhancement.cooldown)
            : 0f;

    public bool OnCooldown => cooldownTimer > 0f;
}

public class BodyEnhancementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFpsController fpsController;
    [SerializeField] private FPSInput fpsInput;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform orientation;

    [Header("Slots")]
    [SerializeField] private EnhancementSlot armLeft = new EnhancementSlot { slotType = BodySlotType.Arm };
    [SerializeField] private EnhancementSlot armRight = new EnhancementSlot { slotType = BodySlotType.Arm };
    [SerializeField] private EnhancementSlot head = new EnhancementSlot { slotType = BodySlotType.Head };
    [SerializeField] private EnhancementSlot chest = new EnhancementSlot { slotType = BodySlotType.Chest };
    [SerializeField] private EnhancementSlot legLeft = new EnhancementSlot { slotType = BodySlotType.Leg };
    [SerializeField] private EnhancementSlot legRight = new EnhancementSlot { slotType = BodySlotType.Leg };

    private EnhancementSlot[] allSlots;
    private BodyEnhancementContext ctx;

    void Awake()
    {
        ctx = new BodyEnhancementContext(
            fpsController,
            fpsController.GetComponent<CharacterController>(),
            fpsInput,
            cameraHolder,
            orientation
        );

        allSlots = new[] { armLeft, armRight, head, chest, legLeft, legRight };

        foreach (var slot in allSlots)
        {
            if (slot.enhancement == null) continue;
            slot.enhancement.OnEquip(ctx);
            if (slot.enhancement.IsPassive)
                slot.enhancement.ApplyPassive(ctx);
        }
    }

    void Update()
    {
        foreach (var slot in allSlots)
        {
            if (slot.enhancement == null || slot.enhancement.IsPassive) continue;

            if (slot.cooldownTimer > 0f)
                slot.cooldownTimer -= Time.deltaTime;

            slot.enhancement.OnUpdate(ctx, ref slot.cooldownTimer);
        }
    }

    public void Equip(BodySlotType slotType, BodyEnhancementSO enhancement, int index = 0)
    {
        var slot = GetSlot(slotType, index);
        if (slot == null) return;

        if (slot.enhancement != null)
            slot.enhancement.OnUnequip(ctx);

        slot.enhancement = enhancement;
        slot.cooldownTimer = 0f;

        if (enhancement != null)
        {
            enhancement.OnEquip(ctx);
            if (enhancement.IsPassive)
                enhancement.ApplyPassive(ctx);
        }
    }

    public void Unequip(BodySlotType slotType, int index = 0)
    {
        var slot = GetSlot(slotType, index);
        if (slot?.enhancement == null) return;

        slot.enhancement.OnUnequip(ctx);
        slot.enhancement = null;
        slot.cooldownTimer = 0f;
    }

    // index handles the two arm/leg slots (0 = left, 1 = right)
    public EnhancementSlot GetSlot(BodySlotType slotType, int index = 0)
    {
        int count = 0;
        foreach (var slot in allSlots)
        {
            if (slot.slotType == slotType)
            {
                if (count == index) return slot;
                count++;
            }
        }
        return null;
    }
}