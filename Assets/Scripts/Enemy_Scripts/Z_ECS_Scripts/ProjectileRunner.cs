using System.Collections.Generic;
using UnityEngine;

// Drives the batched projectile simulation once per frame AND owns all projectile hit VFX
// (impact puffs, explosion effects, hitmarkers) so they can be gated / batched / capped the
// same way WeaponHitscanRunner handles hitscan. Auto-creates itself.
public class ProjectileRunner : MonoBehaviour
{
    public static ProjectileRunner Instance { get; private set; }

    [Header("Hit VFX")]
    [Tooltip("Impact puffs + explosion effects. Off = hits still deal damage + knockback, just no particles.")]
    public bool emitHitVfx = true;
    [Tooltip("Screen-center hitmarker on a projectile hit (once per frame).")]
    public bool showHitMarkers = true;
    [Tooltip("Particle prefab for non-explosive impact puffs. One instance, auto-emission disabled, driven via Emit(). Null = no puffs.")]
    public ParticleSystem impactPrefab;
    [Tooltip("Particles emitted per non-explosive projectile hit.")]
    public int impactParticlesPerHit = 8;
    [Tooltip("Hard cap on impact puffs emitted in one frame.")]
    public int maxImpactsPerFrame = 12;

    ParticleSystem impact;
    ParticleSystem.EmitParams emitParams;

    struct PendingImpact
    {
        public byte Outcome;        // 1 zombie, 2 world
        public Vector3 Point;
        public Vector3 Normal;
        public bool IsCrit;
        public bool BlastHitZombie;
        public bool Explosive;
        public GameObject ExplosionPrefab;
        public float ExplosionDuration;
    }

    static readonly List<PendingImpact> pending = new List<PendingImpact>(32);

    // Called from ProjectileBase.Resolve (works even before the runner GameObject exists).
    public static void EnqueueImpact(byte outcome, Vector3 point, Vector3 normal, bool isCrit,
        bool blastHitZombie, bool explosive, GameObject explosionPrefab, float explosionDuration)
    {
        pending.Add(new PendingImpact
        {
            Outcome = outcome,
            Point = point,
            Normal = normal,
            IsCrit = isCrit,
            BlastHitZombie = blastHitZombie,
            Explosive = explosive,
            ExplosionPrefab = explosionPrefab,
            ExplosionDuration = explosionDuration
        });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => pending.Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ProjectileRunner");
            go.AddComponent<ProjectileRunner>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (impactPrefab != null)
        {
            impact = Instantiate(impactPrefab, transform);
            impact.transform.localPosition = Vector3.zero;
            ParticleSystem.MainModule main = impact.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = impact.emission;
            emission.enabled = false;
            impact.Play();
            impact.Clear();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (ProjectileSimBridge.Count > 0)
            ProjectileSimBridge.Step(Time.deltaTime); // fills `pending` via ProjectileBase.Resolve

        if (pending.Count == 0)
            return;

        int emitted = 0;
        bool anyHit = false;
        bool anyCrit = false;
        Vector3 markerPoint = Vector3.zero;

        for (int i = 0; i < pending.Count; i++)
        {
            PendingImpact p = pending[i];

            // Hitmarker on a direct zombie hit, or an explosion that actually caught a zombie -
            // never on a plain ground/wall impact.
            if (p.Outcome == 1 || p.BlastHitZombie)
            {
                if (!anyHit) { anyHit = true; markerPoint = p.Point; }
                if (p.IsCrit) anyCrit = true;
            }

            if (!emitHitVfx)
                continue;

            if (p.Explosive)
            {
                if (ExplosionPool.Instance != null && p.ExplosionPrefab != null)
                    ExplosionPool.Instance.Spawn(p.ExplosionPrefab, p.Point + p.Normal * 0.3f,
                        Quaternion.FromToRotation(Vector3.up, p.Normal), p.ExplosionDuration);
            }
            else if (impact != null && emitted < maxImpactsPerFrame)
            {
                emitParams.position = p.Point;
                impact.Emit(emitParams, impactParticlesPerHit);
                emitted++;
            }
        }

        pending.Clear();

        if (anyHit && showHitMarkers && HitMarkerPool.Instance != null)
            HitMarkerPool.Instance.Spawn(markerPoint, anyCrit);
    }
}
