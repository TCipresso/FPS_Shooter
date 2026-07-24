using Unity.Entities;

public struct ZombieRoundState : IComponentData
{
    public int Round;
    public int RemainingToSpawn;
    public int TotalThisRound;
    public int KilledThisRound;
    public float SpawnAccumulator;
    public float IntermissionTimer;
    public bool InIntermission;
}

public struct ZombieRoundConfig : IComponentData
{
    public int MaxAlive;
    public int BaseRoundBank;
    public float BankGrowth;
    public float BaseSpawnRate;
    public float SpawnRateGrowth;
    public float MaxSpawnRate;
    public float IntermissionDuration;
    public float SpawnRadiusMin;
    public float SpawnRadiusMax;
    public float ClusterRadius;
    public float ClusterRateMultiplier;
    public int SpawnAttemptsPerZombie;
}
