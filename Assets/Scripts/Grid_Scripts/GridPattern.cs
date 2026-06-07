using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewPattern", menuName = "Bloodsport/Pattern")]
public class GridPattern : ScriptableObject
{
    public int width;
    public int height;
    public int[] tiles;

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
        tiles = new int[w * h];
        prefabPlacements = new List<PrefabPlacement>();
    }

    public int GetTile(int x, int z)
    {
        return tiles[x + z * width];
    }

    public void SetTile(int x, int z, int value)
    {
        tiles[x + z * width] = Mathf.Clamp(value, 0, 10);
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