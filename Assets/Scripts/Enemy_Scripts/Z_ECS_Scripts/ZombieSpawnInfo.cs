using UnityEngine;
using Unity.Entities;

// Replaces ZombieRoundInfo. Read-only bridge from ECS spawn telemetry to the HUD.
// (There is no round system anymore - this just exposes alive count / difficulty ramp.)
public class ZombieSpawnInfo : MonoBehaviour
{
    public static ZombieSpawnInfo Instance { get; private set; }

    public int AliveCount { get; private set; }
    public int TotalSpawned { get; private set; }
    public int TotalKilled { get; private set; }
    public float CurrentSpawnRate { get; private set; }
    public float DifficultySteps { get; private set; }
    public float ElapsedSeconds { get; private set; }

    EntityManager entityManager;
    Entity statsEntity = Entity.Null;
    Entity stateEntity = Entity.Null;
    bool ready;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    bool EnsureReady()
    {
        if (ready) return true;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return false;

        entityManager = world.EntityManager;

        EntityQuery statsQuery = entityManager.CreateEntityQuery(typeof(ZombieSpawnStats));
        if (statsQuery.CalculateEntityCount() == 0) return false;
        statsEntity = statsQuery.GetSingletonEntity();

        EntityQuery stateQuery = entityManager.CreateEntityQuery(typeof(ZombieSpawnState));
        if (stateQuery.CalculateEntityCount() > 0)
            stateEntity = stateQuery.GetSingletonEntity();

        ready = true;
        return true;
    }

    void Update()
    {
        if (!EnsureReady()) return;

        ZombieSpawnStats stats = entityManager.GetComponentData<ZombieSpawnStats>(statsEntity);
        AliveCount = stats.Alive;
        TotalSpawned = stats.TotalSpawned;
        TotalKilled = stats.TotalKilled;
        CurrentSpawnRate = stats.CurrentSpawnRate;
        DifficultySteps = stats.StatMultiplierSteps;

        if (stateEntity != Entity.Null)
            ElapsedSeconds = entityManager.GetComponentData<ZombieSpawnState>(stateEntity).Elapsed;
    }
}
