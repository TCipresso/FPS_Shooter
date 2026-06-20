using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    private string patternName = "NewPattern";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GridManager gm = (GridManager)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Spawn Grid", GUILayout.Height(35)))
        {
            if (gm.tilePrefab == null) { Debug.LogError("Assign a tile prefab first."); return; }
            gm.SpawnGrid();
            EditorUtility.SetDirty(gm);
        }

        if (GUILayout.Button("Spawn Fresh Grid (Zeroed)", GUILayout.Height(35)))
        {
            if (gm.tilePrefab == null) { Debug.LogError("Assign a tile prefab first."); return; }
            ClearScenePrefabs();
            gm.patternToLoad = null;
            gm.SpawnGrid();
            EditorUtility.SetDirty(gm);
        }

        if (GUILayout.Button("Clear Grid", GUILayout.Height(25)))
        {
            ClearScenePrefabs();
            gm.ClearGrid();
            EditorUtility.SetDirty(gm);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Load Pattern", EditorStyles.boldLabel);
        gm.patternToLoad = (GridPattern)EditorGUILayout.ObjectField("Pattern", gm.patternToLoad, typeof(GridPattern), false);

        if (GUILayout.Button("Load Pattern (Heights Only)", GUILayout.Height(35)))
        {
            if (gm.patternToLoad == null) { Debug.LogError("Assign a pattern first."); return; }
            gm.RebuildTileReferences();
            gm.LoadPatternImmediate(gm.patternToLoad);
            EditorUtility.SetDirty(gm);
        }

        if (GUILayout.Button("Load Pattern + Prefabs", GUILayout.Height(35)))
        {
            if (gm.patternToLoad == null) { Debug.LogError("Assign a pattern first."); return; }
            gm.RebuildTileReferences();
            gm.LoadPatternImmediate(gm.patternToLoad);
            LoadPrefabMarkersIntoScene(gm.patternToLoad);
            EditorUtility.SetDirty(gm);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pattern Saving", EditorStyles.boldLabel);
        patternName = EditorGUILayout.TextField("Pattern Name", patternName);

        if (GUILayout.Button("Save Current State as Pattern", GUILayout.Height(35)))
            SavePattern(gm);

        if (GUILayout.Button("Clear Scene Prefabs", GUILayout.Height(25)))
            ClearScenePrefabs();
    }

    void ClearScenePrefabs()
    {
        PatternPrefabMarker[] markers = FindObjectsByType<PatternPrefabMarker>(FindObjectsSortMode.None);
        foreach (var marker in markers)
            if (marker != null) DestroyImmediate(marker.gameObject);
    }

    void LoadPrefabMarkersIntoScene(GridPattern pattern)
    {
        ClearScenePrefabs();

        GridPrefabSpawner spawner = FindFirstObjectByType<GridPrefabSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[GridManagerEditor] No GridPrefabSpawner found in scene.");
            return;
        }

        foreach (var placement in pattern.prefabPlacements)
        {
            if (placement.prefabIndex < 0 || placement.prefabIndex >= spawner.prefabLibrary.Count)
            {
                Debug.LogWarning($"[GridManagerEditor] Prefab index {placement.prefabIndex} out of range, skipping.");
                continue;
            }

            GameObject prefab = spawner.prefabLibrary[placement.prefabIndex];
            if (prefab == null) continue;

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = placement.position;
            go.transform.eulerAngles = placement.eulerAngles;
            go.transform.localScale = placement.scale;

            PatternPrefabMarker marker = go.GetComponent<PatternPrefabMarker>();
            if (marker == null) marker = go.AddComponent<PatternPrefabMarker>();
            marker.prefabIndex = placement.prefabIndex;
        }

        Debug.Log($"[GridManagerEditor] Loaded {pattern.prefabPlacements.Count} prefab(s) into scene.");
    }

    void SavePattern(GridManager gm)
    {
        if (gm.tiles == null)
            gm.RebuildTileReferences();

        if (gm.tiles == null)
        {
            Debug.LogError("No grid found in scene. Spawn the grid first.");
            return;
        }

        string path = $"Assets/Patterns/{patternName}.asset";
        System.IO.Directory.CreateDirectory("Assets/Patterns");

        // Delete existing asset at that path so we never accumulate old data
        if (System.IO.File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }

        GridPattern pattern = ScriptableObject.CreateInstance<GridPattern>();
        pattern.Init(gm.gridWidth, gm.gridHeight);

        for (int x = 0; x < gm.gridWidth; x++)
        {
            for (int z = 0; z < gm.gridHeight; z++)
            {
                Tile tile = gm.tiles[x, z];
                if (tile == null) continue;
                float worldY = tile.transform.localPosition.y;
                pattern.SetTile(x, z, worldY);
            }
        }

        PatternPrefabMarker[] markers = FindObjectsByType<PatternPrefabMarker>(FindObjectsSortMode.None);
        foreach (var marker in markers)
        {
            if (marker == null) continue;
            pattern.prefabPlacements.Add(new GridPattern.PrefabPlacement
            {
                prefabIndex = marker.prefabIndex,
                position = marker.transform.position,
                eulerAngles = marker.transform.eulerAngles,
                scale = marker.transform.localScale
            });
        }

        AssetDatabase.CreateAsset(pattern, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Pattern saved to {path} with {pattern.prefabPlacements.Count} prefab(s).");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = pattern;
    }
}