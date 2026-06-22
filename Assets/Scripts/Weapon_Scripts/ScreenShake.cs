using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    Camera cam;
    Vector3 originalLocalPos;
    Coroutine shakeCoroutine;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        originalLocalPos = transform.localPosition;
    }

    public void Shake(float magnitude, float duration, float frequency = 25f)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(DoShake(magnitude, duration, frequency));
    }

    IEnumerator DoShake(float magnitude, float duration, float frequency)
    {
        float elapsed = 0f;
        float interval = 1f / frequency;
        float timer = 0f;
        Vector3 currentOffset = Vector3.zero;
        Vector3 targetOffset = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            timer += Time.deltaTime;

            float t = elapsed / duration;
            float currentMag = magnitude * (1f - t); // falloff

            if (timer >= interval)
            {
                timer = 0f;
                targetOffset = Random.insideUnitSphere * currentMag;
                targetOffset.z = 0f;
            }

            currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * frequency);
            transform.localPosition = originalLocalPos + currentOffset;

            yield return null;
        }

        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }
}