using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 40;
    public int gridHeight = 40;
    public float tileSize = 5f;
    public GameObject tilePrefab;
    [Header("Runtime")]
    public Tile[,] tiles;
    void Start()
    {
        RebuildTileReferences();
    }
    public void RebuildTileReferences()
    {
        tiles = new Tile[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Transform t = transform.Find($"Tile_{x}_{z}");
                if (t != null)
                    tiles[x, z] = t.GetComponent<Tile>();
            }
        }
    }
    public void SpawnGrid()
    {
        ClearGrid();
        tiles = new Tile[gridWidth, gridHeight];
        float offsetX = (gridWidth - 1) * tileSize / 2f;
        float offsetZ = (gridHeight - 1) * tileSize / 2f;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 pos = new Vector3(
                    x * tileSize - offsetX,
                    0f,
                    z * tileSize - offsetZ
                );
                GameObject tileGO = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, transform);
                tileGO.name = $"Tile_{x}_{z}";
                Tile tile = tileGO.GetComponent<Tile>();
                if (tile == null)
                    tile = tileGO.AddComponent<Tile>();
                tile.tileSize = tileSize;
                tile.animationSpeed = 30f;
                tileGO.transform.localPosition = new Vector3(pos.x, 0f, pos.z);
                tile.SetHeightImmediate(1);
                tiles[x, z] = tile;
            }
        }
    }
    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        tiles = null;
    }
    public void ApplyPattern(GridPattern pattern)
    {
        if (pattern.width != gridWidth || pattern.height != gridHeight)
        {
            Debug.LogError("Pattern size doesn't match grid size.");
            return;
        }
        StartCoroutine(TransitionRoutine(pattern));
    }
    private IEnumerator TransitionRoutine(GridPattern pattern)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
                tiles[x, z].ApplyHeight(pattern.GetTile(x, z));

            yield return null;
        }
    }
}