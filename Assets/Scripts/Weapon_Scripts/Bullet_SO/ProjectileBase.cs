using Unity.Mathematics;
using UnityEngine;

// Visual + context holder for a pooled projectile. All movement and collision run in the
// batched Burst sim (ProjectileSimBridge); this class just follows its state and handles
// the one-shot impact / explosion feedback when it finishes.
public class ProjectileBase : MonoBehaviour
{
    [HideInInspector] public GameObject pool;

    WeaponBase owner;
    WeaponDefinitionSO data;
    PlayerFpsController ownerController;
    bool firedAirborne;
    bool applyDamage;

    TrailRenderer trail;
    bool trailCached;

    public void Launch(Vector3 origin, Vector3 direction, float speed, float gravityScale,
        float life, int damage, bool applyDamage, WeaponBase owner, WeaponDefinitionSO data, float radius)
    {
        this.owner = owner;
        this.data = data;
        this.applyDamage = applyDamage;
        this.ownerController = owner != null ? owner.GetComponentInParent<PlayerFpsController>() : null;
        this.firedAirborne = ownerController != null && !ownerController.IsGrounded;

        int finalDamage = applyDamage && owner != null ? owner.ApplyCrit(damage) : damage;
        byte isCrit = (byte)(finalDamage != damage ? 1 : 0);

        Vector3 vel = direction.sqrMagnitude > 1e-6f ? direction.normalized * speed : Vector3.forward * speed;
        transform.position = origin;
        if (vel.sqrMagnitude > 1e-4f)
            transform.rotation = Quaternion.LookRotation(vel);

        if (!trailCached)
        {
            trail = GetComponentInChildren<TrailRenderer>();
            trailCached = true;
        }
        if (trail != null)
            trail.Clear();

        int mask = (data != null && data.hitMask.value != 0) ? data.hitMask.value : ~0;

        ProjectileSimBridge.Register(this, new ProjectileState
        {
            Position = origin,
            Velocity = vel,
            GravityScale = gravityScale,
            Radius = radius,
            Life = life,
            ArmDistance = 1.5f,
            Damage = finalDamage,
            WeaponId = owner != null ? owner.WeaponId : 0,
            IsCrit = isCrit,
            Explosive = (byte)(data != null && data.isExplosive ? 1 : 0),
            ExplosionRadius = data != null ? data.explosionRadius : 0f,
            HitMask = mask
        });
    }

    // Called by ProjectileSimBridge each frame for projectiles still in flight.
    public void OnSimStep(Vector3 position, Vector3 velocity)
    {
        transform.position = position;
        if (velocity.sqrMagnitude > 1e-4f)
            transform.rotation = Quaternion.LookRotation(velocity);
    }

    // Called once when the projectile finishes. Damage was already applied by the sim job;
    // this does gameplay feedback (knockback) + hands VFX to ProjectileRunner + returns to
    // the pool. outcome: 1 zombie, 2 world, 3 expired.
    public void Resolve(byte outcome, Vector3 point, Vector3 normal, bool isCrit, bool blastHitZombie)
    {
        transform.position = point;

        if (outcome != 3)
        {
            bool explosive = data != null && data.isExplosive;

            if (explosive && applyDamage)
                ApplySelfKnockback(point); // gameplay - always

            ProjectileRunner.EnqueueImpact(outcome, point, normal, isCrit, blastHitZombie, explosive,
                explosive ? data.explosionEffectPrefab : null,
                explosive && data != null ? data.explosionEffectDuration : 0f);
        }

        if (trail != null)
            trail.Clear();

        if (ProjectilePool.Instance != null)
            ProjectilePool.Instance.Return(this);
        else
            gameObject.SetActive(false);
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
        Vector3 horizontal = new Vector3(pushDir.x, 0f, pushDir.z);
        if (horizontal.sqrMagnitude < 0.01f)
            horizontal = new Vector3(ownerController.transform.forward.x, 0f, ownerController.transform.forward.z).normalized;
        else
            horizontal.Normalize();

        pushDir = (horizontal + Vector3.up * data.explosionKnockbackUpBias).normalized;
        ownerController.ApplyImpulse(pushDir * data.explosionSelfKnockback);
    }
}
