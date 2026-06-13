using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPrefabSpawner : MonoBehaviour
{
    public static GridPrefabSpawner Instance { get; private set; }

    [Header("References")]
    public GridManager gridManager;
    public List<GameObject> prefabLibrary = new List<GameObject>();

    [Header("Patterns")]
    public List<GridPattern> stagePatterns = new List<GridPattern>();

    [Header("Animation Settings")]
    public float riseSpeed = 15f;
    public float riseHeight = 30f;
    public float gridWaitTime = 0.4f;

    private List<GameObject> activePrefabs = new List<GameObject>();
    private bool isTransitioning = false;
    private GridPattern lastLoadedPattern;

    public GridPattern GetLastLoadedPattern() => lastLoadedPattern;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public IEnumerator TransitionToPattern(GridPattern pattern)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        yield return StartCoroutine(DespawnPrefabs());
        gridManager.ApplyPattern(pattern);
        yield return new WaitForSeconds(gridWaitTime);
        yield return StartCoroutine(SpawnPrefabs(pattern));

        isTransitioning = false;
    }

    public IEnumerator TransitionToRandomPattern()
    {
        if (stagePatterns == null || stagePatterns.Count == 0)
        {
            Debug.LogWarning("[GridPrefabSpawner] No stage patterns assigned.");
            yield break;
        }

        GridPattern picked = stagePatterns[Random.Range(0, stagePatterns.Count)];
        lastLoadedPattern = picked;
        Debug.Log($"[GridPrefabSpawner] Transitioning to pattern: {picked.name}");
        yield return StartCoroutine(TransitionToPattern(picked));
    }

    IEnumerator DespawnPrefabs()
    {
        if (activePrefabs.Count == 0) yield break;

        List<Coroutine> animations = new List<Coroutine>();
        foreach (GameObject go in activePrefabs)
            if (go != null)
                animations.Add(StartCoroutine(AnimateDown(go)));

        foreach (Coroutine c in animations)
            yield return c;

        foreach (GameObject go in activePrefabs)
            if (go != null) Destroy(go);

        activePrefabs.Clear();
    }

    IEnumerator SpawnPrefabs(GridPattern pattern)
    {
        foreach (var placement in pattern.prefabPlacements)
        {
            if (placement.prefabIndex < 0 || placement.prefabIndex >= prefabLibrary.Count) continue;

            Vector3 spawnPos = new Vector3(
                placement.position.x,
                placement.position.y - riseHeight,
                placement.position.z
            );

            GameObject go = Instantiate(
                prefabLibrary[placement.prefabIndex],
                spawnPos,
                Quaternion.Euler(placement.eulerAngles)
            );

            go.transform.localScale = placement.scale;
            activePrefabs.Add(go);
            StartCoroutine(AnimateUp(go, placement.position.y));
        }

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator AnimateUp(GameObject go, float targetY)
    {
        while (go != null && !Mathf.Approximately(go.transform.position.y, targetY))
        {
            float newY = Mathf.MoveTowards(go.transform.position.y, targetY, riseSpeed * Time.deltaTime);
            go.transform.position = new Vector3(go.transform.position.x, newY, go.transform.position.z);
            yield return null;
        }
        if (go != null)
            go.transform.position = new Vector3(go.transform.position.x, targetY, go.transform.position.z);
    }

    IEnumerator AnimateDown(GameObject go)
    {
        float targetY = go.transform.position.y - riseHeight;
        while (go != null && !Mathf.Approximately(go.transform.position.y, targetY))
        {
            float newY = Mathf.MoveTowards(go.transform.position.y, targetY, riseSpeed * Time.deltaTime);
            go.transform.position = new Vector3(go.transform.position.x, newY, go.transform.position.z);
            yield return null;
        }
    }
}