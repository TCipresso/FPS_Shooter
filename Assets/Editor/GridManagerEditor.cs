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

        if (GUILayout.Button("Clear Grid", GUILayout.Height(25)))
        {
            gm.ClearGrid();
            EditorUtility.SetDirty(gm);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Load Pattern", EditorStyles.boldLabel);
        gm.patternToLoad = (GridPattern)EditorGUILayout.ObjectField("Pattern", gm.patternToLoad, typeof(GridPattern), false);

        if (GUILayout.Button("Load Pattern", GUILayout.Height(35)))
        {
            if (gm.patternToLoad == null) { Debug.LogError("Assign a pattern first."); return; }
            gm.LoadPatternImmediate(gm.patternToLoad);
            EditorUtility.SetDirty(gm);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pattern Saving", EditorStyles.boldLabel);
        patternName = EditorGUILayout.TextField("Pattern Name", patternName);

        if (GUILayout.Button("Save Current State as Pattern", GUILayout.Height(35)))
            SavePattern(gm);

        if (GUILayout.Button("Clear Scene Prefabs", GUILayout.Height(25)))
        {
            PatternPrefabMarker[] markers = FindObjectsByType<PatternPrefabMarker>(FindObjectsSortMode.None);
            foreach (var marker in markers)
                DestroyImmediate(marker.gameObject);
        }

        // Debug readout: shows tile Y values as integers (visual only, stored as float)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tile Heights (visual int, stored float)", EditorStyles.boldLabel);
        if (gm.tiles != null && GUILayout.Button("Print Tile Heights to Console"))
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int z = gm.gridHeight - 1; z >= 0; z--)
            {
                for (int x = 0; x < gm.gridWidth; x++)
                {
                    Tile t = gm.tiles[x, z];
                    float rawY = t != null ? t.transform.localPosition.y : 0f;
                    // Display as int for readability, actual value is float
                    sb.Append(Mathf.RoundToInt(rawY).ToString("D3")).Append(" ");
                }
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());
        }
    }

    void SavePattern(GridManager gm)
    {
        if (gm.tiles == null) { Debug.LogError("No grid spawned."); return; }

        GridPattern pattern = ScriptableObject.CreateInstance<GridPattern>();
        pattern.Init(gm.gridWidth, gm.gridHeight);

        for (int x = 0; x < gm.gridWidth; x++)
        {
            for (int z = 0; z < gm.gridHeight; z++)
            {
                Tile tile = gm.tiles[x, z];
                if (tile == null) continue;

                // Save raw world Y directly — no math, no conversion
                float worldY = tile.transform.localPosition.y;
                pattern.SetTile(x, z, worldY);
            }
        }

        PatternPrefabMarker[] markers = FindObjectsByType<PatternPrefabMarker>(FindObjectsSortMode.None);
        foreach (var marker in markers)
        {
            pattern.prefabPlacements.Add(new GridPattern.PrefabPlacement
            {
                prefabIndex = marker.prefabIndex,
                position = marker.transform.position,
                eulerAngles = marker.transform.eulerAngles,
                scale = marker.transform.localScale
            });
        }

        foreach (var marker in markers)
            DestroyImmediate(marker.gameObject);

        string path = $"Assets/Patterns/{patternName}.asset";
        System.IO.Directory.CreateDirectory("Assets/Patterns");
        AssetDatabase.CreateAsset(pattern, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Pattern saved to {path}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = pattern;
    }
}