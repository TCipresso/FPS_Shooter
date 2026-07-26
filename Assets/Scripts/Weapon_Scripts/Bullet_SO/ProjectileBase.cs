using UnityEngine;
using Unity.Entities;
using float3 = Unity.Mathematics.float3;

public class ProjectileBase : MonoBehaviour
{
    [HideInInspector] public GameObject pool;

    Vector3 position;
    Vector3 velocity;
    float gravityScale;
    float life;
    float radius;
    int damage;
    bool applyDamage;
    LayerMask hitMask;
    WeaponBase owner;
    BulletDataSO data;

    TrailRenderer trail;
    bool trailCached;

    public void Launch(Vector3 origin, Vector3 direction, float speed, float gravityScale,
        float life, int damage, bool applyDamage, WeaponBase owner, BulletDataSO data, float radius)
    {
        this.position = origin;
        this.velocity = direction.normalized * speed;
        this.gravityScale = gravityScale;
        this.life = life;
        this.damage = damage;
        this.applyDamage = applyDamage;
        this.owner = owner;
        this.data = data;
        this.radius = radius;
        this.hitMask = data != null && data.hitMask != 0 ? data.hitMask : ~0;

        transform.position = origin;
        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity);

        if (!trailCached)
        {
            trail = GetComponentInChildren<TrailRenderer>();
            trailCached = true;
        }
        if (trail != null)
            trail.Clear();
    }

    public bool Tick(float dt)
    {
        if (dt <= 0f) return false;

        float speed = velocity.magnitude;
        Vector3 dir = speed > 0.0001f ? velocity / speed : transform.forward;
        float stepDist = speed * dt;

        bool didHitWorld = Physics.Raycast(position, dir, out RaycastHit hit, stepDist, hitMask);
        float searchDist = didHitWorld ? hit.distance : stepDist;

        bool hitZombie = ZombieDamageBridge.TryFindNearestZombieAlongRay(
            (float3)position, (float3)dir, searchDist, radius,
            out Entity zombie, out float3 zombieHitPos);

        if (hitZombie)
        {
            DoImpact((Vector3)zombieHitPos, -dir, true, zombie);
            OnDespawn();
            return true;
        }

        if (didHitWorld)
        {
            DoImpact(hit.point, hit.normal, false, Entity.Null);
            OnDespawn();
            return true;
        }

        position += velocity * dt;
        velocity += Vector3.down * (9.81f * gravityScale) * dt;

        transform.position = position;
        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity);

        life -= dt;
        if (life <= 0f)
        {
            OnDespawn();
            return true;
        }
        return false;
    }

    void OnDespawn()
    {
        if (trail != null)
            trail.Clear();
    }

    void DoImpact(Vector3 point, Vector3 normal, bool directZombieHit, Entity directZombie)
    {
        if (data != null && data.isExplosive)
        {
            if (ExplosionPool.Instance != null && data.explosionEffectPrefab != null)
                ExplosionPool.Instance.Spawn(data.explosionEffectPrefab, point, data.explosionEffectDuration);

            if (applyDamage)
                ZombieDamageBridge.DamageZombiesInRadius((float3)point, data.explosionRadius, damage);

            return;
        }

        if (directZombieHit)
        {
            if (applyDamage && owner != null)
            {
                ZombieDamageBridge.DamageZombie(directZombie, owner.ApplyCrit(damage));
                if (HitMarkerPool.Instance != null)
                    HitMarkerPool.Instance.Spawn(point, false);
            }

            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnZombie(point, normal);
        }
        else
        {
            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnWorld(point, normal);
        }
    }
}