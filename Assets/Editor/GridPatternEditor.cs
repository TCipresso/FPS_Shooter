using UnityEngine;
using UnityEditor;

public enum PaintTool { PlusOne, MinusOne, SetTo, Paint }

[CustomEditor(typeof(GridPattern))]
public class GridPatternEditor : Editor
{
    private GridManager gridManager;
    private Vector2 scrollPos;
    private PaintTool activeTool = PaintTool.PlusOne;
    private int setToValue = 1;
    private int paintValue = 1;
    private string activeTab = "Heights";

    private static readonly Color[] heightColors = new Color[]
    {
        new Color(0.05f, 0.05f, 0.05f), // 0 - black
        new Color(0.9f, 0.1f, 0.1f),    // 1 - red
        new Color(1.0f, 0.55f, 0.0f),   // 2 - orange
        new Color(1.0f, 1.0f, 0.0f),    // 3 - yellow
        new Color(0.1f, 0.9f, 0.1f),    // 4 - green
        new Color(0.2f, 0.5f, 1.0f),    // 5 - blue
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
        if (GUILayout.Toggle(activeTab == "Export", "Export", "Button")) activeTab = "Export";
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (activeTab == "Heights")
            DrawHeightsTab(pattern);
        else if (activeTab == "Prefabs")
            EditorGUILayout.HelpBox("Prefabs tab coming soon.", MessageType.Info);
        else if (activeTab == "Export")
            EditorGUILayout.HelpBox("Export tab coming soon.", MessageType.Info);
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

        // Legend
        EditorGUILayout.LabelField("Height Legend", EditorStyles.boldLabel);
        Rect legendRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        float legendCellW = legendRect.width / 6f;
        for (int i = 0; i <= 5; i++)
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
        if (GUILayout.Toggle(activeTool == PaintTool.PlusOne, "Plus One", "Button")) activeTool = PaintTool.PlusOne;
        if (GUILayout.Toggle(activeTool == PaintTool.MinusOne, "Minus One", "Button")) activeTool = PaintTool.MinusOne;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(activeTool == PaintTool.SetTo, "Set To", "Button")) activeTool = PaintTool.SetTo;
        if (GUILayout.Toggle(activeTool == PaintTool.Paint, "Paint", "Button")) activeTool = PaintTool.Paint;
        EditorGUILayout.EndHorizontal();

        if (activeTool == PaintTool.SetTo)
            setToValue = EditorGUILayout.IntSlider("Set To Value", setToValue, 0, 5);
        if (activeTool == PaintTool.Paint)
            paintValue = EditorGUILayout.IntSlider("Paint Value", paintValue, 0, 5);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill All (1)"))
        {
            for (int i = 0; i < pattern.tiles.Length; i++) pattern.tiles[i] = 1;
            EditorUtility.SetDirty(pattern);
        }
        if (GUILayout.Button("Clear All (0)"))
        {
            for (int i = 0; i < pattern.tiles.Length; i++) pattern.tiles[i] = 0;
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

        // Reserve the full grid rect
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
                int current = pattern.GetTile(x, z);

                int drawZ = pattern.height - 1 - z;
                Rect cellRect = new Rect(
                    gridRect.x + x * buttonSize,
                    gridRect.y + drawZ * buttonSize,
                    buttonSize - 1,
                    buttonSize - 1
                );

                // Draw solid color rect
                EditorGUI.DrawRect(cellRect, heightColors[current]);

                // Draw number label
                GUI.Label(cellRect, current.ToString(), labelStyle);

                // Mouse interaction
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && cellRect.Contains(e.mousePosition))
                {
                    int newVal = current;
                    switch (activeTool)
                    {
                        case PaintTool.PlusOne: newVal = Mathf.Clamp(current + 1, 0, 5); break;
                        case PaintTool.MinusOne: newVal = Mathf.Clamp(current - 1, 0, 5); break;
                        case PaintTool.SetTo: newVal = setToValue; break;
                        case PaintTool.Paint: newVal = paintValue; break;
                    }

                    if (newVal != current)
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
}