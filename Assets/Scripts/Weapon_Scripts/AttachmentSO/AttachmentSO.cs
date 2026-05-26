using UnityEngine;

public enum StatType
{
    Damage,
    Rpm,
    MagSize,
    ReserveAmmo,
    RangeStat,
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

    [Header("Animation Override")]
    public AnimationClip overrideClip;
    public AnimationClip clipToReplace;

    [Header("Reticle")]
    public bool overrideCrosshair = false;
    public Sprite reticleSprite;
    public bool fadeToNothing = false;
    public Color reticleColor = Color.white;
    public float reticleScale = 1f;

    [Header("Barrel")]
    public ParticleSystem muzzleFlashOverride;
    public string muzzlePointName = ""; // name of child GO on the gun prefab
    public AudioClip fireSoundOverride;

    [Header("Modifiers")]
    public StatModifier[] modifiers;
}