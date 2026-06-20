using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections.Generic;

public enum PaintTool { PlusOne, MinusOne, SetTo, Paint }

[CustomEditor(typeof(GridPattern))]
public class GridPatternEditor : Editor
{
    private GridManager gridManager;
    private GridPrefabSpawner prefabSpawner;
    private Vector2 scrollPos;
    private Vector2 scrollPosPrefab;
    private PaintTool activeTool = PaintTool.PlusOne;
    private float setToValue = 0f;
    private float paintValue = 0f;
    private string activeTab = "Heights";
    private int selectedPrefabIndex = 0;
    private float selectedRotation = 0f;

    private static readonly Color[] heightColors = new Color[]
    {
        new Color(0.05f, 0.05f, 0.05f), // 0
        new Color(0.9f, 0.1f, 0.1f),    // 1
        new Color(1.0f, 0.55f, 0.0f),   // 2
        new Color(1.0f, 1.0f, 0.0f),    // 3
        new Color(0.1f, 0.9f, 0.1f),    // 4
        new Color(0.2f, 0.5f, 1.0f),    // 5
        new Color(0.5f, 0.0f, 1.0f),    // 6
        new Color(1.0f, 0.0f, 0.6f),    // 7
        new Color(0.0f, 0.9f, 0.9f),    // 8
        new Color(1.0f, 0.8f, 0.4f),    // 9
        new Color(1.0f, 1.0f, 1.0f),    // 10+
    };

    private GUIStyle labelStyle;

