using UnityEngine;
using System.Collections.Generic;

public class RadiusEnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableEnemy
    {
        public string enemyId;
        [Min(0f)]
        public float weight = 1f;
    }

    [Header("Who To Spawn")]
    public List<SpawnableEnemy> enemies = new List<SpawnableEnemy>();

    [Header("Where (ring around player)")]
    public float minRadius = 15f;
    public float maxRadius = 30f;
    public LayerMask groundLayerMask;
    public float raycastHeight = 200f;

    [Header("Spawn Batching")]
    public int spawnsPerTick = 1;

    [Header("Placement Rules")]
    [Range(0f, 90f)]
    public float maxSlopeDegrees = 40f;
    public int maxAttemptsPerSpawn = 5;

    Transform player;
    float timer;

    void Update()
    {
        if (player == null)
        {
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null) player = stats.transform;
            return;
        }

        if (EnemyDifficultyHandler.Instance == null)
            return;

        timer += Time.deltaTime;
        if (timer < EnemyDifficultyHandler.Instance.GetSpawnInterval())
            return;
        timer = 0f;

        for (int i = 0; i < spawnsPerTick; i++)
            TrySpawnOne();
    }

    void TrySpawnOne()
    {
        if (EnemyPopulationManager.Instance != null && !EnemyPopulationManager.Instance.HasRoomForMoreEnemies())
            return;

        string enemyId = PickEnemyIdWithRoom();
        if (enemyId == null)
            return;

        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float dist = Random.Range(minRadius, maxRadius);
            float x = player.position.x + Mathf.Cos(angle) * dist;
            float z = player.position.z + Mathf.Sin(angle) * dist;

            Vector3 rayStart = new Vector3(x, player.position.y + raycastHeight, z);
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayerMask))
                continue;

            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeDegrees)
                continue;

            GameObject enemy = EnemySpawnManager.Instance.SpawnEnemy(enemyId, hit.point, Quaternion.identity);
            if (enemy != null)
            {
                if (EnemyDifficultyHandler.Instance != null)
                    EnemyDifficultyHandler.Instance.ApplyScaling(enemy);
                if (EnemyPopulationManager.Instance != null)
                    EnemyPopulationManager.Instance.RegisterSpawn(enemyId, enemy);
            }
            return;
        }
    }

    string PickEnemyIdWithRoom()
    {
        float totalWeight = 0f;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].weight > 0f && PoolHasRoom(enemies[i].enemyId))
                totalWeight += enemies[i].weight;
        }
        if (totalWeight <= 0f)
            return null;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].weight <= 0f || !PoolHasRoom(enemies[i].enemyId))
                continue;
            cumulative += enemies[i].weight;
            if (roll <= cumulative)
                return enemies[i].enemyId;
        }
        return null;
    }

    bool PoolHasRoom(string enemyId)
    {
        if (EnemySpawnManager.Instance == null)
            return false;

        foreach (EnemySpawnManager.EnemyPool pool in EnemySpawnManager.Instance.enemyPools)
        {
            if (pool.enemyId == enemyId)
                return pool.enemyQueue.Count > 0;
        }
        return false;
    }
}