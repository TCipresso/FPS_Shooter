#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Put this file anywhere in your project (an "Editor" folder is the Unity
// convention, but the #if UNITY_EDITOR guard already keeps it out of
// builds regardless). Adds a big "Generate Hills" button to the top of the
// HillMesh Inspector so you can regenerate on demand, outside Play mode,
// without it rebuilding on every single keystroke while you're typing
// values in.
[CustomEditor(typeof(HillMesh))]
public class HillMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HillMesh hillMesh = (HillMesh)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Hills", GUILayout.Height(35)))
        {
            Undo.RecordObject(hillMesh, "Generate Hills");
            hillMesh.Generate();
        }

        GUILayout.Space(6);
        if (GUILayout.Button("Generate Props", GUILayout.Height(30)))
        {
            hillMesh.GenerateProps();
        }
        if (GUILayout.Button("Clear Props", GUILayout.Height(22)))
        {
            hillMesh.ClearProps();
        }
    }
}
#endif