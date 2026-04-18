#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AugmentDraftUI))]
public class AugmentDraftUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var ui = (AugmentDraftUI)target;
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Open Augment Draft (Play Mode)"))
                ui.OpenAugmentDraft();

            if (GUILayout.Button("Open Upgrade Draft (Play Mode)"))
                ui.OpenUpgradeDraft();

            if (GUILayout.Button("Close Draft (Play Mode)"))
                ui.CloseDraft();
        }
        if (GUILayout.Button("Reset CanvasGroup Alpha to 0"))
        {
            if (ui.panelGroup)
            {
                ui.panelGroup.alpha = 0f;
                ui.panelGroup.interactable = false;
                ui.panelGroup.blocksRaycasts = false;
                EditorUtility.SetDirty(ui.panelGroup);
            }
        }
    }
}
#endif