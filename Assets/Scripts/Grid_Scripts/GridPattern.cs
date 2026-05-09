using UnityEngine;

[CreateAssetMenu(fileName = "NewPattern", menuName = "Grid/Pattern")]
public class GridPattern : ScriptableObject
{
    public int width;
    public int height;
    public int[] tiles;

    public void Init(int w, int h)
    {
        width = w;
        height = h;
        tiles = new int[w * h];
    }

    public int GetTile(int x, int z)
    {
        return tiles[x + z * width];
    }

    public void SetTile(int x, int z, int value)
    {
        tiles[x + z * width] = Mathf.Clamp(value, 0, 5);
    }
}