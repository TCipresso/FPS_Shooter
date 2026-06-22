using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    Camera cam;
    Vector3 originalLocalPos;

    class ShakeInstance
    {
        public Vector3 offset;
    }

    readonly List<ShakeInstance> activeShakes = new List<ShakeInstance>();

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        originalLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        Vector3 totalOffset = Vector3.zero;

        for (int i = 0; i < activeShakes.Count; i++)
            totalOffset += activeShakes[i].offset;

        transform.localPosition = originalLocalPos + totalOffset;
    }

    public void Shake(float magnitude, float duration, float frequency = 25f)
    {
        StartCoroutine(DoShake(magnitude, duration, frequency));
    }

    IEnumerator DoShake(float magnitude, float duration, float frequency)
    {
        ShakeInstance shake = new ShakeInstance();
        activeShakes.Add(shake);

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
            float currentMag = magnitude * (1f - t);

            if (timer >= interval)
            {
                timer = 0f;
                targetOffset = Random.insideUnitSphere * currentMag;
                targetOffset.z = 0f;
            }

            currentOffset = Vector3.Lerp(
                currentOffset,
                targetOffset,
                Time.deltaTime * frequency
            );

            shake.offset = currentOffset;

            yield return null;
        }

        activeShakes.Remove(shake);
    }
}