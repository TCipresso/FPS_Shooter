using UnityEngine;
using System.Collections.Generic;
public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }
    [System.Serializable]
    public class EnemyPool
    {
        public string enemyId;
        public GameObject enemyPrefab;
        public int poolSize = 20;
        [HideInInspector] public Queue<GameObject> enemyQueue = new Queue<GameObject>();
    }
    [Header("Enemy Pools")]
    public List<EnemyPool> enemyPools = new List<EnemyPool>();
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePools();
    }
    void InitializePools()
    {
        foreach (EnemyPool pool in enemyPools)
        {
            for (int i = 0; i < pool.poolSize; i++)
            {
                GameObject enemy = Instantiate(pool.enemyPrefab, Vector3.zero, Quaternion.identity);
                DontDestroyOnLoad(enemy);
                enemy.SetActive(false);
                pool.enemyQueue.Enqueue(enemy);
            }
        }
    }
    GameObject SpawnEnemyInternal(string enemyId, Vector3 position, Quaternion rotation)
    {
        EnemyPool pool = enemyPools.Find(p => p.enemyId == enemyId);
        if (pool == null) { Debug.LogWarning($"[EnemySpawnManager] No pool for: {enemyId}"); return null; }
        if (pool.enemyQueue.Count == 0) { Debug.LogWarning($"[EnemySpawnManager] Pool empty: {enemyId}"); return null; }
        GameObject enemy = pool.enemyQueue.Dequeue();
        Rigidbody erb = enemy.GetComponent<Rigidbody>();
        if (erb != null)
            erb.isKinematic = true;
        enemy.transform.position = position;
        enemy.transform.rotation = rotation;
        ZombieBase zombie = enemy.GetComponent<ZombieBase>();
        if (zombie != null) zombie.ClearDeathListeners();
        enemy.SetActive(true);
        return enemy;
    }
    public GameObject SpawnEnemy(string enemyId, Transform spawnPoint)
    {
        GameObject enemy = SpawnEnemyInternal(enemyId, spawnPoint.position, spawnPoint.rotation);
        if (enemy == null) return null;

        ZombieBase zombie = enemy.GetComponent<ZombieBase>();
        if (zombie != null)
        {
            SpawnPointPath path = spawnPoint.GetComponent<SpawnPointPath>();
            zombie.SetPath(path != null && path.waypoints.Count > 0 ? path.waypoints : null);
            zombie.OnDeath += () => ReturnEnemy(enemyId, enemy);
            zombie.ResetEnemy();
        }
        return enemy;
    }
    public GameObject SpawnEnemy(string enemyId, Vector3 position, Quaternion rotation)
    {
        GameObject enemy = SpawnEnemyInternal(enemyId, position, rotation);
        if (enemy == null) return null;

        ZombieBase zombie = enemy.GetComponent<ZombieBase>();
        if (zombie != null)
        {
            zombie.SetPath(null);
            zombie.OnDeath += () => ReturnEnemy(enemyId, enemy);
            zombie.ResetEnemy();
        }
        return enemy;
    }
    public void DebugSpawnNearPlayer(string enemyId, int count, float radius = 5f)
    {
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player == null) { Debug.LogWarning("[EnemySpawnManager] No player found."); return; }
        for (int i = 0; i < count; i++)
        {
            Vector2 rand = Random.insideUnitCircle.normalized * radius;
            Vector3 spawnPos = player.transform.position + new Vector3(rand.x, 0f, rand.y);
            SpawnEnemy(enemyId, spawnPos, Quaternion.identity);
        }
    }
    void ReturnEnemy(string enemyId, GameObject enemy)
    {
        EnemyPool pool = enemyPools.Find(p => p.enemyId == enemyId);
        if (pool == null) return;
        enemy.SetActive(false);
        pool.enemyQueue.Enqueue(enemy);
    }
}