    public override void OnInspectorGUI()
    {
        GridPattern pattern = (GridPattern)target;

        labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.white;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(activeTab == "Heights", "Heights", "Button")) activeTab = "Heights";
        if (GUILayout.Toggle(activeTab == "Prefabs", "Prefabs", "Button")) activeTab = "Prefabs";
        if (GUILayout.Toggle(activeTab == "NavMesh", "NavMesh", "Button")) activeTab = "NavMesh";
        if (GUILayout.Toggle(activeTab == "Export", "Export", "Button")) activeTab = "Export";
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (activeTab == "Heights")
            DrawHeightsTab(pattern);
        else if (activeTab == "Prefabs")
            DrawPrefabsTab(pattern);
        else if (activeTab == "NavMesh")
            DrawNavMeshTab(pattern);
        else if (activeTab == "Export")
            EditorGUILayout.HelpBox("Export tab coming soon.", MessageType.Info);
    }

    private void DrawNavMeshTab(GridPattern pattern)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("NavMesh Data", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bake the NavMesh for this pattern, duplicate the generated .asset file, rename it to match this pattern, then drag it in here.", MessageType.Info);
        EditorGUILayout.Space();

        SerializedObject so = new SerializedObject(pattern);
        so.Update();
        EditorGUILayout.PropertyField(so.FindProperty("navMeshData"), new GUIContent("Nav Mesh Data"));
        so.ApplyModifiedProperties();
    }

    private void DrawHeightsTab(GridPattern pattern)
    {
        EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
        gridManager = (GridManager)EditorGUILayout.ObjectField("Grid Manager", gridManager, typeof(GridManager), true);

        if (GUILayout.Button("Init from Grid Manager", GUILayout.Height(28)))
        {
            if (gridManager == null) { Debug.LogError("Assign a Grid Manager first."); return; }
            pattern.Init(gridManager.gridWidth, gridManager.gridHeight);
            EditorUtility.SetDirty(pattern);
        }

        if (pattern.tiles == null || pattern.tiles.Length == 0)
        {
            EditorGUILayout.HelpBox("Init from Grid Manager to begin.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        // Legend — color index is based on rounded int of the float value
        EditorGUILayout.LabelField("Height Legend", EditorStyles.boldLabel);
        Rect legendRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        float legendCellW = legendRect.width / 11f;
        for (int i = 0; i <= 10; i++)
        {
            Rect cell = new Rect(legendRect.x + i * legendCellW, legendRect.y, legendCellW - 2, legendRect.height);
            EditorGUI.DrawRect(cell, heightColors[i]);
            labelStyle.fontSize = 12;
            GUI.Label(cell, i.ToString(), labelStyle);
        }

        EditorGUILayout.Space();

        // Toolbox
        EditorGUILayout.LabelField("Toolbox", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(activeTool == PaintTool.PlusOne, "+5 units", "Button")) activeTool = PaintTool.PlusOne;
        if (GUILayout.Toggle(activeTool == PaintTool.MinusOne, "-5 units", "Button")) activeTool = PaintTool.MinusOne;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(activeTool == PaintTool.SetTo, "Set To", "Button")) activeTool = PaintTool.SetTo;
        if (GUILayout.Toggle(activeTool == PaintTool.Paint, "Paint", "Button")) activeTool = PaintTool.Paint;
        EditorGUILayout.EndHorizontal();

        // Set To / Paint show float field — displayed as int visually
        if (activeTool == PaintTool.SetTo)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Set To Value (float, shown as int)", GUILayout.Width(200));
            // Display as int visually, store as float
            int displayInt = Mathf.RoundToInt(setToValue);
            int newInt = EditorGUILayout.IntField(displayInt);
            setToValue = (float)newInt;
            EditorGUILayout.EndHorizontal();
        }
        if (activeTool == PaintTool.Paint)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Paint Value (float, shown as int)", GUILayout.Width(200));
            int displayInt = Mathf.RoundToInt(paintValue);
            int newInt = EditorGUILayout.IntField(displayInt);
            paintValue = (float)newInt;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill All (5.0)"))
        {
            for (int i = 0; i < pattern.tiles.Length; i++) pattern.tiles[i] = 5f;
            EditorUtility.SetDirty(pattern);
        }
        if (GUILayout.Button("Clear All (0.0)"))
        {
            for (int i = 0; i < pattern.tiles.Length; i++) pattern.tiles[i] = 0f;
            EditorUtility.SetDirty(pattern);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Grid: {pattern.width} x {pattern.height}");
        EditorGUILayout.Space();

        float inspectorWidth = EditorGUIUtility.currentViewWidth - 20f;
        float buttonSize = Mathf.Floor(inspectorWidth / pattern.width);
        buttonSize = Mathf.Clamp(buttonSize, 8f, 32f);

        float gridPixelHeight = buttonSize * pattern.height;
        float scrollHeight = Mathf.Min(gridPixelHeight + 10f, 600f);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

        float totalWidth = buttonSize * pattern.width;
        float totalHeight = buttonSize * pattern.height;
        Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

        Event e = Event.current;
        int fontSize = Mathf.Max(6, (int)(buttonSize * 0.5f));
        labelStyle.fontSize = fontSize;

        for (int z = pattern.height - 1; z >= 0; z--)
        {
            for (int x = 0; x < pattern.width; x++)
            {
                // Raw float stored value
                float currentFloat = pattern.GetTile(x, z);
                // Visual int — purely for display and color indexing
                int currentInt = Mathf.RoundToInt(currentFloat);
                int colorIndex = Mathf.Clamp(currentInt, 0, heightColors.Length - 1);

                int drawZ = pattern.height - 1 - z;

                Rect cellRect = new Rect(
                    gridRect.x + x * buttonSize,
                    gridRect.y + drawZ * buttonSize,
                    buttonSize - 1,
                    buttonSize - 1
                );

                EditorGUI.DrawRect(cellRect, heightColors[colorIndex]);
                // Show as int visually — the actual stored value is float
                GUI.Label(cellRect, currentInt.ToString(), labelStyle);

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && cellRect.Contains(e.mousePosition))
                {
                    float newVal = currentFloat;
                    switch (activeTool)
                    {
                        case PaintTool.PlusOne: newVal = currentFloat + 5f; break;
                        case PaintTool.MinusOne: newVal = currentFloat - 5f; break;
                        case PaintTool.SetTo: newVal = setToValue; break;
                        case PaintTool.Paint: newVal = paintValue; break;
                    }

                    if (!Mathf.Approximately(newVal, currentFloat))
                    {
                        pattern.SetTile(x, z, newVal);
                        EditorUtility.SetDirty(pattern);
                        Repaint();
                    }

                    e.Use();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPrefabsTab(GridPattern pattern)
    {
        prefabSpawner = (GridPrefabSpawner)EditorGUILayout.ObjectField("Prefab Spawner", prefabSpawner, typeof(GridPrefabSpawner), true);

        if (prefabSpawner == null)
        {
            EditorGUILayout.HelpBox("Assign a GridPrefabSpawner to place prefabs.", MessageType.Info);
            return;
        }

        if (pattern.tiles == null || pattern.tiles.Length == 0)
        {
            EditorGUILayout.HelpBox("Init the pattern from the Heights tab first.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selected Prefab", EditorStyles.boldLabel);

        if (prefabSpawner.prefabLibrary.Count == 0)
        {
            EditorGUILayout.HelpBox("Add prefabs to the GridPrefabSpawner's Prefab Library.", MessageType.Info);
        }
        else
        {
            string[] prefabNames = new string[prefabSpawner.prefabLibrary.Count + 1];
            prefabNames[0] = "Eraser";
            for (int i = 0; i < prefabSpawner.prefabLibrary.Count; i++)
                prefabNames[i + 1] = prefabSpawner.prefabLibrary[i] != null ? prefabSpawner.prefabLibrary[i].name : "null";

            selectedPrefabIndex = EditorGUILayout.Popup("Prefab", selectedPrefabIndex, prefabNames);
            selectedRotation = EditorGUILayout.Slider("Rotation", selectedRotation, 0f, 360f);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear All Prefabs"))
        {
            pattern.prefabPlacements.Clear();
            EditorUtility.SetDirty(pattern);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Grid: {pattern.width} x {pattern.height}  |  Cyan = has prefab", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        float inspectorWidth = EditorGUIUtility.currentViewWidth - 20f;
        float buttonSize = Mathf.Floor(inspectorWidth / pattern.width);
        buttonSize = Mathf.Clamp(buttonSize, 8f, 32f);

        float totalWidth = buttonSize * pattern.width;
        float totalHeight = buttonSize * pattern.height;

        scrollPosPrefab = EditorGUILayout.BeginScrollView(scrollPosPrefab, GUILayout.Height(Mathf.Min(totalHeight + 10f, 600f)));

        Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
        Event e = Event.current;
        labelStyle.fontSize = Mathf.Max(6, (int)(buttonSize * 0.4f));

        for (int z = pattern.height - 1; z >= 0; z--)
        {
            for (int x = 0; x < pattern.width; x++)
            {
                int drawZ = pattern.height - 1 - z;
                Rect cellRect = new Rect(
                    gridRect.x + x * buttonSize,
                    gridRect.y + drawZ * buttonSize,
                    buttonSize - 1,
                    buttonSize - 1
                );

                var placement = pattern.GetPrefabAt(x, z);
                Color cellColor = placement != null ? Color.cyan : new Color(0.15f, 0.15f, 0.15f);
                EditorGUI.DrawRect(cellRect, cellColor);

                if (placement != null)
                    GUI.Label(cellRect, placement.prefabIndex.ToString(), labelStyle);

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && cellRect.Contains(e.mousePosition))
                {
                    int prefabIdx = selectedPrefabIndex == 0 ? -1 : selectedPrefabIndex - 1;
                    pattern.SetPrefab(x, z, prefabIdx, new Vector3(0, selectedRotation, 0));
                    EditorUtility.SetDirty(pattern);
                    Repaint();
                    e.Use();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }
}