using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using Unity.AI.Navigation;

[CreateAssetMenu(fileName = "NewPattern", menuName = "Bloodsport/Pattern")]
public class GridPattern : ScriptableObject
{
    public int width;
    public int height;

    // Raw world Y positions per tile — no height index math
    public float[] tiles;

    public NavMeshData navMeshData;

    [System.Serializable]
    public class PrefabPlacement
    {
        public int prefabIndex;
        public Vector3 position;
        public Vector3 eulerAngles;
        public Vector3 scale;
    }

    public List<PrefabPlacement> prefabPlacements = new List<PrefabPlacement>();

    public void Init(int w, int h)
    {
        width = w;
        height = h;
        tiles = new float[w * h];
        prefabPlacements = new List<PrefabPlacement>();
    }

    public float GetTile(int x, int z)
    {
        return tiles[x + z * width];
    }

    public void SetTile(int x, int z, float worldY)
    {
        tiles[x + z * width] = worldY;
    }

    public PrefabPlacement GetPrefabAt(int x, int z)
    {
        return prefabPlacements.Find(p =>
            Mathf.RoundToInt(p.position.x) == x &&
            Mathf.RoundToInt(p.position.z) == z);
    }

    public void SetPrefab(int x, int z, int prefabIndex, Vector3 eulerAngles)
    {
        prefabPlacements.RemoveAll(p =>
            Mathf.RoundToInt(p.position.x) == x &&
            Mathf.RoundToInt(p.position.z) == z);
        if (prefabIndex < 0) return;
        prefabPlacements.Add(new PrefabPlacement
        {
            prefabIndex = prefabIndex,
            position = new Vector3(x, 0, z),
            eulerAngles = eulerAngles,
            scale = Vector3.one
        });
    }
}