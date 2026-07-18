using Unity.Entities;
using UnityEngine;

public class ZombieSpawnAuthoring : MonoBehaviour
{
    public GameObject zPrefab;
    public int spawnCount = 1000;
    public float spawnRadius = 20f;

    class Baker : Baker<ZombieSpawnAuthoring>
    {
        public override void Bake(ZombieSpawnAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            Entity prefabEntity = GetEntity(authoring.zPrefab, TransformUsageFlags.Dynamic);
            AddComponent(entity, new ZombieSpawnConfig
            {
                Prefab = prefabEntity,
                SpawnCount = authoring.spawnCount,
                SpawnRadius = authoring.spawnRadius,
                HasSpawned = false
            });
        }
    }
}