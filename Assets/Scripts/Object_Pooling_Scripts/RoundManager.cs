using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Enemy Types")]
    public List<string> enemyIds = new List<string>();

    [Header("Round Settings")]
    public int baseRoundBank = 10;
    public int bankAddedPerRound = 3;
    public int bankAddedPerStage = 10;
    public int maxRoundBank = 150;
    public int maxAliveAtOnce = 12;
    public int roundsPerStage = 4;
    public float spawnInterval = 0.5f;

    [Header("Spawn Point Safety")]
    public LayerMask enemyLayerMask;
    public float spawnPointCheckRadius = 0.6f;

    [Header("Stage Scenes")]
    [Tooltip("Scene names to load in order. Loops when exhausted.")]
    public List<string> stageScenes = new List<string>();
    public float stageChangeDelay = 5f;

    [Header("Stage Scaling")]
    public float healthMultiplierPerStage = 1.3f;
    public float damageMultiplierPerStage = 1.2f;
    public float speedMultiplierPerStage = 1.1f;

    [Header("Round Progression")]
    public bool scaleByRound = false;

    [Header("UI")]
    public TextMeshProUGUI roundText;

    int currentRound = 0;
    int currentStage = 1;
    int enemiesCurrentlyAlive = 0;
    int enemiesRemainingInBank = 0;
    bool roundActive = false;
    int lastSceneIndex = -1;

    List<Transform> currentSpawnPoints = new List<Transform>();
    bool waitingForPlayer = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        yield return null;
        if (currentRound == 0 && !waitingForPlayer)
        {
            StageSetup setup = FindFirstObjectByType<StageSetup>();
            if (setup != null)
                currentSpawnPoints = setup.spawnPoints;
            waitingForPlayer = true;
            StartCoroutine(WaitForPlayerThenStart());
        }
    }

    public void OnSceneReady(List<Transform> spawnPoints)
    {
        currentSpawnPoints = spawnPoints;
        if (waitingForPlayer) return;
        waitingForPlayer = true;
        StartCoroutine(WaitForPlayerThenStart());
    }

    IEnumerator WaitForPlayerThenStart()
    {
        while (FindFirstObjectByType<PlayerStats>() == null)
            yield return null;
        waitingForPlayer = false;
        StartRound();
    }

    void StartRound()
    {
        currentRound++;
        enemiesCurrentlyAlive = 0;
        enemiesRemainingInBank = Mathf.Min(baseRoundBank + bankAddedPerRound * (currentRound - 1) + bankAddedPerStage * (currentStage - 1), maxRoundBank);
        roundActive = true;

        Debug.Log($"[RoundManager] Stage {currentStage} | Round {currentRound} | Bank: {enemiesRemainingInBank} | Max Alive: {maxAliveAtOnce}");
        if (roundText != null) roundText.text = $"Round {currentRound}";
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (enemiesRemainingInBank > 0)
        {
            if (currentSpawnPoints.Count == 0) yield break;

            if (enemiesCurrentlyAlive < maxAliveAtOnce)
            {
                string id = enemyIds[Random.Range(0, enemyIds.Count)];
                Transform spawnPoint = PickFreeSpawnPoint();
                GameObject enemy = EnemySpawnManager.Instance.SpawnEnemy(id, spawnPoint);

                if (enemy != null)
                {
                    ApplyScaling(enemy);
                    ZombieBase zombie = enemy.GetComponent<ZombieBase>();
                    if (zombie != null)
                        zombie.OnDeath += OnEnemyDied;
                    enemiesCurrentlyAlive++;
                    enemiesRemainingInBank--;
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    Transform PickFreeSpawnPoint()
    {
        List<Transform> shuffled = new List<Transform>(currentSpawnPoints);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        foreach (Transform sp in shuffled)
        {
            if (!Physics.CheckSphere(sp.position, spawnPointCheckRadius, enemyLayerMask))
                return sp;
        }

        return shuffled[0];
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
        enemiesCurrentlyAlive--;
        if (enemiesCurrentlyAlive <= 0 && enemiesRemainingInBank <= 0 && roundActive)
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

    int PickNextSceneIndex()
    {
        if (stageScenes.Count == 1) return 0;

        List<int> candidates = new List<int>();
        for (int i = 0; i < stageScenes.Count; i++)
        {
            if (i != lastSceneIndex)
                candidates.Add(i);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    IEnumerator StageChangeRoutine()
    {
        currentStage++;
        Debug.Log($"[RoundManager] Loading next stage in {stageChangeDelay}s...");
        yield return new WaitForSeconds(stageChangeDelay);

        if (stageScenes.Count > 0)
        {
            int nextIndex = PickNextSceneIndex();
            lastSceneIndex = nextIndex;
            SceneManager.LoadScene(stageScenes[nextIndex]);
        }
        else
        {
            Debug.LogWarning("[RoundManager] No stage scenes assigned.");
            StartRound();
        }
    }

    public int GetCurrentRound() => currentRound;
    public int GetCurrentStage() => currentStage;
}