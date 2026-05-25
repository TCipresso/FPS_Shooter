using UnityEngine;

public enum StatType
{
    Damage,
    Rpm,
    MagSize,
    ReserveAmmo,
    Range,
    ReloadTime
}

public enum ModifierType
{
    Additive,
    Multiplicative
}

[System.Serializable]
public class StatModifier
{
    public StatType stat;
    public ModifierType modifierType;
    public float value;
}

[CreateAssetMenu(fileName = "NewAttachment", menuName = "Bloodsport/Attachment")]
public class AttachmentSO : ScriptableObject
{
    [Header("Info")]
    public string attachmentName = "Attachment";
    public string slotType;

    [Header("Visual")]
    public string modelName = "";

    [Header("Modifiers")]
    public StatModifier[] modifiers;
}