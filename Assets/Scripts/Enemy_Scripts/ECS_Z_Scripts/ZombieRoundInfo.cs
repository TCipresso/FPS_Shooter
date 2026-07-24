using UnityEngine;
using Unity.Entities;

public class ZombieRoundInfo : MonoBehaviour
{
    public static ZombieRoundInfo Instance { get; private set; }

    public int Round { get; private set; }
    public int TotalThisRound { get; private set; }
    public int KilledThisRound { get; private set; }
    public int RemainingToSpawn { get; private set; }
    public bool InIntermission { get; private set; }
    public float IntermissionTimer { get; private set; }
    public int AliveCount { get; private set; }

    EntityManager entityManager;
    Entity singletonEntity = Entity.Null;
    EntityQuery aliveQuery;
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

        EntityQuery query = entityManager.CreateEntityQuery(typeof(ZombieSingletonTag));
        if (query.CalculateEntityCount() == 0) return false;

        singletonEntity = query.GetSingletonEntity();
        aliveQuery = entityManager.CreateEntityQuery(typeof(ZombieTag));
        ready = true;
        return true;
    }

    void Update()
    {
        if (!EnsureReady()) return;
        if (!entityManager.HasComponent<ZombieRoundState>(singletonEntity)) return;

        ZombieRoundState round = entityManager.GetComponentData<ZombieRoundState>(singletonEntity);

        Round = round.Round;
        TotalThisRound = round.TotalThisRound;
        KilledThisRound = round.KilledThisRound;
        RemainingToSpawn = round.RemainingToSpawn;
        InIntermission = round.InIntermission;
        IntermissionTimer = round.IntermissionTimer;
        AliveCount = aliveQuery.CalculateEntityCount();
    }
}
