using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public class ZombieNetworkManager : MonoBehaviour
{
    [Header("Send Settings")]
    public float sendInterval = 0.05f;
    public float cullRadius = 120f;

    static ZombieNetworkManager _instance;
    public static ZombieNetworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ZombieNetworkManager");
                _instance = go.AddComponent<ZombieNetworkManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        _ = Instance;
    }

    EntityManager entityManager;
    Entity singletonEntity = Entity.Null;
    bool ready;

    byte[] sendBuffer;
    readonly List<ushort> despawnScratch = new List<ushort>();

    struct PendingFreeId
    {
        public ushort NetId;
        public float ReleaseTime;
    }

    readonly List<PendingFreeId> pendingFreeIds = new List<PendingFreeId>();
    const float IdRecycleDelay = 3f;

    bool handlersRegistered;
    bool wasServer;
    bool wasClient;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        sendBuffer = new byte[ZombieNetConfig.MaxZombiesPerMessage * ZombieNetConfig.BytesPerZombie];
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

        ZombieSnapshotState snapshotState = entityManager.GetComponentData<ZombieSnapshotState>(singletonEntity);
        snapshotState.Interval = sendInterval;
        entityManager.SetComponentData(singletonEntity, snapshotState);

        ready = true;
        return true;
    }

    void Update()
    {
        if (!EnsureReady()) return;

        UpdateAuthority();
        RegisterHandlers();

        if (NetworkServer.active)
        {
            SendSnapshots();
            SendDeaths();
            SendDespawns();
            RecycleExpiredIds();
        }
    }

    void RecycleExpiredIds()
    {
        if (pendingFreeIds.Count == 0)
            return;

        NativeQueue<ushort> freeIds = entityManager.GetComponentData<ZombieFreeNetIds>(singletonEntity).Queue;
        float now = Time.time;

        for (int i = pendingFreeIds.Count - 1; i >= 0; i--)
        {
            if (pendingFreeIds[i].ReleaseTime > now)
                continue;

            freeIds.Enqueue(pendingFreeIds[i].NetId);
            pendingFreeIds.RemoveAt(i);
        }
    }

    void UpdateAuthority()
    {
        bool isServer = NetworkServer.active;
        bool isClient = NetworkClient.active;

        entityManager.SetComponentData(singletonEntity, new ZombieSimAuthority
        {
            IsNetworked = isServer || isClient,
            IsServer = isServer
        });
    }

    void RegisterHandlers()
    {
        bool isServer = NetworkServer.active;
        bool isClientOnly = NetworkClient.active && !isServer;

        if (isServer && !wasServer)
            NetworkServer.RegisterHandler<ZombieDamageRequestMessage>(OnServerDamageRequest);

        if (isClientOnly && !wasClient)
        {
            NetworkClient.RegisterHandler<ZombieSnapshotMessage>(OnClientSnapshot);
            NetworkClient.RegisterHandler<ZombieDespawnMessage>(OnClientDespawn);
            NetworkClient.RegisterHandler<ZombieDeathMessage>(OnClientDeath);
        }

        wasServer = isServer;
        wasClient = isClientOnly;
        handlersRegistered = true;
    }

    void SendSnapshots()
    {
        ZombieSnapshotState snapshotState = entityManager.GetComponentData<ZombieSnapshotState>(singletonEntity);
        if (!snapshotState.HasNewSnapshot)
            return;

        snapshotState.HasNewSnapshot = false;
        entityManager.SetComponentData(singletonEntity, snapshotState);

        NativeList<ZombieSnapshotEntry> entries = entityManager.GetComponentData<ZombieSnapshotBuffer>(singletonEntity).Entries;
        if (entries.Length == 0)
            return;

        float cullRadiusSq = cullRadius * cullRadius;

        foreach (KeyValuePair<int, NetworkConnectionToClient> pair in NetworkServer.connections)
        {
            NetworkConnectionToClient conn = pair.Value;
            if (conn == null) continue;
            if (conn == NetworkServer.localConnection) continue;
            if (!conn.isReady) continue;

            float3 viewer = float3.zero;
            bool hasViewer = false;
            if (conn.identity != null)
            {
                viewer = (float3)conn.identity.transform.position;
                hasViewer = true;
            }

            int packed = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                ZombieSnapshotEntry entry = entries[i];

                if (hasViewer)
                {
                    float3 delta = entry.Position - viewer;
                    delta.y = 0f;
                    if (math.lengthsq(delta) > cullRadiusSq)
                        continue;
                }

                ZombieNetConfig.Pack(sendBuffer, packed * ZombieNetConfig.BytesPerZombie, entry.NetId, entry.Position, entry.Yaw);
                packed++;

                if (packed == ZombieNetConfig.MaxZombiesPerMessage)
                {
                    FlushSnapshot(conn, packed);
                    packed = 0;
                }
            }

            if (packed > 0)
                FlushSnapshot(conn, packed);
        }
    }

    void FlushSnapshot(NetworkConnectionToClient conn, int count)
    {
        int byteCount = count * ZombieNetConfig.BytesPerZombie;
        byte[] payload = new byte[byteCount];
        System.Array.Copy(sendBuffer, payload, byteCount);

        conn.Send(new ZombieSnapshotMessage
        {
            Data = payload,
            Count = (ushort)count
        }, Channels.Unreliable);
    }

    readonly List<ushort> deathScratch = new List<ushort>();

    void SendDeaths()
    {
        NativeQueue<ushort> deaths = entityManager.GetComponentData<ZombieServerDeathQueue>(singletonEntity).Queue;
        if (deaths.IsEmpty())
            return;

        deathScratch.Clear();
        while (deaths.TryDequeue(out ushort netId))
            deathScratch.Add(netId);

        if (deathScratch.Count == 0)
            return;

        ZombieDeathMessage message = new ZombieDeathMessage
        {
            NetIds = deathScratch.ToArray()
        };

        foreach (KeyValuePair<int, NetworkConnectionToClient> pair in NetworkServer.connections)
        {
            NetworkConnectionToClient conn = pair.Value;
            if (conn == null) continue;
            if (conn == NetworkServer.localConnection) continue;
            if (!conn.isReady) continue;

            conn.Send(message, Channels.Reliable);
        }
    }

    void SendDespawns()
    {
        NativeQueue<ushort> despawns = entityManager.GetComponentData<ZombieServerDespawnQueue>(singletonEntity).Queue;
        if (despawns.IsEmpty())
            return;

        despawnScratch.Clear();
        while (despawns.TryDequeue(out ushort netId))
        {
            despawnScratch.Add(netId);
            pendingFreeIds.Add(new PendingFreeId
            {
                NetId = netId,
                ReleaseTime = Time.time + IdRecycleDelay
            });
        }

        if (despawnScratch.Count == 0)
            return;

        ZombieDespawnMessage message = new ZombieDespawnMessage
        {
            NetIds = despawnScratch.ToArray()
        };

        foreach (KeyValuePair<int, NetworkConnectionToClient> pair in NetworkServer.connections)
        {
            NetworkConnectionToClient conn = pair.Value;
            if (conn == null) continue;
            if (conn == NetworkServer.localConnection) continue;
            if (!conn.isReady) continue;

            conn.Send(message, Channels.Reliable);
        }
    }

    void OnClientSnapshot(ZombieSnapshotMessage message)
    {
        if (!EnsureReady()) return;
        if (message.Data == null) return;

        NativeQueue<ZombieSyncEntry> queue = entityManager.GetComponentData<ZombieSyncQueue>(singletonEntity).Queue;

        for (int i = 0; i < message.Count; i++)
        {
            int offset = i * ZombieNetConfig.BytesPerZombie;
            if (offset + ZombieNetConfig.BytesPerZombie > message.Data.Length)
                break;

            ZombieNetConfig.Unpack(message.Data, offset, out ushort netId, out float3 position, out float yaw);

            queue.Enqueue(new ZombieSyncEntry
            {
                NetId = netId,
                Position = position,
                Yaw = yaw
            });
        }
    }

    void OnClientDespawn(ZombieDespawnMessage message)
    {
        if (!EnsureReady()) return;
        if (message.NetIds == null) return;

        NativeQueue<ushort> queue = entityManager.GetComponentData<ZombieClientDespawnQueue>(singletonEntity).Queue;

        for (int i = 0; i < message.NetIds.Length; i++)
            queue.Enqueue(message.NetIds[i]);
    }

    void OnClientDeath(ZombieDeathMessage message)
    {
        if (!EnsureReady()) return;
        if (message.NetIds == null) return;

        NativeQueue<ushort> queue = entityManager.GetComponentData<ZombieClientDeathQueue>(singletonEntity).Queue;

        for (int i = 0; i < message.NetIds.Length; i++)
            queue.Enqueue(message.NetIds[i]);
    }

    void OnServerDamageRequest(NetworkConnectionToClient conn, ZombieDamageRequestMessage message)
    {
        if (!EnsureReady()) return;

        NativeQueue<ZombieDamageRequest> queue = entityManager.GetComponentData<ZombieDamageRequestQueue>(singletonEntity).Queue;
        queue.Enqueue(new ZombieDamageRequest
        {
            NetId = message.NetId,
            Amount = message.Amount
        });
    }

    public static void SendDamageRequest(ushort netId, int amount)
    {
        if (!NetworkClient.active) return;

        NetworkClient.Send(new ZombieDamageRequestMessage
        {
            NetId = netId,
            Amount = amount
        }, Channels.Reliable);
    }
}