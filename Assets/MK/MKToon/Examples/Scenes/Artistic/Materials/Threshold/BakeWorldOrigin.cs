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

        int bakedCount = 0;
        int skippedCount = 0;

        foreach (GameObject go in Selection.gameObjects)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null)
            {
                Debug.LogWarning($"Skipped '{go.name}': no MeshFilter on this object directly. " +
                                  "Select the child object that actually has the mesh, not a parent.", go);
                skippedCount++;
                continue;
            }
            if (mf.sharedMesh == null)
            {
                Debug.LogWarning($"Skipped '{go.name}': MeshFilter has no mesh assigned.", go);
                skippedCount++;
                continue;
            }

            Mesh source = mf.sharedMesh;
            Mesh baked = Object.Instantiate(source);
            baked.name = source.name + "_originbaked";

            Vector3 originWS = go.transform.position;
            Quaternion rot = go.transform.rotation;
            Matrix4x4 rotMatrix = Matrix4x4.Rotate(rot);
            Vector3 row0 = rotMatrix.GetRow(0);
            Vector3 row1 = rotMatrix.GetRow(1);
            Vector3 row2 = rotMatrix.GetRow(2);

            int vertCount = baked.vertexCount;
            List<Vector3> originUV = new List<Vector3>(vertCount);
            List<Vector3> row0UV = new List<Vector3>(vertCount);
            List<Vector3> row1UV = new List<Vector3>(vertCount);
            List<Vector3> row2UV = new List<Vector3>(vertCount);
            for (int i = 0; i < vertCount; i++)
            {
                originUV.Add(originWS);
                row0UV.Add(row0);
                row1UV.Add(row1);
                row2UV.Add(row2);
            }

            // UV4-7 used deliberately - UV0 is diffuse, UV1/UV2 can be
            // silently overwritten by Unity's lightmap UV generation on
            // static objects, which would corrupt baked data stored there.
            baked.SetUVs(4, originUV);
            baked.SetUVs(5, row0UV);
            baked.SetUVs(6, row1UV);
            baked.SetUVs(7, row2UV);

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{BakedMeshFolder}/{go.name}_{baked.name}.asset");
            AssetDatabase.CreateAsset(baked, path);

            mf.sharedMesh = baked;
            EditorUtility.SetDirty(mf);
            bakedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Baked world origin + rotation for {bakedCount} object(s). Skipped {skippedCount}.");
    }
}
#endif