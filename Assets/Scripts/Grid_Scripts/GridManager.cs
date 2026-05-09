using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 40;
    public int gridHeight = 40;
    public float tileSize = 5f;
    public int maxHeight = 5;
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

                GameObject tileRoot = new GameObject($"Tile_{x}_{z}");
                tileRoot.transform.parent = transform;
                tileRoot.transform.localPosition = pos;

                Tile tile = tileRoot.AddComponent<Tile>();
                tile.tileSize = tileSize;
                tile.animationSpeed = 30f;

                // Stack 5 cubes downward, cube 0 is the top
                List<GameObject> stack = new List<GameObject>();
                for (int h = 0; h < maxHeight; h++)
                {
                    GameObject cube = Instantiate(tilePrefab, tileRoot.transform);
                    cube.transform.localPosition = new Vector3(0f, -h * tileSize, 0f);
                    cube.name = $"Cube_{h}";
                    cube.SetActive(true);
                    stack.Add(cube);
                }

                tile.SetStack(stack);

                // Default all tiles to height 1
                tile.ApplyHeight(1);

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
            for (int z = 0; z < gridHeight; z++)
                tiles[x, z].ApplyHeight(pattern.GetTile(x, z));

        yield break;
    }
}