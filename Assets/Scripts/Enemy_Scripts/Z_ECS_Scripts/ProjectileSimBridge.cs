using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct ProjectileState
{
    public float3 Position;
    public float3 Velocity;
    public float GravityScale;
    public float Radius;          // zombie hit radius (weapon swarmHitRadius)
    public float Life;            // remaining seconds
    public float ArmDistance;     // remaining travel before collisions activate (owner grace)
    public int Damage;            // final, crit already applied on the main thread
    public int WeaponId;
    public byte IsCrit;
    public byte Explosive;
    public float ExplosionRadius;
    public int HitMask;           // world raycast layer mask
}

public struct ProjectileStepResult
{
    public byte Outcome;          // 0 alive, 1 hit zombie, 2 hit world, 3 expired
    public byte IsCrit;
    public byte BlastHitZombie;   // explosion caught >= 1 zombie (drives hitmarker on world hits)
    public float3 HitPoint;
    public float3 HitNormal;
}

// Batched projectile simulation. Weapons register a ProjectileState via ProjectileBase.Launch;
// ProjectileRunner steps every projectile each frame in one Burst job (movement + zombie-grid
// collision + world RaycastCommand batch + explosion damage). The pooled GameObject is only a
// visual that follows its state.
public static class ProjectileSimBridge
{
    static NativeList<ProjectileState> states;
    static readonly List<ProjectileBase> managed = new List<ProjectileBase>(64);
    static bool created;

    const int HardCap = 2048;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        if (created && states.IsCreated)
            states.Dispose();
        managed.Clear();
        created = false;
    }

    static void EnsureCreated()
    {
        if (created) return;
        states = new NativeList<ProjectileState>(64, Allocator.Persistent);
        created = true;
    }

    public static int Count => created ? states.Length : 0;

    public static void Register(ProjectileBase pb, in ProjectileState state)
    {
        if (pb == null) return;
        EnsureCreated();
        if (states.Length >= HardCap) return;
        states.Add(state);
        managed.Add(pb);
    }

    public static void Step(float dt)
    {
        if (!created || states.Length == 0 || dt <= 0f)
            return;

        if (!ZombieDamageBridge.TryGetGrid(out NativeParallelMultiHashMap<int3, ZombieGridEntry> grid, out float cellSize) ||
            !ZombieDamageBridge.TryGetDamageQueue(out NativeQueue<ZombieDamageEvent> damageQueue))
        {
            // Sim world not ready - still integrate so projectiles don't hang in the air.
            for (int i = 0; i < states.Length; i++)
            {
                ProjectileState s = states[i];
                s.Position += s.Velocity * dt;
                s.Velocity += new float3(0f, -9.81f * s.GravityScale, 0f) * dt;
                s.Life -= dt;
                states[i] = s;
            }
            Resolve(dt, hadJob: false, default);
            return;
        }

        int n = states.Length;

        NativeArray<RaycastCommand> cmds = new NativeArray<RaycastCommand>(n, Allocator.TempJob);
        for (int i = 0; i < n; i++)
        {
            ProjectileState s = states[i];
            float sp = math.length(s.Velocity);
            float3 d = sp > 1e-4f ? s.Velocity / sp : new float3(0f, 0f, 1f);
            float step = math.max(sp * dt, 0.001f);
            int mask = s.HitMask != 0 ? s.HitMask : ~0;
            cmds[i] = new RaycastCommand(s.Position, d,
                new QueryParameters(mask, false, QueryTriggerInteraction.Ignore, false), step);
        }

        NativeArray<RaycastHit> worldHits = new NativeArray<RaycastHit>(n, Allocator.TempJob);
        JobHandle rcHandle = RaycastCommand.ScheduleBatch(cmds, worldHits, 16);

        NativeArray<ProjectileStepResult> results = new NativeArray<ProjectileStepResult>(n, Allocator.TempJob);

        new ProjectileStepJob
        {
            States = states.AsArray(),
            WorldHits = worldHits,
            Grid = grid,
            CellSize = cellSize,
            DeltaTime = dt,
            Results = results,
            DamageWriter = damageQueue.AsParallelWriter()
        }.Schedule(n, 16, JobHandle.CombineDependencies(rcHandle, ZombieSimGate.GridBuild)).Complete();

        cmds.Dispose();
        worldHits.Dispose();

        Resolve(dt, hadJob: true, results);
        results.Dispose();
    }

    static void Resolve(float dt, bool hadJob, NativeArray<ProjectileStepResult> results)
    {
        // Backwards so RemoveAtSwapBack only ever moves an already-processed element down.
        for (int i = states.Length - 1; i >= 0; i--)
        {
            ProjectileBase pb = managed[i];

            byte outcome;
            float3 point, normal;
            byte crit;
            byte blast;

            if (hadJob)
            {
                ProjectileStepResult r = results[i];
                outcome = r.Outcome;
                point = r.HitPoint;
                normal = r.HitNormal;
                crit = r.IsCrit;
                blast = r.BlastHitZombie;
            }
            else
            {
                outcome = (byte)(states[i].Life <= 0f ? 3 : 0);
                point = states[i].Position;
                normal = new float3(0f, 1f, 0f);
                crit = states[i].IsCrit;
                blast = 0;
            }

            bool remove;
            if (pb == null)
            {
                remove = true; // GameObject destroyed - drop the state
            }
            else if (outcome == 0)
            {
                pb.OnSimStep((Vector3)states[i].Position, (Vector3)states[i].Velocity);
                remove = false;
            }
            else
            {
                pb.Resolve(outcome, (Vector3)point, (Vector3)normal, crit == 1, blast == 1);
                remove = true;
            }

            if (remove)
            {
                states.RemoveAtSwapBack(i);
                managed[i] = managed[managed.Count - 1];
                managed.RemoveAt(managed.Count - 1);
            }
        }
    }
}

