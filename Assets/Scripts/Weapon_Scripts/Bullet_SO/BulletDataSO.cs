using UnityEngine;

public enum BulletType { Hitscan, Projectile }

[CreateAssetMenu(fileName = "BulletData", menuName = "Bloodsport/Bullet Data")]
public class BulletDataSO : ScriptableObject
{
    [Header("Type")]
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
}