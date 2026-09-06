using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public class ZombiePlayerRegistry : MonoBehaviour
{
    // Singleplayer. Kept as a list/registry so the ECS targeting buffer stays unchanged.
    public const int MaxPlayers = 1;

    static ZombiePlayerRegistry _instance;
    public static ZombiePlayerRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ZombiePlayerRegistry");
                _instance = go.AddComponent<ZombiePlayerRegistry>();
            }
            return _instance;
        }
    }

    readonly List<PlayerZombieBridge> players = new List<PlayerZombieBridge>();

    EntityManager entityManager;
    Entity singletonEntity = Entity.Null;
    bool ready;

    public void Register(PlayerZombieBridge bridge)
    {
        if (bridge == null) return;
        if (players.Contains(bridge)) return;
        if (players.Count >= MaxPlayers)
        {
            Debug.LogWarning($"[ZombiePlayerRegistry] Player limit ({MaxPlayers}) reached, ignoring {bridge.name}");
            return;
        }
        players.Add(bridge);
    }

    public void Unregister(PlayerZombieBridge bridge)
    {
        players.Remove(bridge);
    }

    public int GetIndex(PlayerZombieBridge bridge)
    {
        return players.IndexOf(bridge);
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
        ready = true;
        return true;
    }

    void Update()
    {
        if (!EnsureReady()) return;

        WritePlayerPositions();
        DrainDamage();
        DrainCredits();
    }

    void WritePlayerPositions()
    {
        DynamicBuffer<PlayerTargetElement> buffer = entityManager.GetBuffer<PlayerTargetElement>(singletonEntity);

        for (int i = 0; i < buffer.Length; i++)
        {
            if (i < players.Count && players[i] != null)
            {
                PlayerZombieBridge bridge = players[i];
                buffer[i] = new PlayerTargetElement
                {
                    Position = (float3)bridge.transform.position,
                    IsRegistered = true,
                    IsTargetable = bridge.IsTargetable
                };
            }
            else
            {
                buffer[i] = new PlayerTargetElement
                {
                    Position = float3.zero,
                    IsRegistered = false,
                    IsTargetable = false
                };
            }
        }
    }

    void DrainDamage()
    {
        NativeQueue<PlayerDamageEvent> queue = entityManager.GetComponentData<PlayerDamageQueue>(singletonEntity).Queue;

        while (queue.TryDequeue(out PlayerDamageEvent damageEvent))
        {
            int i = damageEvent.PlayerIndex;
            if (i < 0 || i >= players.Count) continue;

            PlayerZombieBridge bridge = players[i];
            if (bridge == null) continue;

            bridge.ApplyContactDamage(damageEvent.Amount);
        }
    }

    void DrainCredits()
    {
        NativeQueue<ZombieCreditEvent> queue = entityManager.GetComponentData<ZombieCreditQueue>(singletonEntity).Queue;

        while (queue.TryDequeue(out ZombieCreditEvent creditEvent))
        {
            // Always resolve the ticket (even for PlayerIndex < 0 cleanup events) so the
            // managed dictionary in ZombieDamageBridge doesn't leak.
            WeaponBase weapon = ZombieDamageBridge.ConsumeWeaponTicket(creditEvent.WeaponTicket);

            int i = creditEvent.PlayerIndex;
            if (i < 0 || i >= players.Count) continue;

            PlayerZombieBridge bridge = players[i];
            if (bridge == null) continue;

            if (creditEvent.IsKill)
            {
                bridge.GrantKillGold(creditEvent.Position);
                if (weapon != null && creditEvent.XpAmount > 0f)
                    weapon.GrantKillXp(creditEvent.XpAmount);
            }
            else
            {
                bridge.GrantHitGold();
            }
        }
    }
}