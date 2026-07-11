using UnityEngine;

public enum WeaponType
{
    Pistol,
    SMG,
    AR,
    Shotgun,
    Sniper,
    LMG,
    Launcher
}

[CreateAssetMenu(fileName = "NewWeaponDefinition", menuName = "Zarcade/Weapon Definition")]
public class WeaponDefinitionSO : ScriptableObject
{
    [Header("Info (label only, not used as an ID)")]
    public string weaponName = "Weapon";
    public WeaponType category;

    [Header("Leveling (Pack-a-Punch style)")]
    [Tooltip("Level 1 stats come from whatever is already set on the pre-placed WeaponBase. This only defines how it scales.")]
    public int maxLevel = 5;
    public float damageGrowthPerLevel = 0.10f;
    public float rpmGrowthPerLevel = 0.10f;
    public float rangeGrowthPerLevel = 0.10f;

    [Header("Power-Up Behavior")]
    [Tooltip("How long this weapon stays active per level stage when picked up as a power-up. Trickles down one level per expiry until it reaches level 1, then reverts to the base weapon.")]
    public float powerUpDurationPerLevel = 30f;

    [Header("Skin (Level 2+)")]
    [Tooltip("Applied to every material slot on the weapon's skinRenderer once level > 1. Level 1 keeps the original placeholder materials untouched.")]
    public Material packedMaterial;
    [Tooltip("Index 0 = level 2 tint, index 1 = level 3 tint, etc.")]
    public Color[] levelTintColors;
    public string tintPropertyName = "_BaseColor";
}