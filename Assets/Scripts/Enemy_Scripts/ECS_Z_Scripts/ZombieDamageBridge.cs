using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public static class ZombieDamageBridge
{
    static World cachedWorld;
    static EntityManager entityManager;
    static Entity gridSingletonEntity = Entity.Null;
    static Entity damageQueueSingletonEntity = Entity.Null;
    static bool initialized;

    static void EnsureInitialized()
    {
        World world = World.DefaultGameObjectInjectionWorld;

        if (world != cachedWorld)
        {
            cachedWorld = world;
            initialized = false;
            gridSingletonEntity = Entity.Null;
            damageQueueSingletonEntity = Entity.Null;
        }

        if (initialized)
            return;

        if (world == null || !world.IsCreated)
            return;

        entityManager = world.EntityManager;

        EntityQuery gridQuery = entityManager.CreateEntityQuery(typeof(ZombieGridSingleton));
        if (gridQuery.CalculateEntityCount() > 0)
            gridSingletonEntity = gridQuery.GetSingletonEntity();

        EntityQuery damageQuery = entityManager.CreateEntityQuery(typeof(ZombieDamageQueue));
        if (damageQuery.CalculateEntityCount() > 0)
            damageQueueSingletonEntity = damageQuery.GetSingletonEntity();

        initialized = gridSingletonEntity != Entity.Null && damageQueueSingletonEntity != Entity.Null;
    }

    public static void DamageZombie(Entity target, int amount, int playerIndex = -1)
    {
        EnsureInitialized();
        if (!initialized)
            return;

        if (entityManager.HasComponent<ZombieSimAuthority>(gridSingletonEntity))
        {
            ZombieSimAuthority authority = entityManager.GetComponentData<ZombieSimAuthority>(gridSingletonEntity);
            if (authority.IsNetworked && !authority.IsServer)
            {
                if (entityManager.HasComponent<ZombieNetId>(target))
                {
                    ushort netId = entityManager.GetComponentData<ZombieNetId>(target).Value;
                    if (netId != 0)
                        ZombieNetworkManager.SendDamageRequest(netId, amount, playerIndex);
                }
                return;
            }
        }

        NativeQueue<ZombieDamageEvent> queue = entityManager.GetComponentData<ZombieDamageQueue>(damageQueueSingletonEntity).Queue;
        queue.Enqueue(new ZombieDamageEvent { Target = target, Amount = amount, PlayerIndex = playerIndex });
    }

    public static int DamageZombiesInRadius(float3 center, float radius, int amount, int playerIndex = -1)
    {
        EnsureInitialized();
        if (!initialized)
            return 0;

        ZombieGridSingleton gridSingleton = entityManager.GetComponentData<ZombieGridSingleton>(gridSingletonEntity);
        float cellSize = gridSingleton.CellSize;
        int3 centerCell = (int3)math.floor(center / cellSize);
        int cellRange = (int)math.ceil(radius / cellSize);
        int yRange = cellRange + 2;

        int hitCount = 0;

        for (int dx = -cellRange; dx <= cellRange; dx++)
        {
            for (int dz = -cellRange; dz <= cellRange; dz++)
            {
                for (int dy = -yRange; dy <= yRange; dy++)
                {
                    int3 neighborCell = centerCell + new int3(dx, dy, dz);
                    if (gridSingleton.Grid.TryGetFirstValue(neighborCell, out ZombieGridEntry entry, out var iterator))
                    {
                        do
                        {
                            float2 horizontalDelta = new float2(center.x - entry.Position.x, center.z - entry.Position.z);
                            float horizontalDist = math.length(horizontalDelta);

                            if (horizontalDist <= radius + entry.Radius)
                            {
                                DamageZombie(entry.Entity, amount, playerIndex);
                                hitCount++;
                            }
                        } while (gridSingleton.Grid.TryGetNextValue(out entry, ref iterator));
                    }
                }
            }
        }

        return hitCount;
    }

    public static bool TryFindNearestZombieAlongRay(float3 origin, float3 direction, float maxDistance, float radius, out Entity result, out float3 hitPosition)
    {
        result = Entity.Null;
        hitPosition = float3.zero;

        EnsureInitialized();
        if (!initialized)
            return false;

        ZombieGridSingleton gridSingleton = entityManager.GetComponentData<ZombieGridSingleton>(gridSingletonEntity);
        float cellSize = gridSingleton.CellSize;
        float3 dir = math.normalize(direction);

        float bestT = maxDistance;
        bool found = false;

        float step = cellSize;
        int steps = (int)math.ceil(maxDistance / step) + 1;
        int3 lastCell = new int3(int.MinValue, int.MinValue, int.MinValue);

        for (int s = 0; s <= steps; s++)
        {
            float t = math.min(s * step, maxDistance);
            float3 samplePos = origin + dir * t;
            int3 sampleCell = (int3)math.floor(samplePos / cellSize);

            if (sampleCell.x != lastCell.x || sampleCell.y != lastCell.y || sampleCell.z != lastCell.z)
            {
                lastCell = sampleCell;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int3 neighborCell = sampleCell + new int3(dx, 0, dz);
                        if (gridSingleton.Grid.TryGetFirstValue(neighborCell, out ZombieGridEntry entry, out var iterator))
                        {
                            do
                            {
                                float3 toEntry = entry.Position - origin;
                                float projT = math.dot(toEntry, dir);
                                if (projT < 0f || projT > maxDistance)
                                    continue;

                                float3 closestPoint = origin + dir * projT;
                                float2 horizontalDelta = new float2(closestPoint.x - entry.Position.x, closestPoint.z - entry.Position.z);
                                float horizontalDist = math.length(horizontalDelta);
                                float feetY = entry.Position.y - entry.GroundOffset;
                                bool withinHeight = closestPoint.y >= feetY - 0.05f && closestPoint.y <= feetY + entry.Height;

                                if (horizontalDist <= entry.Radius + radius && withinHeight && projT < bestT)
                                {
                                    bestT = projT;
                                    result = entry.Entity;
                                    hitPosition = closestPoint;
                                    found = true;
                                }
                            } while (gridSingleton.Grid.TryGetNextValue(out entry, ref iterator));
                        }
                    }
                }
            }

            if (t >= maxDistance)
                break;
        }

        return found;
    }

    public static bool TryFindNearestZombie(float3 worldPosition, float radius, out Entity result)
    {
        result = Entity.Null;

        EnsureInitialized();
        if (!initialized)
            return false;

        ZombieGridSingleton gridSingleton = entityManager.GetComponentData<ZombieGridSingleton>(gridSingletonEntity);
        float cellSize = gridSingleton.CellSize;
        int3 cell = (int3)math.floor(worldPosition / cellSize);

        float closestDist = radius;
        bool found = false;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                int3 neighborCell = cell + new int3(dx, 0, dz);
                if (gridSingleton.Grid.TryGetFirstValue(neighborCell, out ZombieGridEntry entry, out var iterator))
                {
                    do
                    {
                        float2 horizontalDelta = new float2(worldPosition.x - entry.Position.x, worldPosition.z - entry.Position.z);
                        float horizontalDist = math.length(horizontalDelta);
                        float feetY = entry.Position.y - entry.GroundOffset;
                        bool withinHeight = worldPosition.y >= feetY - 0.05f && worldPosition.y <= feetY + entry.Height;

                        if (withinHeight && horizontalDist <= entry.Radius + radius && horizontalDist < closestDist)
                        {
                            closestDist = horizontalDist;
                            result = entry.Entity;
                            found = true;
                        }
                    } while (gridSingleton.Grid.TryGetNextValue(out entry, ref iterator));
                }
            }
        }

        return found;
    }
}