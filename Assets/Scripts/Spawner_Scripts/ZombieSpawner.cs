using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZombieSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject zombiePrefab;
    public List<Transform> spawnPoints;

    [Header("Round Settings")]
    public int zombiesInFirstRound = 6;
    public float zombiesPerRoundMultiplier = 1.5f;
    public float timeBetweenSpawns = 1.5f;
    public float timeBetweenRounds = 5f;

    public int CurrentRound { get; private set; } = 0;
    int zombiesLeftToSpawn = 0;
    int zombiesAlive = 0;
    bool isSpawning = false;

    void Start()
    {
        StartNextRound();
    }

    void StartNextRound()
    {
        CurrentRound++;
        zombiesLeftToSpawn = Mathf.RoundToInt(zombiesInFirstRound * Mathf.Pow(zombiesPerRoundMultiplier, CurrentRound - 1));

        Debug.Log($"[ZombieSpawner] Round {CurrentRound} started. Spawning {zombiesLeftToSpawn} zombies.");

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        isSpawning = true;

        while (zombiesLeftToSpawn > 0)
        {
            SpawnZombie();
            zombiesLeftToSpawn--;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;
    }

    void SpawnZombie()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[ZombieSpawner] No spawn points assigned.");
            return;
        }

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject zombie = Instantiate(zombiePrefab, point.position, point.rotation);

        ZombieBase zb = zombie.GetComponentInChildren<ZombieBase>();
        if (zb != null)
            zb.OnDeath += OnZombieDied;

        zombiesAlive++;
    }

    void OnZombieDied()
    {
        zombiesAlive--;

        if (!isSpawning && zombiesAlive <= 0)
            StartCoroutine(NextRoundDelay());
    }

    IEnumerator NextRoundDelay()
    {
        Debug.Log($"[ZombieSpawner] Round {CurrentRound} cleared. Next round in {timeBetweenRounds}s.");
        yield return new WaitForSeconds(timeBetweenRounds);
        StartNextRound();
    }
}