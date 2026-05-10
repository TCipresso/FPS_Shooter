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
            if (gm.tilePrefab == null)
            {
                Debug.LogError("Assign a tile prefab first.");
                return;
            }
            gm.SpawnGrid();
            EditorUtility.SetDirty(gm);
        }

        if (GUILayout.Button("Clear Grid", GUILayout.Height(25)))
        {
            gm.ClearGrid();
            EditorUtility.SetDirty(gm);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pattern Saving", EditorStyles.boldLabel);
        patternName = EditorGUILayout.TextField("Pattern Name", patternName);

        if (GUILayout.Button("Save Current State as Pattern", GUILayout.Height(35)))
        {
            SavePattern(gm);
        }
    }

    void SavePattern(GridManager gm)
    {
        if (gm.tiles == null)
        {
            Debug.LogError("No grid spawned.");
            return;
        }

        GridPattern pattern = ScriptableObject.CreateInstance<GridPattern>();
        pattern.Init(gm.gridWidth, gm.gridHeight);

        // Read tile heights
        for (int x = 0; x < gm.gridWidth; x++)
        {
            for (int z = 0; z < gm.gridHeight; z++)
            {
                Tile tile = gm.tiles[x, z];
                if (tile == null) continue;

                // Reverse the math: localY = (height-1)*tileSize - columnHeight
                // So height = (localY + columnHeight) / tileSize + 1
                float localY = tile.transform.localPosition.y;
                int height = Mathf.RoundToInt((localY + tile.columnHeight) / gm.tileSize);
                height = Mathf.Clamp(height, 0, 5);
                pattern.SetTile(x, z, height);
            }
        }

        // Read prefab markers
        PatternPrefabMarker[] markers = FindObjectsByType<PatternPrefabMarker>(FindObjectsSortMode.None);
        foreach (var marker in markers)
        {
            // Find closest tile
            float closestDist = float.MaxValue;
            int bestX = 0, bestZ = 0;

            for (int x = 0; x < gm.gridWidth; x++)
            {
                for (int z = 0; z < gm.gridHeight; z++)
                {
                    Tile tile = gm.tiles[x, z];
                    if (tile == null) continue;

                    float dist = Vector2.Distance(
                        new Vector2(marker.transform.position.x, marker.transform.position.z),
                        new Vector2(tile.transform.position.x, tile.transform.position.z)
                    );

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }

            float rotation = marker.transform.eulerAngles.y;
            pattern.SetPrefab(bestX, bestZ, marker.prefabIndex, rotation);
        }

        // Save as asset
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