using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class BulletTrail : MonoBehaviour, IPoolable
{
    [Header("Trail Settings")]
    public float travelTime = 0.06f;
    public string poolKey = "BulletTrail";

    [Header("Heavy Mode")]
    public bool heavyMode = false;

    TrailRenderer tr;
    Coroutine travelCoroutine;
    float originalWidthMultiplier;

    void Awake()
    {
        tr = GetComponent<TrailRenderer>();
        originalWidthMultiplier = tr.widthMultiplier;
    }

    public void OnSpawn()
    {
        tr.widthMultiplier = originalWidthMultiplier;
        tr.Clear();
    }

    public void OnReturnToPool()
    {
        tr.widthMultiplier = originalWidthMultiplier;
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

        if (heavyMode)
        {
            float fadeElapsed = 0f;
            float startWidth = tr.widthMultiplier;
            while (fadeElapsed < tr.time)
            {
                fadeElapsed += Time.deltaTime;
                tr.widthMultiplier = Mathf.Lerp(startWidth, 0f, fadeElapsed / tr.time);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(tr.time);
        }

        BulletPool.Instance.Return(poolKey, gameObject);
    }
}