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
        public GameObject ragdollPrefab;
        public int poolSize = 20;

        [HideInInspector] public Queue<GameObject> enemyQueue = new Queue<GameObject>();
        [HideInInspector] public Queue<GameObject> ragdollQueue = new Queue<GameObject>();
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

                GameObject ragdoll = Instantiate(pool.ragdollPrefab, Vector3.zero, Quaternion.identity);
                DontDestroyOnLoad(ragdoll);
                ragdoll.SetActive(false);
                pool.ragdollQueue.Enqueue(ragdoll);
            }
        }
    }

    public GameObject SpawnEnemy(string enemyId, Vector3 position, Quaternion rotation)
    {
        EnemyPool pool = enemyPools.Find(p => p.enemyId == enemyId);
        if (pool == null) { Debug.LogWarning($"[EnemySpawnManager] No pool for: {enemyId}"); return null; }
        if (pool.enemyQueue.Count == 0) { Debug.LogWarning($"[EnemySpawnManager] Pool empty: {enemyId}"); return null; }

        GameObject enemy = pool.enemyQueue.Dequeue();
        enemy.transform.position = position;
        enemy.transform.rotation = rotation;

        ZombieBase zombie = enemy.GetComponent<ZombieBase>();
        if (zombie != null) zombie.ClearDeathListeners();

        enemy.SetActive(true);

        if (zombie != null)
        {
            zombie.OnDeath += () => ReturnEnemy(enemyId, enemy);
            zombie.ResetEnemy();
        }

        return enemy;
    }

    public GameObject SpawnRagdoll(string enemyId, Vector3 position, Quaternion rotation, Vector3 hitDirection, float force, string hitBone = "")
    {
        EnemyPool pool = enemyPools.Find(p => p.enemyId == enemyId);
        if (pool == null) return null;
        if (pool.ragdollQueue.Count == 0) return null;

        GameObject ragdoll = pool.ragdollQueue.Dequeue();
        ragdoll.transform.position = position;
        ragdoll.transform.rotation = rotation;
        ragdoll.SetActive(true);

        RagdollCorpse corpse = ragdoll.GetComponent<RagdollCorpse>();
        if (corpse != null)
            corpse.Launch(hitDirection, force, hitBone, () => ReturnRagdoll(enemyId, ragdoll));

        return ragdoll;
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

    void ReturnRagdoll(string enemyId, GameObject ragdoll)
    {
        EnemyPool pool = enemyPools.Find(p => p.enemyId == enemyId);
        if (pool == null) return;

        ragdoll.SetActive(false);
        pool.ragdollQueue.Enqueue(ragdoll);
    }
}