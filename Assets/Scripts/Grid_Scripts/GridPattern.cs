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
        public int x;
        public int z;
        public int prefabIndex;
        public float rotation; // Y axis rotation in degrees
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
        tiles[x + z * width] = Mathf.Clamp(value, 0, 5);
    }

    public void SetPrefab(int x, int z, int prefabIndex, float rotation)
    {
        // Remove existing at this position
        prefabPlacements.RemoveAll(p => p.x == x && p.z == z);

        if (prefabIndex < 0) return; // -1 = erase

        prefabPlacements.Add(new PrefabPlacement { x = x, z = z, prefabIndex = prefabIndex, rotation = rotation });
    }

    public PrefabPlacement GetPrefabAt(int x, int z)
    {
        return prefabPlacements.Find(p => p.x == x && p.z == z);
    }
}