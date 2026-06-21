using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
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
    public int enemiesAddedPerStage = 3;
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

    [Header("NavMesh")]
    public NavMeshSurface navMeshSurface;

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

        if (GridPrefabSpawner.Instance != null)
            yield return StartCoroutine(GridPrefabSpawner.Instance.TransitionToRandomPattern());

        yield return null;

        RefreshSpawnPoints();

        if (navMeshSurface != null && GridPrefabSpawner.Instance != null)
        {
            GridPattern pattern = GridPrefabSpawner.Instance.GetLastLoadedPattern();
            if (pattern != null && pattern.navMeshData != null)
            {
                navMeshSurface.RemoveData();
                navMeshSurface.navMeshData = pattern.navMeshData;
                navMeshSurface.AddData();
            }
        }

        StartRound();
    }

    void RefreshSpawnPoints()
    {
        spawnPoints.Clear();
        foreach (SpawnPoint sp in FindObjectsOfType<SpawnPoint>())
            spawnPoints.Add(sp.transform);

        Debug.Log($"[RoundManager] Found {spawnPoints.Count} spawn points.");

        if (spawnPoints.Count == 0)
            Debug.LogError("[RoundManager] Zero spawn points found.");
    }

    void StartRound()
    {
        currentRound++;
        enemiesRemainingAlive = 0;
        enemiesLeftToSpawn = Mathf.Min(baseEnemiesPerRound + enemiesAddedPerStage * (currentStage - 1), maxEnemiesPerRound);
        roundActive = true;

        Debug.Log($"[RoundManager] Stage {currentStage} | Round {currentRound} started. Spawning {enemiesLeftToSpawn} enemies.");

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("[RoundManager] No spawn points found! Tag your SpawnPoint prefabs as 'SpawnPoint'.");
            yield break;
        }

        Debug.Log($"[RoundManager] SpawnRoutine started. Count: {enemiesLeftToSpawn}");
        Debug.Log($"[RoundManager] SpawnPoints: {spawnPoints.Count} | EnemyIds: {enemyIds.Count}");
        Debug.Log($"[RoundManager] EnemySpawnManager null? {EnemySpawnManager.Instance == null}");

        List<string> shuffled = GetShuffledEnemyList(enemiesLeftToSpawn);

        foreach (string id in shuffled)
        {
            Debug.Log($"[RoundManager] Attempting to spawn: {id}");

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            GameObject enemy = EnemySpawnManager.Instance.SpawnEnemy(id, spawnPoint.position, spawnPoint.rotation);

            AudioSource spawnAudio = spawnPoint.GetComponent<AudioSource>();
            if (spawnAudio != null)
            {
                spawnAudio.pitch = Random.Range(0.85f, 1.1f);
                spawnAudio.Play();
            }

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
        Debug.Log($"[RoundManager] ===== STAGE {currentStage} BEGIN ===== Transitioning map...");

        if (GridPrefabSpawner.Instance != null)
            yield return StartCoroutine(GridPrefabSpawner.Instance.TransitionToRandomPattern());

        yield return null;

        RefreshSpawnPoints();

        if (navMeshSurface != null && GridPrefabSpawner.Instance != null)
        {
            GridPattern pattern = GridPrefabSpawner.Instance.GetLastLoadedPattern();
            if (pattern != null && pattern.navMeshData != null)
            {
                navMeshSurface.RemoveData();
                navMeshSurface.navMeshData = pattern.navMeshData;
                navMeshSurface.AddData();
                Debug.Log($"[RoundManager] NavMesh swapped to: {pattern.name}");
            }
            else
            {
                Debug.LogWarning($"[RoundManager] Pattern has no NavMeshData assigned — skipping swap.");
            }
        }
        else
        {
            Debug.LogWarning("[RoundManager] No NavMeshSurface assigned — skipping NavMesh swap.");
        }

        Debug.Log($"[RoundManager] Map ready. Stage {currentStage} starting in {stageChangeDelay}s.");
        yield return new WaitForSeconds(stageChangeDelay);
        StartRound();
    }

    public int GetCurrentRound() => currentRound;
    public int GetCurrentStage() => currentStage;
}