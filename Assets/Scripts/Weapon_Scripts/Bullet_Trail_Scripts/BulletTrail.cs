using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class BulletTrail : MonoBehaviour, IPoolable
{
    [Header("Trail Settings")]
    public float travelTime = 0.06f;
    public string poolKey = "BulletTrail";

    TrailRenderer tr;

    void Awake()
    {
        tr = GetComponent<TrailRenderer>();
    }

    public void OnSpawn()
    {
        tr.Clear();
    }

    public void OnReturnToPool()
    {
        tr.Clear();
        BulletTrailManager.Instance.Unregister(gameObject);
    }

    public void Fire(Vector3 start, Vector3 end)
    {
        transform.position = start;
        BulletTrailManager.Instance.Register(gameObject, transform, tr, poolKey, start, end, travelTime);
    }
}