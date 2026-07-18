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
        if (!SystemAPI.TryGetSingletonEntity<PlayerPosition>(out Entity singletonEntity))
            return;

        PlayerPosition playerPosition = SystemAPI.GetComponent<PlayerPosition>(singletonEntity);
        if (!playerPosition.IsValid)
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

        int wallLayerMask = 0;
        float wallCheckDistance = 0.6f;
        float climbSpeed = 4f;
        int groundLayerMask = 0;
        float groundCheckDistance = 15f;
        if (SystemAPI.TryGetSingleton<ZombieWallConfig>(out ZombieWallConfig wallConfig))
        {
            wallLayerMask = wallConfig.WallLayerMask;
            wallCheckDistance = wallConfig.CheckDistance;
            climbSpeed = wallConfig.ClimbSpeed;
            groundLayerMask = wallConfig.GroundLayerMask;
            groundCheckDistance = wallConfig.GroundCheckDistance;
        }

        NativeArray<RaycastCommand> wallCommands = new NativeArray<RaycastCommand>(zombieCount, Allocator.TempJob);
        NativeArray<RaycastHit> wallResults = new NativeArray<RaycastHit>(zombieCount, Allocator.TempJob);
        NativeArray<RaycastCommand> groundCommands = new NativeArray<RaycastCommand>(zombieCount, Allocator.TempJob);
        NativeArray<RaycastHit> groundResults = new NativeArray<RaycastHit>(zombieCount, Allocator.TempJob);
        NativeArray<float3> desiredMoveDirections = new NativeArray<float3>(zombieCount, Allocator.TempJob);

        JobHandle desiredMoveHandle = new ZombieDesiredMoveJob
        {
            PlayerPosition = playerPosition.Value,
            Grid = gridSingleton.Grid,
            CellSize = cellSize,
            SeparationRadius = 1.5f,
            SeparationStrength = 2f,
            WallCheckDistance = wallCheckDistance,
            WallLayerMask = wallLayerMask,
            GroundLayerMask = groundLayerMask,
            GroundCheckDistance = groundCheckDistance,
            WallCommands = wallCommands,
            GroundCommands = groundCommands,
            DesiredMoveDirections = desiredMoveDirections
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
            PlayerPosition = playerPosition.Value,
            DeltaTime = SystemAPI.Time.DeltaTime,
            PlayerDamageWriter = playerDamageWriter,
            ContactRadius = 1f,
            ContactDamage = 5,
            ContactCooldownDuration = 1f,
            GravityAcceleration = 20f,
            TerminalFallSpeed = -30f,
            ClimbSpeed = climbSpeed
        }.ScheduleParallel(raycastHandle);

        JobHandle disposeWallCommands = wallCommands.Dispose(applyHandle);
        JobHandle disposeWallResults = wallResults.Dispose(applyHandle);
        JobHandle disposeGroundCommands = groundCommands.Dispose(applyHandle);
        JobHandle disposeGroundResults = groundResults.Dispose(applyHandle);
        JobHandle disposeDirections = desiredMoveDirections.Dispose(applyHandle);

        JobHandle disposeHandleA = JobHandle.CombineDependencies(disposeWallCommands, disposeWallResults, disposeGroundCommands);
        JobHandle disposeHandleB = JobHandle.CombineDependencies(disposeGroundResults, disposeDirections);

        state.Dependency = JobHandle.CombineDependencies(disposeHandleA, disposeHandleB);
    }
}

[BurstCompile]
[WithAll(typeof(ZombieTag))]
partial struct BuildGridJob : IJobEntity
{
    public NativeParallelMultiHashMap<int3, ZombieGridEntry>.ParallelWriter GridWriter;
    public float CellSize;

    void Execute(Entity entity, in LocalTransform transform, in ZombieHitboxHeight hitboxHeight, in ZombieGroundOffset groundOffset)
    {
        int3 cell = (int3)math.floor(transform.Position / CellSize);
        GridWriter.Add(cell, new ZombieGridEntry { Entity = entity, Position = transform.Position, Height = hitboxHeight.Value, GroundOffset = groundOffset.Value });
    }
}

