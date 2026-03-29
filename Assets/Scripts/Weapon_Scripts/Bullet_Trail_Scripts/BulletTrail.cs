using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class BulletTrail : MonoBehaviour, IPoolable
{
    [Header("Trail Settings")]
    public float travelTime = 0.06f;
    public string poolKey = "BulletTrail";

    TrailRenderer tr;
    Coroutine travelCoroutine;

    void Awake()
    {
        tr = GetComponent<TrailRenderer>();
    }

    public void OnSpawn()
    {
        // Clear any leftover trail from previous use
        tr.Clear();
    }

    public void OnReturnToPool()
    {
        tr.Clear();
        if (travelCoroutine != null)
        {
            StopCoroutine(travelCoroutine);
            travelCoroutine = null;
        }
    }

    public void Fire(Vector3 start, Vector3 end)
    {
        transform.position = start;
        if (travelCoroutine != null)
            StopCoroutine(travelCoroutine);
        travelCoroutine = StartCoroutine(Travel(start, end));
    }

    IEnumerator Travel(Vector3 start, Vector3 end)
    {
        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelTime;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;

        // Wait for trail to fade then return to pool
        yield return new WaitForSeconds(tr.time);

        BulletPool.Instance.Return(poolKey, gameObject);
    }
}