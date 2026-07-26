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
    Transform ownerRoot;
    PlayerFpsController ownerController;
    bool firedAirborne;
    BulletDataSO data;

    static readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    const float castRadius = 0.15f;

    public static bool debugLogging = false;
    int tickCount;

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
        this.ownerRoot = owner != null ? owner.transform.root : null;
        this.ownerController = owner != null ? owner.GetComponentInParent<PlayerFpsController>() : null;
        this.firedAirborne = ownerController != null && !ownerController.IsGrounded;
        this.data = data;
        this.radius = radius;
        this.hitMask = data != null && data.hitMask != 0 ? data.hitMask : ~0;
        this.tickCount = 0;

        if (debugLogging)
            Debug.Log($"[PROJ] LAUNCH origin={origin} dir={direction.normalized} speed={speed} mask={(int)this.hitMask} life={life}");

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

        int hitCount = Physics.SphereCastNonAlloc(position, castRadius, dir, hitBuffer, stepDist, hitMask);

        if (debugLogging && tickCount < 10)
        {
            Debug.Log($"[PROJ] tick={tickCount} pos={position} dir={dir} step={stepDist:F3} hits={hitCount}");
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = hitBuffer[i];
                string colName = h.collider != null ? h.collider.name : "NULL";
                int colLayer = h.collider != null ? h.collider.gameObject.layer : -1;
                bool isOwner = ownerRoot != null && h.collider != null && h.transform.root == ownerRoot;
                Debug.Log($"[PROJ]   hit[{i}] col={colName} layer={colLayer} dist={h.distance:F3} point={h.point} owner={isOwner}");
            }
        }
        tickCount++;

        bool didHitWorld = false;
        RaycastHit worldHit = default;
        float closestDist = stepDist;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (h.collider == null) continue;
            if (ownerRoot != null && h.transform.root == ownerRoot) continue;
            if (h.distance < closestDist)
            {
                closestDist = h.distance;
                worldHit = h;
                didHitWorld = true;
            }
        }

        if (didHitWorld && worldHit.distance <= 0f)
        {
            worldHit.point = position;
            worldHit.normal = -dir;
        }

        float searchDist = didHitWorld ? worldHit.distance : stepDist;

        Entity zombie = Entity.Null;
        float3 zombieHitPos = float3.zero;
        bool hitZombie = ZombieDamageBridge.TryFindNearestZombieAlongRay(
            (float3)position, (float3)dir, searchDist, radius,
            out zombie, out zombieHitPos);

        if (hitZombie)
        {
            DoImpact((Vector3)zombieHitPos, -dir, true, zombie);
            OnDespawn();
            return true;
        }

        if (didHitWorld)
        {
            DoImpact(worldHit.point, worldHit.normal, false, Entity.Null);
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

    void ApplySelfKnockback(Vector3 explosionPoint)
    {
        if (ownerController == null || data == null) return;
        if (data.explosionSelfKnockback <= 0f) return;
        if (ownerController.IsGrounded && !firedAirborne) return;

        Vector3 playerPos = ownerController.transform.position;
        Vector3 toPlayer = playerPos - explosionPoint;
        float dist = toPlayer.magnitude;
        if (dist > data.explosionRadius) return;

        Vector3 pushDir = dist > 0.05f ? toPlayer / dist : Vector3.up;

        Vector3 moveDir = ownerController.transform.forward;
        Vector3 horizontal = new Vector3(pushDir.x, 0f, pushDir.z);
        if (horizontal.sqrMagnitude < 0.01f)
            horizontal = new Vector3(moveDir.x, 0f, moveDir.z).normalized;
        else
            horizontal.Normalize();

        pushDir = (horizontal + Vector3.up * data.explosionKnockbackUpBias).normalized;

        ownerController.ApplyImpulse(pushDir * data.explosionSelfKnockback);
    }

    void DoImpact(Vector3 point, Vector3 normal, bool directZombieHit, Entity directZombie)
    {
        if (debugLogging)
            Debug.Log($"[PROJ] IMPACT point={point} zombieHit={directZombieHit} explosive={data != null && data.isExplosive} vfx={(data != null && data.explosionEffectPrefab != null)} pool={(ExplosionPool.Instance != null)} applyDamage={applyDamage}");

        if (data != null && data.isExplosive)
        {
            if (ExplosionPool.Instance != null && data.explosionEffectPrefab != null)
                ExplosionPool.Instance.Spawn(data.explosionEffectPrefab, point + normal * 0.3f,
                    Quaternion.FromToRotation(Vector3.up, normal), data.explosionEffectDuration);

            if (applyDamage)
            {
                ZombieDamageBridge.DamageZombiesInRadius((float3)point, data.explosionRadius, damage);
                ApplySelfKnockback(point);
            }

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