[BurstCompile]
[WithAll(typeof(ZombieTag))]
partial struct ZombieDesiredMoveJob : IJobEntity
{
    public float3 PlayerPosition;
    [ReadOnly] public NativeParallelMultiHashMap<int3, ZombieGridEntry> Grid;
    public float CellSize;
    public float SeparationRadius;
    public float SeparationStrength;
    public float WallCheckDistance;
    public int WallLayerMask;
    public int GroundLayerMask;
    public float GroundCheckDistance;
    [NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> WallCommands;
    [NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> GroundCommands;
    [NativeDisableParallelForRestriction] public NativeArray<float3> DesiredMoveDirections;

    const float GroundRayUpOffset = 3f;

    void Execute(Entity entity, [EntityIndexInQuery] int index, in LocalTransform transform, in ZombieHitboxHeight hitboxHeight, in ZombieGroundOffset groundOffset)
    {
        float3 position = transform.Position;
        float3 toPlayer = PlayerPosition - position;
        toPlayer.y = 0f;
        float distToPlayer = math.length(toPlayer);
        float3 chaseDir = distToPlayer > 0.0001f ? toPlayer / distToPlayer : float3.zero;

        float3 separation = float3.zero;
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
                        }
                    } while (Grid.TryGetNextValue(out entry, ref iterator));
                }
            }
        }

        float3 moveDir = chaseDir + separation * SeparationStrength;
        float moveLen = math.length(moveDir);
        if (moveLen > 0.0001f)
            moveDir /= moveLen;

        DesiredMoveDirections[index] = moveDir;

        float chestHeight = (position.y - groundOffset.Value) + hitboxHeight.Value * 0.5f;
        Vector3 wallRayOrigin = new Vector3(position.x, chestHeight, position.z);
        QueryParameters wallQueryParams = new QueryParameters(WallLayerMask, false, QueryTriggerInteraction.Ignore, false);

        if (moveLen > 0.0001f)
            WallCommands[index] = new RaycastCommand(wallRayOrigin, (Vector3)moveDir, wallQueryParams, WallCheckDistance);
        else
            WallCommands[index] = new RaycastCommand(wallRayOrigin, Vector3.forward, wallQueryParams, 0f);

        Vector3 groundRayOrigin = new Vector3(position.x, position.y + GroundRayUpOffset, position.z);
        QueryParameters groundQueryParams = new QueryParameters(GroundLayerMask, false, QueryTriggerInteraction.Ignore, false);
        GroundCommands[index] = new RaycastCommand(groundRayOrigin, Vector3.down, groundQueryParams, GroundRayUpOffset + GroundCheckDistance);
    }
}

[BurstCompile]
[WithAll(typeof(ZombieTag))]
partial struct ZombieApplyMovementJob : IJobEntity
{
    [ReadOnly] public NativeArray<float3> DesiredMoveDirections;
    [ReadOnly] public NativeArray<RaycastHit> WallResults;
    [ReadOnly] public NativeArray<RaycastHit> GroundResults;
    public float3 PlayerPosition;
    public float DeltaTime;
    public NativeQueue<PlayerDamageEvent>.ParallelWriter PlayerDamageWriter;
    public float ContactRadius;
    public int ContactDamage;
    public float ContactCooldownDuration;
    public float GravityAcceleration;
    public float TerminalFallSpeed;
    public float ClimbSpeed;

    void Execute([EntityIndexInQuery] int index, ref LocalTransform transform, in ZombieMoveSpeed moveSpeed, ref ZombieContactCooldown cooldown, ref ZombieVerticalVelocity verticalVelocity, in ZombieGroundOffset groundOffset)
    {
        float3 position = transform.Position;
        float3 moveDir = DesiredMoveDirections[index];
        RaycastHit wallHit = WallResults[index];
        RaycastHit groundHit = GroundResults[index];
        bool blocked = wallHit.colliderInstanceID != 0;
        bool hasGround = groundHit.colliderInstanceID != 0;

        float3 toPlayer = PlayerPosition - position;
        toPlayer.y = 0f;
        float distToPlayer = math.length(toPlayer);
        float3 chaseDir = distToPlayer > 0.0001f ? toPlayer / distToPlayer : float3.zero;

        float3 horizontalMove = moveDir * moveSpeed.Value * DeltaTime;
        float newY;

        if (blocked)
        {
            verticalVelocity.Value = ClimbSpeed;
            newY = position.y + verticalVelocity.Value * DeltaTime;
        }
        else if (hasGround)
        {
            float landingY = groundHit.point.y + groundOffset.Value;
            if (position.y > landingY)
            {
                verticalVelocity.Value = math.max(verticalVelocity.Value - GravityAcceleration * DeltaTime, TerminalFallSpeed);
                newY = position.y + verticalVelocity.Value * DeltaTime;
                if (newY <= landingY)
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

        transform.Position = new float3(position.x + horizontalMove.x, newY, position.z + horizontalMove.z);

        if (distToPlayer > 0.0001f)
        {
            quaternion targetRotation = quaternion.LookRotationSafe(chaseDir, math.up());
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, DeltaTime * 10f);
        }

        if (distToPlayer < ContactRadius)
        {
            cooldown.Value -= DeltaTime;
            if (cooldown.Value <= 0f)
            {
                PlayerDamageWriter.Enqueue(new PlayerDamageEvent { Amount = ContactDamage });
                cooldown.Value = ContactCooldownDuration;
            }
        }
        else
        {
            cooldown.Value = math.max(0f, cooldown.Value - DeltaTime);
        }
    }
}