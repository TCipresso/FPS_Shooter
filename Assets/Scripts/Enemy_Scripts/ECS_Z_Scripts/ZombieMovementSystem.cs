using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

public partial struct ZombieMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ZombieSingletonTag>(out Entity singletonEntity))
            return;

        DynamicBuffer<PlayerTargetElement> playerBuffer = SystemAPI.GetBuffer<PlayerTargetElement>(singletonEntity);
        ZombieTargetConfig targetConfig = SystemAPI.GetComponent<ZombieTargetConfig>(singletonEntity);

        bool anyPlayerRegistered = false;
        for (int i = 0; i < playerBuffer.Length; i++)
        {
            if (playerBuffer[i].IsRegistered)
            {
                anyPlayerRegistered = true;
                break;
            }
        }
        if (!anyPlayerRegistered)
            return;

        ZombieGridSingleton gridSingleton = SystemAPI.GetComponent<ZombieGridSingleton>(singletonEntity);
        gridSingleton.Grid.Clear();

        int zombieCount = SystemAPI.QueryBuilder().WithAll<ZombieTag, LocalTransform>().Build().CalculateEntityCount();
        if (zombieCount > gridSingleton.Grid.Capacity)
            gridSingleton.Grid.Capacity = zombieCount;

        float cellSize = gridSingleton.CellSize;

        JobHandle buildHandle = new BuildGridJob
        {
            GridWriter = gridSingleton.Grid.AsParallelWriter(),
            CellSize = cellSize
        }.ScheduleParallel(state.Dependency);

        ZombieSimAuthority authority = SystemAPI.GetComponent<ZombieSimAuthority>(singletonEntity);
        if (!authority.ShouldSimulate)
        {
            state.Dependency = buildHandle;
            return;
        }

        int wallLayerMask = 0;
        float wallCheckDistance = 0.6f;
        float wallCheckRadius = 0.4f;
        float climbSpeed = 4f;
        float ledgeLaunchSpeed = 6f;
        float zombieClimbDistance = 2f;
        float maxStackHeight = 8f;
        int groundLayerMask = 0;
        float groundCheckDistance = 15f;
        if (SystemAPI.TryGetSingleton<ZombieWallConfig>(out ZombieWallConfig wallConfig))
        {
            wallLayerMask = wallConfig.WallLayerMask;
            wallCheckDistance = wallConfig.CheckDistance;
            wallCheckRadius = wallConfig.CheckRadius;
            climbSpeed = wallConfig.ClimbSpeed;
            ledgeLaunchSpeed = wallConfig.LedgeLaunchSpeed;
            zombieClimbDistance = wallConfig.ZombieClimbDistance;
            maxStackHeight = wallConfig.MaxStackHeight;
            groundLayerMask = wallConfig.GroundLayerMask;
            groundCheckDistance = wallConfig.GroundCheckDistance;
        }


        NativeArray<RaycastCommand> wallCommands = new NativeArray<RaycastCommand>(zombieCount, Allocator.TempJob);
        NativeArray<RaycastHit> wallResults = new NativeArray<RaycastHit>(zombieCount, Allocator.TempJob);
        NativeArray<RaycastCommand> groundCommands = new NativeArray<RaycastCommand>(zombieCount, Allocator.TempJob);
        NativeArray<RaycastHit> groundResults = new NativeArray<RaycastHit>(zombieCount, Allocator.TempJob);
        NativeArray<float3> desiredMoveDirections = new NativeArray<float3>(zombieCount, Allocator.TempJob);
        NativeArray<bool> zombieBlockedFlags = new NativeArray<bool>(zombieCount, Allocator.TempJob);
        NativeArray<float> zombieStandHeights = new NativeArray<float>(zombieCount, Allocator.TempJob);

        JobHandle desiredMoveHandle = new ZombieDesiredMoveJob
        {
            Players = playerBuffer.AsNativeArray(),
            RecheckInterval = targetConfig.RecheckInterval,
            SwitchDistanceRatioSq = targetConfig.SwitchDistanceRatio * targetConfig.SwitchDistanceRatio,
            DeltaTime = SystemAPI.Time.DeltaTime,
            Grid = gridSingleton.Grid,
            CellSize = cellSize,
            SeparationRadius = 3f,
            SeparationStrength = 2f,
            WallCheckDistance = wallCheckDistance,
            WallCheckRadius = wallCheckRadius,
            WallLayerMask = wallLayerMask,
            ZombieClimbDistance = zombieClimbDistance,
            MaxStackHeight = maxStackHeight,
            GroundLayerMask = groundLayerMask,
            GroundCheckDistance = groundCheckDistance,
            WallCommands = wallCommands,
            GroundCommands = groundCommands,
            DesiredMoveDirections = desiredMoveDirections,
            ZombieBlockedFlags = zombieBlockedFlags,
            ZombieStandHeights = zombieStandHeights
        }.ScheduleParallel(buildHandle);

        JobHandle wallHandle = RaycastCommand.ScheduleBatch(wallCommands, wallResults, 32, desiredMoveHandle);
        JobHandle groundHandle = RaycastCommand.ScheduleBatch(groundCommands, groundResults, 32, desiredMoveHandle);
        JobHandle raycastHandle = JobHandle.CombineDependencies(wallHandle, groundHandle);

        NativeQueue<PlayerDamageEvent>.ParallelWriter playerDamageWriter =
            SystemAPI.GetComponent<PlayerDamageQueue>(singletonEntity).Queue.AsParallelWriter();

        JobHandle applyHandle = new ZombieApplyMovementJob
        {
            DesiredMoveDirections = desiredMoveDirections,
            WallResults = wallResults,
            GroundResults = groundResults,
            ZombieBlockedFlags = zombieBlockedFlags,
            ZombieStandHeights = zombieStandHeights,
            DeltaTime = SystemAPI.Time.DeltaTime,
            PlayerDamageWriter = playerDamageWriter,
            ContactRadius = 1f,
            ContactDamage = 5,
            ContactCooldownDuration = 1f,
            GravityAcceleration = 20f,
            TerminalFallSpeed = -30f,
            ClimbSpeed = climbSpeed,
            LedgeLaunchSpeed = ledgeLaunchSpeed
        }.ScheduleParallel(raycastHandle);

        JobHandle disposeWallCommands = wallCommands.Dispose(applyHandle);
        JobHandle disposeWallResults = wallResults.Dispose(applyHandle);
        JobHandle disposeGroundCommands = groundCommands.Dispose(applyHandle);
        JobHandle disposeGroundResults = groundResults.Dispose(applyHandle);
        JobHandle disposeDirections = desiredMoveDirections.Dispose(applyHandle);
        JobHandle disposeBlockedFlags = zombieBlockedFlags.Dispose(applyHandle);
        JobHandle disposeStandHeights = zombieStandHeights.Dispose(applyHandle);

        JobHandle disposeHandleA = JobHandle.CombineDependencies(disposeWallCommands, disposeWallResults, disposeGroundCommands);
        JobHandle disposeHandleB = JobHandle.CombineDependencies(disposeGroundResults, disposeDirections, disposeBlockedFlags);

        state.Dependency = JobHandle.CombineDependencies(disposeHandleA, disposeHandleB, disposeStandHeights);
    }
}

