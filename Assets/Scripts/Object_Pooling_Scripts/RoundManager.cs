using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Spawn Points")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Enemy Types")]
    public List<string> enemyIds = new List<string>();

    [Header("Round Settings")]
    public int baseEnemiesPerRound = 10;
    public int enemiesAddedPerRound = 3;
    public int maxEnemiesPerRound = 40;
    public int roundsPerStage = 4;
    public float spawnInterval = 0.5f;

    [Header("Stage Scaling")]
    public float healthMultiplierPerStage = 1.3f;
    public float damageMultiplierPerStage = 1.2f;
    public float speedMultiplierPerStage = 1.1f;
    public float stageChangeDelay = 5f;

    [Header("Round Progression")]
    [Tooltip("False = enemies scale per stage (default). True = enemies scale per round instead.")]
    public bool scaleByRound = false;

    int currentRound = 0;
    int currentStage = 1;
    int enemiesRemainingAlive = 0;
    int enemiesLeftToSpawn = 0;
    bool roundActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[RoundManager] Awake fired.");
    }

    IEnumerator Start()
    {
        yield return null;
        StartRound();
    }

    void StartRound()
    {
        currentRound++;
        enemiesRemainingAlive = 0;
        enemiesLeftToSpawn = Mathf.Min(baseEnemiesPerRound + enemiesAddedPerRound * (currentRound - 1), maxEnemiesPerRound);
        roundActive = true;

        Debug.Log($"[RoundManager] Stage {currentStage} | Round {currentRound} started. Spawning {enemiesLeftToSpawn} enemies.");

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        Debug.Log($"[RoundManager] SpawnRoutine started. Count: {enemiesLeftToSpawn}");
        Debug.Log($"[RoundManager] SpawnPoints: {spawnPoints.Count} | EnemyIds: {enemyIds.Count}");
        Debug.Log($"[RoundManager] EnemySpawnManager null? {EnemySpawnManager.Instance == null}");

        List<string> shuffled = GetShuffledEnemyList(enemiesLeftToSpawn);

        foreach (string id in shuffled)
        {
            Debug.Log($"[RoundManager] Attempting to spawn: {id}");

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            GameObject enemy = EnemySpawnManager.Instance.SpawnEnemy(id, spawnPoint.position, spawnPoint.rotation);

            Debug.Log($"[RoundManager] Spawn result for {id}: {(enemy == null ? "NULL" : enemy.name)}");

            if (enemy != null)
            {
                ApplyScaling(enemy);

                ZombieBase zombie = enemy.GetComponent<ZombieBase>();
                if (zombie != null)
                    zombie.OnDeath += OnEnemyDied;

                enemiesRemainingAlive++;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    List<string> GetShuffledEnemyList(int count)
    {
        List<string> list = new List<string>();
        for (int i = 0; i < count; i++)
            list.Add(enemyIds[Random.Range(0, enemyIds.Count)]);

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    void ApplyScaling(GameObject enemy)
    {
        ZombieBase zombie = enemy.GetComponent<ZombieBase>();
        if (zombie == null) return;

        int scalingStep = scaleByRound ? (currentRound - 1) : (currentStage - 1);
        if (scalingStep <= 0) return;

        zombie.maxHealth = Mathf.RoundToInt(zombie.maxHealth * Mathf.Pow(healthMultiplierPerStage, scalingStep));
        zombie.currentHealth = zombie.maxHealth;
        zombie.attackDamage = Mathf.RoundToInt(zombie.attackDamage * Mathf.Pow(damageMultiplierPerStage, scalingStep));
        zombie.moveSpeed *= Mathf.Pow(speedMultiplierPerStage, scalingStep);
    }

    void OnEnemyDied()
    {
        enemiesRemainingAlive--;

        if (enemiesRemainingAlive <= 0 && roundActive)
        {
            roundActive = false;
            OnRoundComplete();
        }
    }

    void OnRoundComplete()
    {
        Debug.Log($"[RoundManager] Round {currentRound} complete.");

        if (currentRound % roundsPerStage == 0)
            StartCoroutine(StageChangeRoutine());
        else
            StartRound();
    }

    IEnumerator StageChangeRoutine()
    {
        currentStage++;
        Debug.Log($"[RoundManager] ===== STAGE {currentStage} BEGIN ===== (starting in {stageChangeDelay}s)");
        yield return new WaitForSeconds(stageChangeDelay);
        StartRound();
    }

    public int GetCurrentRound() => currentRound;
    public int GetCurrentStage() => currentStage;
}