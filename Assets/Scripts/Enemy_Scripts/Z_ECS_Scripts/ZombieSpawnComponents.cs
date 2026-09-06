using Unity.Entities;

// Constant-spawn config. Mirrors the new game's GameObject spawners:
//  - RadiusEnemySpawner  (ring placement + spawnsPerTick + slope check)
//  - EnemyDifficultyHandler (time-based spawn rate + stat scaling)
//  - EnemyPopulationManager (population cap + far-enemy recycling)
// There is no round system anymore - spawning is continuous.
public struct ZombieConstantSpawnConfig : IComponentData
{
    // Population cap (EnemyPopulationManager.maxActiveEnemies)
    public int MaxAlive;

    // Difficulty ramp (EnemyDifficultyHandler)
    public float DifficultyInterval;       // difficultyInterval (seconds per growth step)
    public float BaseSpawnRate;            // baseSpawnRate (spawns/sec)
    public float MaxSpawnRate;             // maxSpawnRate
    public float SpawnRateGrowthAmount;    // spawnRateGrowthAmount (added per whole step)
    public float HealthMultiplierPerInterval;
    public float DamageMultiplierPerInterval;
    public float SpeedMultiplierPerInterval;
    public float MaxStatMultiplier;

    // Ring placement (RadiusEnemySpawner)
    public float MinRadius;
    public float MaxRadius;
    public float RaycastHeight;
    public float MaxSlopeDegrees;
    public int MaxAttemptsPerSpawn;
    public int SpawnsPerTick;

    // Recycling (EnemyPopulationManager)
    public float RecycleDistance;
    public float RecycleRadiusMin;
    public float RecycleRadiusMax;
    public int RecycleChecksPerFrame;
}

// Stress-test override. When Enabled, the spawner ignores the difficulty rate AND
// MaxAlive and just tops the alive count up to TargetCount (SpawnPerFrame at a time),
// so you can slam N zombies on screen and watch the frame cost.
public struct ZombieSpawnDebug : IComponentData
{
    public bool Enabled;
    public int TargetCount;
    public int SpawnPerFrame;
}

public struct ZombieSpawnState : IComponentData
{
    public float Elapsed;      // seconds since the spawner started running (players present)
    public float SpawnTimer;   // accumulator vs. the current spawn interval
    public int RecycleCursor;  // round-robin index into the alive set for recycling
}

// Read-only HUD/telemetry snapshot (replaces ZombieRoundInfo's data).
public struct ZombieSpawnStats : IComponentData
{
    public int Alive;
    public int TotalSpawned;
    public int TotalKilled;
    public float CurrentSpawnRate;
    public float StatMultiplierSteps;
}
