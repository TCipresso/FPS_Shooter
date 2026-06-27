using UnityEngine;

public enum BulletType { Hitscan, Projectile }

[CreateAssetMenu(fileName = "BulletData", menuName = "Zarcade/Bullet Data")]
public class BulletDataSO : ScriptableObject
{
    [Header("Type")]
    public BulletType bulletType = BulletType.Hitscan;

    [Header("Shotgun")]
    public bool isShotgun = false;
    [Tooltip("Number of pellets fired per shot. Only used if isShotgun is true.")]
    public int pelletCount = 8;
    [Tooltip("Max random spread angle in degrees per pellet.")]
    public float pelletSpreadAngle = 5f;
    [Tooltip("If true, pellets spread evenly in a flat horizontal line instead of random angles.")]
    public bool flatSpread = false;

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
}