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
    public List<string> stageScenes = new List<string>();

    [Header("Stage Portals")]
    public List<GameObject> portalPrefabs = new List<GameObject>();

    [Header("Money Power Ups")]
    public GameObject moneyPickupPrefab;
    public int minMoneyPickups = 10;
    public int maxMoneyPickups = 15;
    public int moneyClusterCount = 3;
    public float clusterRadius = 2.5f;

    [Header("Weapon Power Ups")]
    public List<GameObject> weaponPowerUpPrefabs = new List<GameObject>();
    [Range(0f, 1f)] public float extraWeaponPowerUpChance = 0.5f;
    public float weaponPowerUpOffsetRadius = 1.2f;

    [Header("Zombie Drops")]
    [Range(0f, 1f)] public float zombieMoneyDropChance = 0.15f;
    [Range(0f, 1f)] public float zombieWeaponDropChance = 0.03f;
    public float zombieDropGroundOffset = 0.1f;

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
    List<Transform> currentPortalSpawnPoints = new List<Transform>();
    List<Transform> currentPickupSpawnPoints = new List<Transform>();
    List<GameObject> activeStagePortals = new List<GameObject>();
    List<Transform> spawnShuffleCache = new List<Transform>();
    bool waitingForPlayer = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        lastSceneIndex = stageScenes.IndexOf(SceneManager.GetActiveScene().name);
    }

    IEnumerator Start()
    {
        yield return null;
        if (currentRound == 0 && !waitingForPlayer)
        {
            StageSetup setup = FindFirstObjectByType<StageSetup>();
            if (setup != null)
            {
                currentSpawnPoints = setup.spawnPoints;
                currentPortalSpawnPoints = setup.portalSpawnPoints;
                currentPickupSpawnPoints = setup.pickupSpawnPoints;
            }
            waitingForPlayer = true;
            StartCoroutine(WaitForPlayerThenStart());
        }
    }

    public void OnSceneReady(List<Transform> spawnPoints, List<Transform> portalSpawnPoints, List<Transform> pickupSpawnPoints)
    {
        currentSpawnPoints = spawnPoints;
        currentPortalSpawnPoints = portalSpawnPoints;
        currentPickupSpawnPoints = pickupSpawnPoints;
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

        SpawnMoneyPickups();
        SpawnWeaponPowerUps();

        if (roundText != null) roundText.text = $"Round {currentRound}";
        StartCoroutine(SpawnRoutine());
    }

    GameObject SpawnPickup(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;
        if (PickupPool.Instance != null)
            return PickupPool.Instance.Spawn(prefab, position, Quaternion.identity);
        return Instantiate(prefab, position, Quaternion.identity);
    }

    void SpawnMoneyPickups()
    {
        if (moneyPickupPrefab == null || currentPickupSpawnPoints.Count == 0) return;

        List<Transform> shuffledPoints = new List<Transform>(currentPickupSpawnPoints);
        for (int i = shuffledPoints.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffledPoints[i], shuffledPoints[j]) = (shuffledPoints[j], shuffledPoints[i]);
        }

        int clusterCount = Mathf.Min(moneyClusterCount, shuffledPoints.Count);
        int totalMoney = Random.Range(minMoneyPickups, maxMoneyPickups + 1);

        for (int i = 0; i < totalMoney; i++)
        {
            Transform clusterCenter = shuffledPoints[i % clusterCount];
            Vector2 offset = Random.insideUnitCircle * clusterRadius;
            Vector3 spawnPos = clusterCenter.position + new Vector3(offset.x, 0f, offset.y);
            SpawnPickup(moneyPickupPrefab, spawnPos);
        }
    }

    void SpawnWeaponPowerUps()
    {
        if (weaponPowerUpPrefabs.Count == 0 || currentPickupSpawnPoints.Count == 0) return;

        Transform spawnPoint = currentPickupSpawnPoints[Random.Range(0, currentPickupSpawnPoints.Count)];
        GameObject chosenWeapon = weaponPowerUpPrefabs[Random.Range(0, weaponPowerUpPrefabs.Count)];
        float chance = extraWeaponPowerUpChance;

        SpawnPickup(chosenWeapon, spawnPoint.position);

        while (Random.value < chance)
        {
            Vector2 offset = Random.insideUnitCircle * weaponPowerUpOffsetRadius;
            Vector3 spawnPos = spawnPoint.position + new Vector3(offset.x, 0f, offset.y);
            SpawnPickup(chosenWeapon, spawnPos);
            chance *= extraWeaponPowerUpChance;
        }
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
                        zombie.OnDeath += () => OnEnemyDied(zombie.transform.position);
                    enemiesCurrentlyAlive++;
                    enemiesRemainingInBank--;
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    Transform PickFreeSpawnPoint()
    {
        spawnShuffleCache.Clear();
        spawnShuffleCache.AddRange(currentSpawnPoints);
        for (int i = spawnShuffleCache.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (spawnShuffleCache[i], spawnShuffleCache[j]) = (spawnShuffleCache[j], spawnShuffleCache[i]);
        }

        foreach (Transform sp in spawnShuffleCache)
        {
            if (!Physics.CheckSphere(sp.position, spawnPointCheckRadius, enemyLayerMask))
                return sp;
        }

        return spawnShuffleCache[0];
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

    void OnEnemyDied(Vector3 deathPosition)
    {
        enemiesCurrentlyAlive--;
        TryDropPickup(deathPosition);
        if (enemiesCurrentlyAlive <= 0 && enemiesRemainingInBank <= 0 && roundActive)
        {
            roundActive = false;
            OnRoundComplete();
        }
    }

    void TryDropPickup(Vector3 position)
    {
        position += Vector3.up * zombieDropGroundOffset;
        float roll = Random.value;

        if (roll < zombieWeaponDropChance)
        {
            if (weaponPowerUpPrefabs.Count == 0) return;
            SpawnPickup(weaponPowerUpPrefabs[Random.Range(0, weaponPowerUpPrefabs.Count)], position);
        }
        else if (roll < zombieWeaponDropChance + zombieMoneyDropChance)
        {
            SpawnPickup(moneyPickupPrefab, position);
        }
    }

    void OnRoundComplete()
    {
        if (currentRound % roundsPerStage == 0)
            SpawnStagePortals();
        else
            StartRound();
    }

    void SpawnStagePortals()
    {
        if (portalPrefabs.Count == 0 || currentPortalSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[RoundManager] No portal prefabs or portal spawn points assigned, staying on current stage.");
            StartRound();
            return;
        }

        List<Transform> shuffledPoints = new List<Transform>(currentPortalSpawnPoints);
        for (int i = shuffledPoints.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffledPoints[i], shuffledPoints[j]) = (shuffledPoints[j], shuffledPoints[i]);
        }

        List<GameObject> shuffledPrefabs = new List<GameObject>(portalPrefabs);
        for (int i = 0; i < shuffledPrefabs.Count - 1; i++)
        {
            int j = Random.Range(i, shuffledPrefabs.Count);
            (shuffledPrefabs[i], shuffledPrefabs[j]) = (shuffledPrefabs[j], shuffledPrefabs[i]);
        }

        int count = Mathf.Min(shuffledPrefabs.Count, shuffledPoints.Count);
        activeStagePortals.Clear();
        for (int i = 0; i < count; i++)
        {
            GameObject portal = Instantiate(shuffledPrefabs[i], shuffledPoints[i].position, shuffledPoints[i].rotation);
            activeStagePortals.Add(portal);
        }
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

    public void OnStagePortalEntered()
    {
        foreach (GameObject portal in activeStagePortals)
            if (portal != null) Destroy(portal);
        activeStagePortals.Clear();

        if (PickupPool.Instance != null)
            PickupPool.Instance.ReturnAllActive();

        currentStage++;

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