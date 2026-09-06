using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// One pellet's ray, submitted by WeaponBase during Update. Pure data - no managed refs -
// so the whole batch can be marched in a Burst job.
public struct PelletRayRequest
{
    public float3 Origin;
    public float3 Direction;
    public float MaxDistance;   // clamped to the world (wall) hit so pellets don't shoot through geometry
    public float SwarmRadius;
    public int Damage;          // already crit-scaled on the main thread
    public byte IsCrit;
    public int WeaponId;

    // World (level geometry) hit, resolved on the main thread. Used for the impact puff
    // only when no zombie is in the way.
    public byte HasWorldHit;
    public float3 WorldHitPoint;
    public float3 WorldHitNormal;
}

public struct PelletHitResult
{
    public Entity Zombie;
    public float3 Point;
    public int Damage;
    public byte IsCrit;
    public int WeaponId;
    public byte Hit;            // 1 = hit a zombie

    public byte WorldHit;       // 1 = hit world geometry and NO zombie was closer
    public float3 WorldPoint;
    public float3 WorldNormal;
}

public static class ZombieHitscanBridge
{
    static NativeList<PelletRayRequest> pending;
    static bool created;

    const int HardCap = 4096; // safety net if no WeaponHitscanRunner is present to drain

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        if (created && pending.IsCreated)
            pending.Dispose();
        created = false;
    }

    static void EnsureCreated()
    {
        if (created) return;
        pending = new NativeList<PelletRayRequest>(128, Allocator.Persistent);
        created = true;
    }

    public static int PendingCount => created ? pending.Length : 0;

    public static void Submit(in PelletRayRequest req)
    {
        EnsureCreated();
        if (pending.Length < HardCap)
            pending.Add(req);
    }

    // Called once per frame by WeaponHitscanRunner.LateUpdate. Marches every submitted
    // pellet against the zombie grid in one parallel Burst job, applies damage through the
    // existing deferred queue, and returns the hits for VFX (caller disposes the array).
    public static NativeArray<PelletHitResult> Flush()
    {
        if (!created || pending.Length == 0)
            return default;

        if (!ZombieDamageBridge.TryGetGrid(out NativeParallelMultiHashMap<int3, ZombieGridEntry> grid, out float cellSize))
        {
            pending.Clear();
            return default;
        }

        int count = pending.Length;
        // TempJob; the caller (WeaponHitscanRunner) disposes it this frame.
        var results = new NativeArray<PelletHitResult>(count, Allocator.TempJob);

        // Chain on the grid-build job only (cheap) - not the whole movement sim.
        new PelletMarchJob
        {
            Requests = pending.AsArray(),
            Grid = grid,
            CellSize = cellSize,
            Results = results
        }.Schedule(count, 16, ZombieSimGate.GridBuild).Complete();

        pending.Clear();

        // Fold every pellet that hit the same zombie into ONE damage event. Multiple lethal
        // events for one entity in the same drain = double pool-release = visible jitter.
        for (int i = 0; i < count; i++)
        {
            PelletHitResult r = results[i];
            if (r.Hit == 0) continue;

            bool firstForThisZombie = true;
            for (int k = 0; k < i; k++)
            {
                if (results[k].Hit == 1 && results[k].Zombie == r.Zombie)
                {
                    firstForThisZombie = false;
                    break;
                }
            }
            if (!firstForThisZombie) continue;

            int total = r.Damage;
            for (int j = i + 1; j < count; j++)
            {
                if (results[j].Hit == 1 && results[j].Zombie == r.Zombie)
                    total += results[j].Damage;
            }

            ZombieDamageBridge.DamageZombie(r.Zombie, total, r.WeaponId);
        }

        return results;
    }
}

[BurstCompile]
struct PelletMarchJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<PelletRayRequest> Requests;
    [ReadOnly] public NativeParallelMultiHashMap<int3, ZombieGridEntry> Grid;
    public float CellSize;
    [WriteOnly] public NativeArray<PelletHitResult> Results;

    public void Execute(int index)
    {
        PelletRayRequest req = Requests[index];
        float3 origin = req.Origin;
        float3 dir = math.normalizesafe(req.Direction);
        float maxDistance = req.MaxDistance;
        float radius = req.SwarmRadius;

        float bestT = maxDistance;
        Entity best = Entity.Null;
        float3 bestPoint = float3.zero;
        bool found = false;

        float step = CellSize;
        int steps = (int)math.ceil(maxDistance / step) + 1;
        int2 lastCell = new int2(int.MinValue, int.MinValue);

        for (int s = 0; s <= steps; s++)
        {
            float t = math.min(s * step, maxDistance);
            if (t > bestT) break; // nothing further along the ray can beat the current hit

            float3 samplePos = origin + dir * t;
            int2 sampleCell = new int2((int)math.floor(samplePos.x / CellSize), (int)math.floor(samplePos.z / CellSize));

            if (sampleCell.x != lastCell.x || sampleCell.y != lastCell.y)
            {
                lastCell = sampleCell;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int3 cell = new int3(sampleCell.x + dx, 0, sampleCell.y + dz);
                        if (!Grid.TryGetFirstValue(cell, out ZombieGridEntry entry, out var it))
                            continue;

                        do
                        {
                            float3 toEntry = entry.Position - origin;
                            float projT = math.dot(toEntry, dir);
                            if (projT < 0f || projT >= bestT)
                                continue;

                            float3 cp = origin + dir * projT;
                            float2 hd = new float2(cp.x - entry.Position.x, cp.z - entry.Position.z);
                            if (math.length(hd) > entry.Radius + radius)
                                continue;

                            float feetY = entry.Position.y - entry.GroundOffset;
                            if (cp.y < feetY - 0.05f || cp.y > feetY + entry.Height)
                                continue;

                            bestT = projT;
                            best = entry.Entity;
                            bestPoint = cp;
                            found = true;
                        } while (Grid.TryGetNextValue(out entry, ref it));
                    }
                }
            }

            if (t >= maxDistance)
                break;
        }

        Results[index] = new PelletHitResult
        {
            Zombie = best,
            Point = bestPoint,
            Damage = req.Damage,
            IsCrit = req.IsCrit,
            WeaponId = req.WeaponId,
            Hit = (byte)(found ? 1 : 0),
            // World puff only if a zombie didn't take the hit first.
            WorldHit = (byte)(!found && req.HasWorldHit == 1 ? 1 : 0),
            WorldPoint = req.WorldHitPoint,
            WorldNormal = req.WorldHitNormal
        };
    }
}
