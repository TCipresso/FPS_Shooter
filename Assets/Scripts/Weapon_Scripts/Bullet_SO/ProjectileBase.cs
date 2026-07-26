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
            Vector3 end = (Vector3)zombieHitPos;

            if (applyDamage && owner != null)
            {
                ZombieDamageBridge.DamageZombie(zombie, owner.ApplyCrit(damage));
                if (HitMarkerPool.Instance != null)
                    HitMarkerPool.Instance.Spawn(end, false);
            }

            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnZombie(end, -dir);

            return true;
        }

        if (didHitWorld)
        {
            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnWorld(hit.point, hit.normal);

            return true;
        }

        position += velocity * dt;
        velocity += Vector3.down * (9.81f * gravityScale) * dt;

        transform.position = position;
        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity);

        life -= dt;
        return life <= 0f;
    }
}
