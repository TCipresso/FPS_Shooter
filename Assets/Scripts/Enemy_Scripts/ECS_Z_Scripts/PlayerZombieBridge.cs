using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public class PlayerZombieBridge : MonoBehaviour
{
    PlayerStats stats;
    EntityManager entityManager;
    Entity singletonEntity;
    bool ready;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {
        if (!ready)
        {
            EntityQuery query = entityManager.CreateEntityQuery(typeof(PlayerPosition));
            if (query.CalculateEntityCount() == 0)
                return;
            singletonEntity = query.GetSingletonEntity();
            ready = true;
        }

        entityManager.SetComponentData(singletonEntity, new PlayerPosition
        {
            Value = transform.position,
            IsValid = true
        });

        NativeQueue<PlayerDamageEvent> queue = entityManager.GetComponentData<PlayerDamageQueue>(singletonEntity).Queue;
        while (queue.TryDequeue(out PlayerDamageEvent damageEvent))
        {
            stats.TakeDamage(damageEvent.Amount);
        }
    }
}
