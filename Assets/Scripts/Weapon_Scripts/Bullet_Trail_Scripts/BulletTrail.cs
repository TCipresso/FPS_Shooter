using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    public float fadeTime = 0.08f;

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    public void Fire(Vector3 start, Vector3 end)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        float elapsed = 0f;
        Color startColor = lr.startColor;
        Color endColor = lr.endColor;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);

            lr.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            lr.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}