using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Generates a flat grid of verts and pushes their height up/down with noise,
// so it looks like rolling hills instead of a flat plane. This is your
// static prototype ground. The "seed" field is the ONLY thing that changes
// later when you add real random generation - swap the hardcoded 1 for
// Random.Range(int.MinValue, int.MaxValue) and everything else stays as-is.

// ExecuteAlways: build the mesh in the Editor too, not just at Play time,
// so the hills are actually visible in the Scene view and you can drag
// trees/rocks/props onto them by hand before ever pressing Play.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class HillMesh : MonoBehaviour
{
    [Header("Grid")]
    public int size = 50;          // 50x50 squares of ground
    public float spacing = 2f;     // world units between verts

    [Header("Hills")]
    [Range(0.5f, 15f)]
    public float heightScale = 4f;   // how tall the hills get, before variance below scales it further
    [Range(0.002f, 0.05f)]
    public float noiseScale = 0.012f; // LOWER = bigger, smoother, fewer hills. HIGHER = more frequent bumps.
    public int seed = 1;             // static for now, will be randomized later

    [Header("Hill Variance (occasional bigger, wider hills)")]
    [Range(0.001f, 0.02f)]
    public float macroNoiseScale = 0.004f; // very low frequency - picks WHERE the bigger hill regions land
    [Range(0f, 2f)]
    public float minHillVariance = 0.6f;   // hill height multiplier in the gentler regions
    [Range(0.5f, 3f)]
    public float maxHillVariance = 1.8f;   // hill height multiplier in the "bigger hill" regions

    [Header("Collider")]
    public string groundLayerName = "Ground"; // must match a layer name that already exists in Project Settings > Tags and Layers
    public PhysicsMaterial physicsMaterial;    // drag your physics material asset in here

    [Header("Props")]
    public GameObject[] propPrefabs;        // trees/rocks/etc - one is picked at random per spot
    public float propCellSize = 4f;         // grid spacing between placement attempts (smaller = denser, slower)
    [Range(0f, 1f)]
    public float propDensity = 0.5f;        // chance any given grid cell actually spawns something
    public int propSeed = 2;                // separate from the terrain seed so prop layout can vary independently
    [Range(0f, 90f)]
    public float propMaxSlopeDegrees = 35f; // skip spots steeper than this - keeps props off cliff faces
    public Vector2 propScaleRange = new Vector2(0.85f, 1.15f); // random size variation per instance

    const string PropsContainerName = "Props (Generated)";

    // Auto-builds once when the object loads/recompiles so it's never blank,
    // but does NOT regenerate on every Inspector edit anymore - use the
    // "Generate" button in the Inspector (or the context menu) for that, so
    // typing in a field doesn't rebuild the mesh on every keystroke.
    void OnEnable()
    {
        Generate();
    }

    [ContextMenu("Generate Hills")]
    public void Generate()
    {
        int verts = size + 1;
        Vector3[] vertices = new Vector3[verts * verts];
        Vector2[] uvs = new Vector2[verts * verts];

        for (int z = 0; z <= size; z++)
        {
            for (int x = 0; x <= size; x++)
            {
                float worldX = x * spacing;
                float worldZ = z * spacing;

                float height = SampleHeight(worldX, worldZ);

                vertices[z * verts + x] = new Vector3(worldX, height, worldZ);
                uvs[z * verts + x] = new Vector2((float)x / size, (float)z / size);
            }
        }

        int[] triangles = new int[size * size * 6];
        int t = 0;
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = z * verts + x;

                triangles[t++] = i;
                triangles[t++] = i + verts;
                triangles[t++] = i + 1;

                triangles[t++] = i + 1;
                triangles[t++] = i + verts;
                triangles[t++] = i + verts + 1;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        // Collider matters for both: hand-placing objects with a "drop to
        // ground" raycast now, and the procedural placer raycasting down to
        // find surface height later. Same mesh, same collider, no rework.
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.material = physicsMaterial;

        ApplyGroundLayer();
    }

    void ApplyGroundLayer()
    {
        if (string.IsNullOrEmpty(groundLayerName))
            return;

        int layer = LayerMask.NameToLayer(groundLayerName);
        if (layer == -1)
        {
            Debug.LogWarning($"HillMesh: layer \"{groundLayerName}\" doesn't exist. " +
                "Add it in Project Settings > Tags and Layers, or fix the name in the Inspector.", this);
            return;
        }

        gameObject.layer = layer;
    }

    // Scatters propPrefabs across this same ground by raycasting down onto
    // it - the exact same "find the real surface" trick used for enemy
    // spawn height, just run once across the whole map instead of live
    // around the player. Run "Generate Hills" first (or just after this
    // one - Generate() at the top already ran if you clicked this button
    // second) so the collider matches whatever the sliders currently show.
    [ContextMenu("Generate Props")]
    public void GenerateProps()
    {
        if (propPrefabs == null || propPrefabs.Length == 0)
        {
            Debug.LogWarning("HillMesh: assign at least one prop prefab before generating props.", this);
            return;
        }

        int layer = LayerMask.NameToLayer(groundLayerName);
        if (layer == -1)
        {
            Debug.LogWarning($"HillMesh: ground layer \"{groundLayerName}\" doesn't exist. " +
                "Check Project Settings > Tags and Layers.", this);
            return;
        }
        int layerMask = 1 << layer;

        ClearProps();
        Transform container = GetOrCreatePropsContainer();

        System.Random rng = new System.Random(propSeed);
        float mapWidth = size * spacing;
        Vector3 origin = transform.position;
        float raycastHeight = heightScale * Mathf.Max(minHillVariance, maxHillVariance) + 50f;

        int placed = 0;
        for (float z = 0; z < mapWidth; z += propCellSize)
        {
            for (float x = 0; x < mapWidth; x += propCellSize)
            {
                if (rng.NextDouble() > propDensity)
                    continue;

                float jitterX = (float)(rng.NextDouble() - 0.5) * propCellSize;
                float jitterZ = (float)(rng.NextDouble() - 0.5) * propCellSize;
                float worldX = origin.x + x + jitterX;
                float worldZ = origin.z + z + jitterZ;

                Vector3 rayStart = new Vector3(worldX, origin.y + raycastHeight, worldZ);
                if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, layerMask))
                    continue;

                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > propMaxSlopeDegrees)
                    continue;

                GameObject prefab = propPrefabs[rng.Next(propPrefabs.Length)];
                GameObject instance = Instantiate(prefab, container);
                instance.transform.position = hit.point;

                Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Quaternion spin = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);
                instance.transform.rotation = slopeRotation * spin;

                float scale = Mathf.Lerp(propScaleRange.x, propScaleRange.y, (float)rng.NextDouble());
                instance.transform.localScale = Vector3.one * scale;

                placed++;
            }
        }

        Debug.Log($"HillMesh: placed {placed} props.");
    }

    [ContextMenu("Clear Props")]
    public void ClearProps()
    {
        Transform container = transform.Find(PropsContainerName);
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    Transform GetOrCreatePropsContainer()
    {
        Transform existing = transform.Find(PropsContainerName);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(PropsContainerName);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    // This is the actual "make some hills bigger/wider, none of them
    // spiky" logic. Two things are layered:
    //
    // 1. "macro" - a VERY low frequency noise sampled once per vertex. It
    //    changes slowly across the whole map, so it defines broad regions
    //    (tens of units across) rather than per-hill detail. We use it to
    //    scale the regular hill height up or down - that's what gives you
    //    "sometimes there's a noticeably bigger, wider hill" without ever
    //    introducing a sharp feature, since it's still smooth Perlin noise,
    //    just changing gradually.
    //
    // 2. Two blended octaves of the normal hill noise, for a little bit of
    //    natural irregularity in the hill shapes themselves.
    //
    // Nothing here uses max()/ridge operations or steep exponents, which
    // are what actually create sharp mountain-style peaks - everything is
    // straight Perlin blended together, so hilltops stay rounded no matter
    // how tall they get.
    float SampleHeight(float worldX, float worldZ)
    {
        float offsetX = worldX + seed * 1000f;
        float offsetZ = worldZ + seed * 1000f;

        float macro = Mathf.PerlinNoise(offsetX * macroNoiseScale + 500f, offsetZ * macroNoiseScale + 500f);
        float variance = Mathf.Lerp(minHillVariance, maxHillVariance, macro);

        float n1 = Mathf.PerlinNoise(offsetX * noiseScale, offsetZ * noiseScale);
        float n2 = Mathf.PerlinNoise(offsetX * noiseScale * 2.2f + 100f, offsetZ * noiseScale * 2.2f + 100f);
        float combined = n1 * 0.75f + n2 * 0.25f;

        return combined * heightScale * variance;
    }

#if UNITY_EDITOR
    // Right-click the component header in the Inspector -> "Bake To Static
    // Mesh". Use this once the hills look right and you're ready to start
    // placing props. It saves the current mesh as a real .asset file on
    // disk, then deletes THIS script off the object. After that it's just
    // a plain MeshFilter/MeshRenderer/MeshCollider like any other scene
    // object - it will never regenerate again, so nothing you place on it
    // moves out from under you. (Keep a backup copy of this GameObject as a
    // prefab first if you want an easy way to re-roll a fresh one later.)
    // Note: any props you generated stay exactly where they are - they're
    // separate GameObjects under "Props (Generated)", untouched by baking.
    [ContextMenu("Bake To Static Mesh (Locks In Current Hills)")]
    void BakeToStaticMesh()
    {
        Generate(); // make sure the baked mesh matches whatever the sliders currently show

        Mesh bakedMesh = GetComponent<MeshFilter>().sharedMesh;

        string path = AssetDatabase.GenerateUniqueAssetPath(
            "Assets/GeneratedHillMesh_" + gameObject.name + ".asset");
        AssetDatabase.CreateAsset(bakedMesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log("Baked hill mesh saved to " + path +
            ". Removing HillMesh component - this ground is now static and won't regenerate.");

        DestroyImmediate(this);
    }
#endif
}