[BurstCompile]
[WithAll(typeof(ZombieTag))]
[WithNone(typeof(ZombieDead))]
partial struct BuildGridJob : IJobEntity
{
    public NativeParallelMultiHashMap<int3, ZombieGridEntry>.ParallelWriter GridWriter;
    public float CellSize;

    void Execute(Entity entity, in LocalTransform transform, in ZombieHitboxHeight hitboxHeight, in ZombieHitboxRadius hitboxRadius, in ZombieGroundOffset groundOffset)
    {
        int3 cell = (int3)math.floor(transform.Position / CellSize);
        GridWriter.Add(cell, new ZombieGridEntry { Entity = entity, Position = transform.Position, Height = hitboxHeight.Value, Radius = hitboxRadius.Value, GroundOffset = groundOffset.Value });
    }
}

[BurstCompile]
[WithAll(typeof(ZombieTag))]
[WithNone(typeof(ZombieDead))]
partial struct ZombieDesiredMoveJob : IJobEntity
{
    [ReadOnly] public NativeArray<PlayerTargetElement> Players;
    public float RecheckInterval;
    public float SwitchDistanceRatioSq;
    public float DeltaTime;
    [ReadOnly] public NativeParallelMultiHashMap<int3, ZombieGridEntry> Grid;
    public float CellSize;
    public float SeparationRadius;
    public float SeparationStrength;
    public float WallCheckDistance;
    public float WallCheckRadius;
    public int WallLayerMask;
    public float ZombieClimbDistance;
    public float MaxStackHeight;
    public int GroundLayerMask;
    public float GroundCheckDistance;
    [NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> WallCommands;
    [NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> GroundCommands;
    [NativeDisableParallelForRestriction] public NativeArray<float3> DesiredMoveDirections;
    [NativeDisableParallelForRestriction] public NativeArray<bool> ZombieBlockedFlags;
    [NativeDisableParallelForRestriction] public NativeArray<float> ZombieStandHeights;

    const float GroundRayUpOffset = 10f;
    const float GroundLookAhead = 0.5f;
    const float StandFootprintRadius = 0.6f;
    const float ClimbHeightThreshold = 0.3f;
    const float ForwardDotThreshold = 0.95f;
    const float StandTolerance = 0.3f;

    void Execute(Entity entity, [EntityIndexInQuery] int index, in LocalTransform transform, in ZombieHitboxHeight hitboxHeight, in ZombieGroundOffset groundOffset, ref ZombieTarget target)
    {
        float3 position = transform.Position;

        target.RecheckTimer -= DeltaTime;

        bool currentValid = target.HasTarget
            && target.Index >= 0
            && target.Index < Players.Length
            && Players[target.Index].IsTargetable;

        if (!currentValid || target.RecheckTimer <= 0f)
        {
            int bestIndex = -1;
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < Players.Length; i++)
            {
                if (!Players[i].IsTargetable)
                    continue;

                float3 delta = Players[i].Position - position;
                delta.y = 0f;
                float distSq = math.lengthsq(delta);

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }

            if (currentValid && bestIndex != target.Index && bestIndex >= 0)
            {
                float3 currentDelta = Players[target.Index].Position - position;
                currentDelta.y = 0f;
                float currentDistSq = math.lengthsq(currentDelta);

                if (bestDistSq > currentDistSq * SwitchDistanceRatioSq)
                    bestIndex = target.Index;
            }

            target.Index = bestIndex;
            target.HasTarget = bestIndex >= 0;
            target.RecheckTimer = RecheckInterval;
        }

        if (target.HasTarget)
            target.Position = Players[target.Index].Position;

        float3 toPlayer = target.HasTarget ? target.Position - position : float3.zero;
        toPlayer.y = 0f;
        float distToPlayer = math.length(toPlayer);
        float3 chaseDir = distToPlayer > 0.0001f ? toPlayer / distToPlayer : float3.zero;

        float chestHeight = (position.y - groundOffset.Value) + hitboxHeight.Value * 0.5f;

        float3 separation = float3.zero;
        bool zombieBlocked = false;
        float closestBlockerDist = ZombieClimbDistance;
        float zombieStandHeight = float.NegativeInfinity;
        int3 cell = (int3)math.floor(position / CellSize);
        int cellRadius = (int)math.ceil(SeparationRadius / CellSize);
        for (int dx = -cellRadius; dx <= cellRadius; dx++)
        {
            for (int dz = -cellRadius; dz <= cellRadius; dz++)
            {
                int3 neighborCell = cell + new int3(dx, 0, dz);
                if (Grid.TryGetFirstValue(neighborCell, out ZombieGridEntry entry, out var iterator))
                {
                    do
                    {
                        if (entry.Entity != entity)
                        {
                            float3 away = position - entry.Position;
                            away.y = 0f;
                            float dist = math.length(away);
                            if (dist > 0.0001f && dist < SeparationRadius)
                                separation += (away / dist) * (SeparationRadius - dist);

                            float3 towardEntry = entry.Position - position;
                            towardEntry.y = 0f;
                            float distToEntry = math.length(towardEntry);
                            float neighborTop = entry.Position.y - entry.GroundOffset + entry.Height;

                            bool isStandingSupport = distToEntry <= StandFootprintRadius;

                            if (!isStandingSupport && distToEntry > 0.0001f && distToEntry <= closestBlockerDist && neighborTop > chestHeight + ClimbHeightThreshold)
                            {
                                float3 dirToEntry = towardEntry / distToEntry;
                                if (math.dot(dirToEntry, chaseDir) > ForwardDotThreshold)
                                {
                                    zombieBlocked = true;
                                    closestBlockerDist = distToEntry;
                                }
                            }

                            if (isStandingSupport)
                            {
                                float myFeet = position.y - groundOffset.Value;
                                if (neighborTop <= myFeet + StandTolerance && neighborTop > zombieStandHeight)
                                    zombieStandHeight = neighborTop;
                            }
                        }
                    } while (Grid.TryGetNextValue(out entry, ref iterator));
                }
            }
        }

        ZombieBlockedFlags[index] = zombieBlocked;
        ZombieStandHeights[index] = zombieStandHeight;

        float3 moveDir = chaseDir + separation * SeparationStrength;
        float moveLen = math.length(moveDir);
        if (moveLen > 0.0001f)
            moveDir /= moveLen;

        DesiredMoveDirections[index] = moveDir;

        Vector3 wallRayOrigin = new Vector3(position.x, chestHeight, position.z);
        QueryParameters wallQueryParams = new QueryParameters(WallLayerMask, false, QueryTriggerInteraction.Ignore, false);

        Vector3 wallCheckDir = distToPlayer > 0.0001f ? (Vector3)chaseDir : Vector3.forward;
        WallCommands[index] = new RaycastCommand(wallRayOrigin, wallCheckDir, wallQueryParams, WallCheckDistance);

        float3 groundCheckPos = position + chaseDir * GroundLookAhead;
        Vector3 groundRayOrigin = new Vector3(groundCheckPos.x, position.y + GroundRayUpOffset, groundCheckPos.z);
        QueryParameters groundQueryParams = new QueryParameters(GroundLayerMask, false, QueryTriggerInteraction.Ignore, false);
        GroundCommands[index] = new RaycastCommand(groundRayOrigin, Vector3.down, groundQueryParams, GroundRayUpOffset + GroundCheckDistance);
    }
}

[BurstCompile]
[WithAll(typeof(ZombieTag))]
[WithNone(typeof(ZombieDead))]
partial struct ZombieApplyMovementJob : IJobEntity
{
    [ReadOnly] public NativeArray<float3> DesiredMoveDirections;
    [ReadOnly] public NativeArray<RaycastHit> WallResults;
    [ReadOnly] public NativeArray<RaycastHit> GroundResults;
    [ReadOnly] public NativeArray<bool> ZombieBlockedFlags;
    [ReadOnly] public NativeArray<float> ZombieStandHeights;
    public float DeltaTime;
    public NativeQueue<PlayerDamageEvent>.ParallelWriter PlayerDamageWriter;
    public float ContactRadius;
    public int ContactDamage;
    public float ContactCooldownDuration;
    public float GravityAcceleration;
    public float TerminalFallSpeed;
    public float ClimbSpeed;
    public float LedgeLaunchSpeed;

