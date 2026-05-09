using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
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
    }
}