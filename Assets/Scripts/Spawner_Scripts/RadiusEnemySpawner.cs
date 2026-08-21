using UnityEngine;
using System.Collections.Generic;

// Spawns enemies from your EnemySpawnManager pool in a ring around the
// player, picking from a list of enemy types instead of just one. Two
// things keep this cheap:
//
// 1. It only checks anything on a timer (spawnInterval), not every frame.
// 2. Before doing any raycasting, it checks which enemy types still have
//    room in their pool (pool.enemyQueue.Count > 0) and only picks among
//    those. If every pool is already maxed out, it skips the whole spawn
//    attempt instantly - no raycasts, no work. Each pool's size is
//    naturally the cap on how many of that enemy can exist at once.
//
// Height correctness works the same way the props do: raycast straight
// down from above the candidate point onto the ground layer and use the
// hit point. Since the terrain is a heightmap (one Y per X,Z, no overhangs
// or caves), that always finds the real surface - never spawns under a hill.
public class RadiusEnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableEnemy
    {
        public string enemyId;   // must match an enemyId in EnemySpawnManager's Enemy Pools list
        [Min(0f)]
        public float weight = 1f; // relative chance vs other entries in this list - doesn't need to sum to anything
    }

    [Header("Who To Spawn")]
    public List<SpawnableEnemy> enemies = new List<SpawnableEnemy>();

    [Header("Where (ring around player)")]
    public float minRadius = 15f;   // don't spawn closer than this - keeps them out of the player's face
    public float maxRadius = 30f;   // don't spawn farther than this - keeps them relevant
    public LayerMask groundLayerMask; // set this to your Ground layer (the same one HillMesh uses)
    public float raycastHeight = 200f; // start point above ground for the downward raycast

    [Header("Pacing")]
    public float spawnInterval = 2f; // seconds between spawn attempts - this is the main frame-cost lever
    public int spawnsPerTick = 1;    // how many to try each interval

    [Header("Placement Rules")]
    [Range(0f, 90f)]
    public float maxSlopeDegrees = 40f; // skip spots too steep to stand on
    public int maxAttemptsPerSpawn = 5; // retries if a ring point misses the ground or is too steep

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

        timer += Time.deltaTime;
        if (timer < spawnInterval)
            return;
        timer = 0f;

        for (int i = 0; i < spawnsPerTick; i++)
            TrySpawnOne();
    }

    void TrySpawnOne()
    {
        string enemyId = PickEnemyIdWithRoom();
        if (enemyId == null)
            return; // every pool is already full of active enemies - nothing to do

        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float dist = Random.Range(minRadius, maxRadius);
            float x = player.position.x + Mathf.Cos(angle) * dist;
            float z = player.position.z + Mathf.Sin(angle) * dist;

            Vector3 rayStart = new Vector3(x, player.position.y + raycastHeight, z);
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayerMask))
                continue; // missed the ground entirely (shouldn't happen on this terrain, but cheap to guard)

            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeDegrees)
                continue; // too steep here, try another ring point

            EnemySpawnManager.Instance.SpawnEnemy(enemyId, hit.point, Quaternion.identity);
            return;
        }
    }

    // Weighted-random pick, but only among enemy types whose pool currently
    // has a free instance. Returns null if nothing is spawnable right now.
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