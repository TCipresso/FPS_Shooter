using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class BakeWorldOrigin
{
    const string BakedMeshFolder = "Assets/BakedMeshes/WorldOrigin";

    [MenuItem("Tools/Bloodsport/Bake World Origin On Selected")]
    static void BakeSelected()
    {
        if (!Directory.Exists(BakedMeshFolder))
            Directory.CreateDirectory(BakedMeshFolder);

        int count = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh source = mf.sharedMesh;
            Mesh baked = Object.Instantiate(source);
            baked.name = source.name + "_originbaked";

            Vector3 originWS = go.transform.position;
            int vertCount = baked.vertexCount;
            List<Vector3> originUV = new List<Vector3>(vertCount);
            for (int i = 0; i < vertCount; i++)
                originUV.Add(originWS);

            baked.SetUVs(1, originUV);

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{BakedMeshFolder}/{go.name}_{baked.name}.asset");
            AssetDatabase.CreateAsset(baked, path);

            mf.sharedMesh = baked;
            EditorUtility.SetDirty(mf);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Baked world origin into UV1 for {count} object(s).");
    }
}
#endif