    void Execute([EntityIndexInQuery] int index, ref LocalTransform transform, in ZombieMoveSpeed moveSpeed, ref ZombieContactCooldown cooldown, ref ZombieVerticalVelocity verticalVelocity, ref ZombieClimbState climbState, in ZombieGroundOffset groundOffset, in ZombieTarget target)
    {
        float3 position = transform.Position;
        float3 moveDir = DesiredMoveDirections[index];
        RaycastHit wallHit = WallResults[index];
        RaycastHit groundHit = GroundResults[index];
        bool physicsGround = groundHit.colliderInstanceID != 0;
        float zombieStandHeight = ZombieStandHeights[index];
        bool zombieGround = zombieStandHeight > float.NegativeInfinity;

        bool wallBlockedThisFrame = wallHit.colliderInstanceID != 0;
        bool blocked = wallBlockedThisFrame || ZombieBlockedFlags[index];
        bool hasGround = physicsGround || zombieGround;
        float physicsLandingY = physicsGround ? groundHit.point.y + groundOffset.Value : float.NegativeInfinity;
        float zombieLandingY = zombieGround ? zombieStandHeight + groundOffset.Value : float.NegativeInfinity;
        float combinedLandingY = math.max(physicsLandingY, zombieLandingY);
        bool justClearedWall = climbState.WasWallBlocked && !wallBlockedThisFrame;
        bool justCleared = climbState.WasBlocked && !blocked;
        climbState.WasWallBlocked = wallBlockedThisFrame;
        climbState.WasBlocked = blocked;

        float3 toPlayer = target.HasTarget ? target.Position - position : float3.zero;
        toPlayer.y = 0f;
        float distToPlayer = math.length(toPlayer);
        float3 chaseDir = distToPlayer > 0.0001f ? toPlayer / distToPlayer : float3.zero;

        float3 horizontalMove = blocked ? float3.zero : moveDir * moveSpeed.Value * DeltaTime;
        float newY;

        if (blocked)
        {
            verticalVelocity.Value = ClimbSpeed;
            newY = position.y + verticalVelocity.Value * DeltaTime;
        }
        else
        {
            if (justClearedWall)
                verticalVelocity.Value = LedgeLaunchSpeed;
            else if (justCleared)
                verticalVelocity.Value = 0f;

            if (hasGround)
            {
                float landingY = combinedLandingY;
                if (position.y > landingY || verticalVelocity.Value > 0f)
                {
                    verticalVelocity.Value = math.max(verticalVelocity.Value - GravityAcceleration * DeltaTime, TerminalFallSpeed);
                    newY = position.y + verticalVelocity.Value * DeltaTime;
                    if (newY <= landingY && verticalVelocity.Value <= 0f)
                    {
                        newY = landingY;
                        verticalVelocity.Value = 0f;
                    }
                }
                else
                {
                    newY = landingY;
                    verticalVelocity.Value = 0f;
                }
            }
            else
            {
                verticalVelocity.Value = math.max(verticalVelocity.Value - GravityAcceleration * DeltaTime, TerminalFallSpeed);
                newY = position.y + verticalVelocity.Value * DeltaTime;
            }
        }

        transform.Position = new float3(position.x + horizontalMove.x, newY, position.z + horizontalMove.z);

        if (distToPlayer > 0.0001f)
        {
            quaternion targetRotation = quaternion.LookRotationSafe(chaseDir, math.up());
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, DeltaTime * 10f);
        }

        if (target.HasTarget && distToPlayer < ContactRadius)
        {
            cooldown.Value -= DeltaTime;
            if (cooldown.Value <= 0f)
            {
                PlayerDamageWriter.Enqueue(new PlayerDamageEvent { PlayerIndex = target.Index, Amount = ContactDamage });
                cooldown.Value = ContactCooldownDuration;
            }
        }
        else
        {
            cooldown.Value = math.max(0f, cooldown.Value - DeltaTime);
        }
    }
}