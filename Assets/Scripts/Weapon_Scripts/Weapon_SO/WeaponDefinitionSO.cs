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
public enum BulletType { Hitscan, Projectile }
[CreateAssetMenu(fileName = "NewWeaponDefinition", menuName = "Zarcade/Weapon Definition")]
public class WeaponDefinitionSO : ScriptableObject
{
    [Header("Info (label only, not used as an ID)")]
    public string weaponName = "Weapon";
    public WeaponType category;

    [Header("Drop Prefab")]
    [Tooltip("This weapon's own hand-built pickup prefab (must have a WeaponPickup component and be registered as a spawnable prefab in the NetworkManager).")]
    public GameObject dropPrefab;

    [Header("Stats")]
    public int damage = 25;
    public float range = 50f;
    public float swarmHitRadius = 0.4f;
    public bool isAutomatic = false;
    public float rpm = 300f;
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critMultiplier = 2f;
    public bool isShotgun = false;
    public int pelletCount = 8;
    public float pelletSpreadAngle = 5f;
    public bool flatSpread = false;

    [Header("Accuracy")]
    public float baseAccuracy = 1f;
    public float bloomPerShot = 0.5f;
    public float maxBloom = 4f;
    public float bloomDecaySpeed = 3f;

    [Header("Bullet Type")]
    public BulletType bulletType = BulletType.Hitscan;

    [Header("Trail")]
    public BulletTrail trailPrefab;
    public string trailPoolKey = "BulletTrail";
    public int trailPoolSize = 10;

    [Header("Hit")]
    public LayerMask hitMask = ~0;
    public GameObject impactEffectOverride;

    [Header("Projectile Only")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 40f;
    public float projectileGravityScale = 0f;

    [Header("Explosion (AoE)")]
    [Tooltip("If true, the projectile explodes on impact and damages every zombie in explosionRadius instead of a single target.")]
    public bool isExplosive = false;
    [Tooltip("Radius of the explosion damage in world units.")]
    public float explosionRadius = 4f;
    [Tooltip("Pooled particle effect spawned at the impact point. Shows on all clients.")]
    public GameObject explosionEffectPrefab;
    [Tooltip("How long before the explosion effect returns to its pool.")]
    public float explosionEffectDuration = 2f;
    [Tooltip("Impulse applied to the firing player when caught in their own explosion. 0 = no rocket jump.")]
    public float explosionSelfKnockback = 18f;
    [Tooltip("Upward bias mixed into the knockback direction so ground shots launch you up, not just sideways.")]
    public float explosionKnockbackUpBias = 0.6f;

    [Header("Leveling")]
    public int maxLevel = 5;

    /*
    [Header("Leveling (Pack-a-Punch style)")]
    [Tooltip("Level 1 stats come from whatever is already set on the pre-placed WeaponBase. This only defines how it scales.")]
    public float damageGrowthPerLevel = 0.10f;
    public float rpmGrowthPerLevel = 0.10f;
    public float rangeGrowthPerLevel = 0.10f;

    [Header("Power-Up Behavior")]
    [Tooltip("How long this weapon stays active per level stage when picked up as a power-up. Trickles down one level per expiry until it reaches level 1, then reverts to the base weapon.")]
    public float powerUpDurationPerLevel = 30f;
    */

    [Header("Skin (Level 2+)")]
    [Tooltip("Applied to every material slot on the weapon's skinRenderer once level > 1. Level 1 keeps the original placeholder materials untouched.")]
    public Material packedMaterial;
    [Tooltip("Index 0 = level 2 tint, index 1 = level 3 tint, etc.")]
    public Color[] levelTintColors;
    public string tintPropertyName = "_BaseColor";
}