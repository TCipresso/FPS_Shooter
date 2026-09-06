using Unity.Collections;
using UnityEngine;

// Drains every pellet submitted this frame, marches them against the zombie grid in one
// batched Burst job (ZombieHitscanBridge.Flush), and emits all impact VFX in one pass.
// Auto-creates itself; add it to a scene object only if you want to assign the impact
// particle prefab / tune the caps in the inspector.
public class WeaponHitscanRunner : MonoBehaviour
{
    public static WeaponHitscanRunner Instance { get; private set; }

    [Header("Hit VFX")]
    [Tooltip("Impact puffs only (zombie + world). Turn off to profile the hit logic in isolation - hitmarkers are unaffected.")]
    public bool emitHitVfx = true;
    [Tooltip("Screen-center hitmarker + hit sound on a zombie hit (once per frame).")]
    public bool showHitMarkers = true;
    [Tooltip("Your existing zombie-impact particle prefab. One instance is spawned; its auto-emission is disabled and it's driven manually via Emit(). Leave null to skip zombie puffs.")]
    public ParticleSystem impactPrefab;
    [Tooltip("Particles emitted per zombie hit (match what your prefab's burst would spawn).")]
    public int impactParticlesPerHit = 8;
    [Tooltip("Hard cap on hits (zombie + world) that spawn a puff in a single frame.")]
    public int maxImpactsPerFrame = 12;

    ParticleSystem impact;
    ParticleSystem.EmitParams emitParams;
    bool warnedNoImpact;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("WeaponHitscanRunner");
            go.AddComponent<WeaponHitscanRunner>();
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

            // Neutralize whatever emission the prefab has - we drive it entirely via Emit().
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

    void LateUpdate()
    {
        if (ZombieHitscanBridge.PendingCount == 0)
            return;

        NativeArray<PelletHitResult> results = ZombieHitscanBridge.Flush();
        if (!results.IsCreated)
            return;

        bool anyZombieHit = false;
        bool anyCrit = false;
        Vector3 markerPoint = Vector3.zero;
        int impactsEmitted = 0;

        for (int i = 0; i < results.Length; i++)
        {
            PelletHitResult r = results[i];

            if (r.Hit == 1)
            {
                if (!anyZombieHit)
                {
                    anyZombieHit = true;
                    markerPoint = (Vector3)r.Point;
                }
                if (r.IsCrit == 1)
                    anyCrit = true;

                if (emitHitVfx && impact != null && impactsEmitted < maxImpactsPerFrame)
                {
                    emitParams.position = (Vector3)r.Point;
                    impact.Emit(emitParams, impactParticlesPerHit);
                    impactsEmitted++;
                }
            }
            else if (r.WorldHit == 1)
            {
                if (emitHitVfx && ImpactEffectPool.Instance != null && impactsEmitted < maxImpactsPerFrame)
                {
                    ImpactEffectPool.Instance.SpawnWorld((Vector3)r.WorldPoint, (Vector3)r.WorldNormal);
                    impactsEmitted++;
                }
            }
        }

        results.Dispose();

        if (!anyZombieHit)
            return;

        if (emitHitVfx && impact == null && !warnedNoImpact)
        {
            warnedNoImpact = true;
            Debug.LogWarning("[WeaponHitscanRunner] No impactPrefab assigned - zombie hits register but show no puff.");
        }

        // One hitmarker + one hit sound per frame, not per pellet.
        if (showHitMarkers && HitMarkerPool.Instance != null)
            HitMarkerPool.Instance.Spawn(markerPoint, anyCrit);
    }
}
