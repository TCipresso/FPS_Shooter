using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

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

        NativeQueue<PlayerDamageEvent>.ParallelWriter playerDamageWriter =
            SystemAPI.GetComponent<PlayerDamageQueue>(singletonEntity).Queue.AsParallelWriter();

        JobHandle movementHandle = new ZombieMovementJob
        {
            PlayerPosition = playerPosition.Value,
            Grid = gridSingleton.Grid,
            CellSize = cellSize,
            DeltaTime = SystemAPI.Time.DeltaTime,
            PlayerDamageWriter = playerDamageWriter,
            ContactRadius = 1f,
            ContactDamage = 5,
            ContactCooldownDuration = 1f
        }.ScheduleParallel(buildHandle);

        state.Dependency = movementHandle;
    }
}

[BurstCompile]
[WithAll(typeof(ZombieTag))]
partial struct BuildGridJob : IJobEntity
{
    public NativeParallelMultiHashMap<int3, ZombieGridEntry>.ParallelWriter GridWriter;
    public float CellSize;

    void Execute(Entity entity, in LocalTransform transform)
    {
        int3 cell = (int3)math.floor(transform.Position / CellSize);
        GridWriter.Add(cell, new ZombieGridEntry { Entity = entity, Position = transform.Position });
    }
}

[BurstCompile]
[WithAll(typeof(ZombieTag))]
partial struct ZombieMovementJob : IJobEntity
{
    [ReadOnly] public NativeParallelMultiHashMap<int3, ZombieGridEntry> Grid;
    public float3 PlayerPosition;
    public float CellSize;
    public float DeltaTime;
    public NativeQueue<PlayerDamageEvent>.ParallelWriter PlayerDamageWriter;
    public float ContactRadius;
    public int ContactDamage;
    public float ContactCooldownDuration;

    void Execute(Entity entity, ref LocalTransform transform, in ZombieMoveSpeed moveSpeed, ref ZombieContactCooldown cooldown)
    {
        float3 position = transform.Position;
        float3 toPlayer = PlayerPosition - position;
        toPlayer.y = 0f;
        float distToPlayer = math.length(toPlayer);
        float3 chaseDir = distToPlayer > 0.0001f ? toPlayer / distToPlayer : float3.zero;

        float3 separation = float3.zero;
        int3 cell = (int3)math.floor(position / CellSize);
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
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
                            if (dist > 0.0001f && dist < CellSize)
                                separation += (away / dist) * (CellSize - dist);
                        }
                    } while (Grid.TryGetNextValue(out entry, ref iterator));
                }
            }
        }

        float3 moveDir = chaseDir + separation * 0.6f;
        float moveLen = math.length(moveDir);
        if (moveLen > 0.0001f)
            moveDir /= moveLen;

        transform.Position = position + moveDir * moveSpeed.Value * DeltaTime;

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
