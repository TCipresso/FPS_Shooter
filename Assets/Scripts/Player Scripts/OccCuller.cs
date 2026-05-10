using UnityEngine;

public class OccCuller : MonoBehaviour
{
    public Camera cam;

    Plane[] frustumPlanes = new Plane[6];
    Renderer[] renderers;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
    }

    void Update()
    {
        GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanes);

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            r.enabled = GeometryUtility.TestPlanesAABB(frustumPlanes, r.bounds);
        }
    }
}