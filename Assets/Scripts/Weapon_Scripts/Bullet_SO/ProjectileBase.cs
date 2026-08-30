using UnityEngine;
using System.Collections.Generic;

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
    WeaponDefinitionSO data;
    static readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    static readonly RaycastHit[] zombieHitBuffer = new RaycastHit[16];
    static readonly Collider[] explosionOverlapBuffer = new Collider[32];
    static readonly HashSet<ZombieBase> explosionHitSet = new HashSet<ZombieBase>();
    const float castRadius = 0.15f;
    public static bool debugLogging = false;
    int tickCount;
    TrailRenderer trail;
    bool trailCached;

    public void Launch(Vector3 origin, Vector3 direction, float speed, float gravityScale,
        float life, int damage, bool applyDamage, WeaponBase owner, WeaponDefinitionSO data, float radius)
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
        bool hitZombie = TryFindNearestZombieAlongRay(position, dir, searchDist, out ZombieBase zombie, out HitBox hitBox, out Vector3 zombieHitPos);

        if (hitZombie)
        {
            DoImpact(zombieHitPos, -dir, true, zombie, hitBox);
            OnDespawn();
            return true;
        }

        if (didHitWorld)
        {
            DoImpact(worldHit.point, worldHit.normal, false, null, null, worldHit.collider);
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

    bool TryFindNearestZombieAlongRay(Vector3 origin, Vector3 direction, float maxDistance, out ZombieBase zombie, out HitBox hitBox, out Vector3 hitPoint)
    {
        int hitCount = Physics.SphereCastNonAlloc(origin, radius, direction, zombieHitBuffer, maxDistance);
        ZombieBase closestZombie = null;
        HitBox closestHitBox = null;
        float closestDistance = float.MaxValue;
        Vector3 closestPoint = default;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = zombieHitBuffer[i];
            HitBox candidateHitBox = hit.collider.GetComponentInParent<HitBox>();
            ZombieBase candidate = candidateHitBox != null
                ? candidateHitBox.zombie
                : hit.collider.GetComponentInParent<ZombieBase>();

            if (candidate == null || candidate.IsDead) continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestZombie = candidate;
                closestHitBox = candidateHitBox;
                closestPoint = hit.point;
            }
        }

        zombie = closestZombie;
        hitBox = closestHitBox;
        hitPoint = closestPoint;
        return closestZombie != null;
    }

    void DamageZombiesInRadius(Vector3 center, float explosionRadius, int amount)
    {
        int count = Physics.OverlapSphereNonAlloc(center, explosionRadius, explosionOverlapBuffer);
        explosionHitSet.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider col = explosionOverlapBuffer[i];
            if (col == null) continue;

            ZombieBase zombie = col.GetComponentInParent<ZombieBase>();
            if (zombie == null || zombie.IsDead) continue;

            if (!explosionHitSet.Add(zombie)) continue;

            Vector3 toZombie = zombie.transform.position - center;
            Vector3 hitDir = toZombie.sqrMagnitude > 0.0001f ? toZombie.normalized : Vector3.up;

            // Apply crit and pass weapon reference for XP tracking
            int finalDamage = owner != null ? owner.ApplyCrit(amount) : amount;
            zombie.TakeDamage(finalDamage, owner != null ? owner.OwnerStats : null, 1f, hitDir, 1f, "", owner);
            zombie.hitFlash?.Flash(false);
        }
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

    void DoImpact(Vector3 point, Vector3 normal, bool directZombieHit, ZombieBase directZombie, HitBox directHitBox = null, Collider worldCollider = null)
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
                DamageZombiesInRadius(point, data.explosionRadius, damage);
                ApplySelfKnockback(point);
            }
            return;
        }

        if (directZombieHit)
        {
            if (applyDamage && owner != null)
            {
                // Apply crit chance
                int finalDamage = owner.ApplyCrit(damage);

                if (directHitBox != null)
                {
                    // Pass weapon reference for XP tracking
                    directHitBox.TakeDamageWithHitPoint(finalDamage, owner.OwnerStats, owner, point, 1f, normal, 1f);
                }
                else
                {
                    // Pass weapon reference for XP tracking
                    directZombie.TakeDamage(finalDamage, owner.OwnerStats, 1f, normal, 1f, "", owner);
                    directZombie.hitFlash?.Flash(false);
                    if (HitMarkerPool.Instance != null)
                        HitMarkerPool.Instance.Spawn(point, false);
                }
            }

            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnZombie(point, normal);
        }
        else
        {
            if (ImpactEffectPool.Instance != null)
                ImpactEffectPool.Instance.SpawnWorld(point, normal);

            SandboxSpawner spawner = worldCollider != null ? worldCollider.GetComponentInParent<SandboxSpawner>() : null;
            if (spawner != null)
                spawner.TriggerSpawn();
        }
    }
}