[BurstCompile]
struct ProjectileStepJob : IJobParallelFor
{
    public NativeArray<ProjectileState> States;
    [ReadOnly] public NativeArray<RaycastHit> WorldHits;
    [ReadOnly] public NativeParallelMultiHashMap<int3, ZombieGridEntry> Grid;
    public float CellSize;
    public float DeltaTime;
    [WriteOnly] public NativeArray<ProjectileStepResult> Results;
    public NativeQueue<ZombieDamageEvent>.ParallelWriter DamageWriter;

    public void Execute(int index)
    {
        ProjectileState s = States[index];

        float speed = math.length(s.Velocity);
        float3 dir = speed > 1e-4f ? s.Velocity / speed : new float3(0f, 0f, 1f);
        float stepDist = speed * DeltaTime;

        RaycastHit wh = WorldHits[index];
        bool worldHit = wh.colliderInstanceID != 0;
        float worldDist = worldHit ? wh.distance : stepDist;

        bool armed = s.ArmDistance <= 0f;
        float searchDist = math.min(worldDist, stepDist);

        Entity zEnt = Entity.Null;
        float3 zPoint = float3.zero;
        bool zHit = armed && MarchZombie(s.Position, dir, searchDist, s.Radius, out zEnt, out zPoint);

        if (zHit)
        {
            if (s.Explosive == 1) ExplodeDamage(zPoint, s);
            else DamageWriter.Enqueue(new ZombieDamageEvent { Target = zEnt, Amount = s.Damage, PlayerIndex = 0, WeaponTicket = s.WeaponId });

            Results[index] = new ProjectileStepResult { Outcome = 1, IsCrit = s.IsCrit, BlastHitZombie = 1, HitPoint = zPoint, HitNormal = -dir };
            return;
        }

        if (armed && worldHit)
        {
            float3 wp = wh.point;
            float3 wn = wh.normal;
            if (wh.distance <= 0f) { wp = s.Position; wn = -dir; }
            byte blast = 0;
            if (s.Explosive == 1) blast = (byte)(ExplodeDamage(wp, s) > 0 ? 1 : 0);

            Results[index] = new ProjectileStepResult { Outcome = 2, IsCrit = s.IsCrit, BlastHitZombie = blast, HitPoint = wp, HitNormal = wn };
            return;
        }

        s.Position += s.Velocity * DeltaTime;
        s.Velocity += new float3(0f, -9.81f * s.GravityScale, 0f) * DeltaTime;
        s.Life -= DeltaTime;
        s.ArmDistance = math.max(0f, s.ArmDistance - stepDist);
        States[index] = s;

        Results[index] = s.Life <= 0f
            ? new ProjectileStepResult { Outcome = 3, IsCrit = s.IsCrit, HitPoint = s.Position, HitNormal = new float3(0f, 1f, 0f) }
            : new ProjectileStepResult { Outcome = 0 };
    }

    bool MarchZombie(float3 origin, float3 dir, float maxDistance, float radius, out Entity result, out float3 point)
    {
        result = Entity.Null;
        point = float3.zero;

        float bestT = maxDistance;
        bool found = false;

        float step = CellSize;
        int steps = (int)math.ceil(maxDistance / step) + 1;
        int2 lastCell = new int2(int.MinValue, int.MinValue);

        for (int sIdx = 0; sIdx <= steps; sIdx++)
        {
            float t = math.min(sIdx * step, maxDistance);
            if (t > bestT) break;

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
                        if (!Grid.TryGetFirstValue(cell, out ZombieGridEntry e, out var it))
                            continue;
                        do
                        {
                            float3 toEntry = e.Position - origin;
                            float projT = math.dot(toEntry, dir);
                            if (projT < 0f || projT >= bestT)
                                continue;

                            float3 cp = origin + dir * projT;
                            float2 hd = new float2(cp.x - e.Position.x, cp.z - e.Position.z);
                            if (math.length(hd) > e.Radius + radius)
                                continue;

                            float feetY = e.Position.y - e.GroundOffset;
                            if (cp.y < feetY - 0.05f || cp.y > feetY + e.Height)
                                continue;

                            bestT = projT;
                            result = e.Entity;
                            point = cp;
                            found = true;
                        } while (Grid.TryGetNextValue(out e, ref it));
                    }
                }
            }

            if (t >= maxDistance) break;
        }

        return found;
    }

    int ExplodeDamage(float3 center, in ProjectileState s)
    {
        int3 c = new int3((int)math.floor(center.x / CellSize), 0, (int)math.floor(center.z / CellSize));
        int range = (int)math.ceil(s.ExplosionRadius / CellSize);
        int caught = 0;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dz = -range; dz <= range; dz++)
            {
                int3 cell = new int3(c.x + dx, 0, c.z + dz);
                if (!Grid.TryGetFirstValue(cell, out ZombieGridEntry e, out var it))
                    continue;
                do
                {
                    float2 hd = new float2(center.x - e.Position.x, center.z - e.Position.z);
                    if (math.length(hd) <= s.ExplosionRadius + e.Radius)
                    {
                        DamageWriter.Enqueue(new ZombieDamageEvent { Target = e.Entity, Amount = s.Damage, PlayerIndex = 0, WeaponTicket = s.WeaponId });
                        caught++;
                    }
                } while (Grid.TryGetNextValue(out e, ref it));
            }
        }

        return caught;
    }
}
