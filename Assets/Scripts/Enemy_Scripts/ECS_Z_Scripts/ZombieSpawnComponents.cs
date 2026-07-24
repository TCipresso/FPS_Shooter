using Unity.Entities;

public struct ZombieSpawnState : IComponentData
{
    public float ElapsedTime;
    public float SpawnAccumulator;
}

public struct ZombieSpawnTuning : IComponentData
{
    public int MaxAlive;
    public float BaseSpawnRate;
    public float RatePerMinute;
    public float MaxSpawnRate;
    public float SpawnRadiusMin;
    public float SpawnRadiusMax;
    public float ClusterRadius;
    public float ClusterRateMultiplier;
    public int SpawnAttemptsPerZombie;

    public float HealthPerMinute;
    public float SpeedPerMinute;
    public float MaxSpeedMultiplier;

    public float DeathDuration;
}
