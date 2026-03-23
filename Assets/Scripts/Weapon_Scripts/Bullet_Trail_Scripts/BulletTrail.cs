using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class BulletTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    public float travelTime = 0.06f;

    TrailRenderer tr;

    void Awake()
    {
        tr = GetComponent<TrailRenderer>();
    }

    public void Fire(Vector3 start, Vector3 end)
    {
        transform.position = start;
        StartCoroutine(Travel(start, end));
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

        // Wait for trail to naturally fade out then destroy
        yield return new WaitForSeconds(tr.time);
        Destroy(gameObject);
    }
}