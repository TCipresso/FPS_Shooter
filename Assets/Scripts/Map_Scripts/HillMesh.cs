using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class HillMesh : MonoBehaviour
{
    [System.Serializable]
    public class PropData
    {
        public GameObject prefab;
        [Min(0)] public float weight = 1f;
        public Vector2 scaleRange = Vector2.one;
        public Vector2 xRotationRange = Vector2.zero;
        public Vector2 yRotationRange = new Vector2(0f, 360f);
        public Vector2 zRotationRange = Vector2.zero;
        [Range(0f, 90f)] public float maxSlope = 35f;
        public float verticalOffset = 0f;
    }

    [Header("Terrain")]
    public int size = 50;
    public float spacing = 2f;
    [Range(0.5f, 15f)] public float heightScale = 4f;
    [Range(0.002f, 0.05f)] public float noiseScale = 0.012f;
    public int seed = 1;

    [Header("Variance")]
    [Range(0.001f, 0.02f)] public float macroNoiseScale = 0.004f;
    [Range(0f, 2f)] public float minVariance = 0.6f;
    [Range(0.5f, 3f)] public float maxVariance = 1.8f;

    [Header("Collider")]
    public string groundLayer = "Ground";
    public PhysicsMaterial physicsMaterial;

    [Header("Props")]
    public PropData[] props;
    public float cellSize = 4f;
    [Range(0f, 1f)] public float density = 0.5f;
    public int propSeed = 2;

    private const string ContainerName = "Props (Generated)";

    private void OnEnable() => Generate();

    [ContextMenu("Generate Hills")]
    public void Generate()
    {
        int verts = size + 1;
        var vertices = new Vector3[verts * verts];
        var uvs = new Vector2[verts * verts];

        for (int z = 0; z <= size; z++)
        {
            for (int x = 0; x <= size; x++)
            {
                float wx = x * spacing;
                float wz = z * spacing;
                int idx = z * verts + x;
                vertices[idx] = new Vector3(wx, SampleHeight(wx, wz), wz);
                uvs[idx] = new Vector2((float)x / size, (float)z / size);
            }
        }

        var triangles = new int[size * size * 6];
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

        var mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = vertices,
            triangles = triangles,
            uv = uvs
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        var collider = GetComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.material = physicsMaterial;

        ApplyLayer();
    }

    private void ApplyLayer()
    {
        if (string.IsNullOrEmpty(groundLayer)) return;
        int layer = LayerMask.NameToLayer(groundLayer);
        if (layer == -1)
        {
            Debug.LogWarning($"Layer '{groundLayer}' not found. Add it in Tags & Layers.", this);
            return;
        }
        gameObject.layer = layer;
    }

    private float SampleHeight(float x, float z)
    {
        float ox = x + seed * 1000f;
        float oz = z + seed * 1000f;

        float macro = Mathf.PerlinNoise(ox * macroNoiseScale + 500f, oz * macroNoiseScale + 500f);
        float variance = Mathf.Lerp(minVariance, maxVariance, macro);

        float n1 = Mathf.PerlinNoise(ox * noiseScale, oz * noiseScale);
        float n2 = Mathf.PerlinNoise(ox * noiseScale * 2.2f + 100f, oz * noiseScale * 2.2f + 100f);

        return (n1 * 0.75f + n2 * 0.25f) * heightScale * variance;
    }

    [ContextMenu("Generate Props")]
    public void GenerateProps()
    {
        if (props == null || props.Length == 0)
        {
            Debug.LogWarning("No props configured.", this);
            return;
        }

        int layer = LayerMask.NameToLayer(groundLayer);
        if (layer == -1)
        {
            Debug.LogWarning($"Layer '{groundLayer}' not found.", this);
            return;
        }

        ClearProps();
        var container = GetContainer();
        var rng = new System.Random(propSeed);
        float mapSize = size * spacing;
        Vector3 origin = transform.position;
        float rayHeight = heightScale * Mathf.Max(minVariance, maxVariance) + 50f;
        int placed = 0;

        for (float z = 0; z < mapSize; z += cellSize)
        {
            for (float x = 0; x < mapSize; x += cellSize)
            {
                if (rng.NextDouble() > density) continue;

                var prop = PickProp(rng);
                if (prop?.prefab == null) continue;

                float jitterX = (float)(rng.NextDouble() - 0.5) * cellSize;
                float jitterZ = (float)(rng.NextDouble() - 0.5) * cellSize;

                Vector3 rayStart = new Vector3(
                    origin.x + x + jitterX,
                    origin.y + rayHeight,
                    origin.z + z + jitterZ
                );

                if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayHeight * 2f, 1 << layer))
                    continue;

                if (Vector3.Angle(hit.normal, Vector3.up) > prop.maxSlope) continue;

                var instance = Instantiate(prop.prefab, container);
                instance.transform.position = hit.point + Vector3.up * prop.verticalOffset;

                Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Quaternion rotationOffset = Quaternion.Euler(
                    RandomRange(rng, prop.xRotationRange),
                    RandomRange(rng, prop.yRotationRange),
                    RandomRange(rng, prop.zRotationRange)
                );

                instance.transform.rotation = slopeRotation * rotationOffset;
                instance.transform.localScale = prop.prefab.transform.localScale * RandomRange(rng, prop.scaleRange);
                placed++;
            }
        }

        Debug.Log($"Placed {placed} props.");
    }

    private PropData PickProp(System.Random rng)
    {
        float total = 0f;
        foreach (var p in props)
        {
            if (p?.prefab != null && p.weight > 0)
                total += p.weight;
        }

        if (total <= 0) return null;

        float roll = (float)rng.NextDouble() * total;
        float cumulative = 0f;
        foreach (var p in props)
        {
            if (p?.prefab == null || p.weight <= 0) continue;
            cumulative += p.weight;
            if (roll <= cumulative) return p;
        }
        return null;
    }

    private float RandomRange(System.Random rng, Vector2 range) =>
        Mathf.Lerp(range.x, range.y, (float)rng.NextDouble());

    [ContextMenu("Clear Props")]
    public void ClearProps()
    {
        var container = transform.Find(ContainerName);
        if (container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    private Transform GetContainer()
    {
        var existing = transform.Find(ContainerName);
        if (existing != null) return existing;

        var go = new GameObject(ContainerName);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

#if UNITY_EDITOR
    [ContextMenu("Bake To Static Mesh")]
    public void BakeToStaticMesh()
    {
        Generate();

        string path = AssetDatabase.GenerateUniqueAssetPath($"Assets/GeneratedHillMesh_{gameObject.name}.asset");
        AssetDatabase.CreateAsset(GetComponent<MeshFilter>().sharedMesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Baked hill mesh saved to {path}. Removing HillMesh component - terrain is now static.");

        DestroyImmediate(this);
    }
#endif
}