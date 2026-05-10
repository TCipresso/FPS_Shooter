using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPrefabSpawner : MonoBehaviour
{
    public static GridPrefabSpawner Instance { get; private set; }

    [Header("References")]
    public GridManager gridManager;
    public List<GameObject> prefabLibrary = new List<GameObject>();

    [Header("Animation Settings")]
    public float riseSpeed = 15f;
    public float riseHeight = 30f; // how far below ground they start

    private List<GameObject> activePrefabs = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public IEnumerator TransitionToPattern(GridPattern pattern)
    {
        // Step 1: despawn existing prefabs
        yield return StartCoroutine(DespawnPrefabs());

        // Step 2: transition grid
        gridManager.ApplyPattern(pattern);

        // Step 3: wait for grid to finish animating
        yield return new WaitForSeconds(0.8f);

        // Step 4: spawn new prefabs
        yield return StartCoroutine(SpawnPrefabs(pattern));
    }

    IEnumerator DespawnPrefabs()
    {
        if (activePrefabs.Count == 0) yield break;

        List<Coroutine> animations = new List<Coroutine>();
        foreach (GameObject go in activePrefabs)
        {
            if (go != null)
                animations.Add(StartCoroutine(AnimateDown(go)));
        }

        // Wait for all to finish
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

            // Get tile world position
            Tile tile = gridManager.tiles[placement.x, placement.z];
            if (tile == null) continue;

            int tileHeight = pattern.GetTile(placement.x, placement.z);
            float topY = (tileHeight - 1) * gridManager.tileSize;

            Vector3 spawnPos = new Vector3(
                tile.transform.position.x,
                topY - riseHeight,
                tile.transform.position.z
            );

            GameObject go = Instantiate(
                prefabLibrary[placement.prefabIndex],
                spawnPos,
                Quaternion.Euler(0, placement.rotation, 0)
            );

            activePrefabs.Add(go);
            StartCoroutine(AnimateUp(go, topY));
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