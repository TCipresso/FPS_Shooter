using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public float animationSpeed = 30f;

    private float currentY = 0f;
    private float targetY = 0f;
    private Coroutine currentAnimation;

    // Sets tile to exact world Y immediately, no animation
    public void SetHeightImmediate(float worldY)
    {
        StopAllCoroutines();
        currentY = worldY;
        targetY = worldY;
        Vector3 p = transform.localPosition;
        transform.localPosition = new Vector3(p.x, worldY, p.z);
        enabled = false;
    }

    // Smoothly animates tile to exact world Y
    public void ApplyHeight(float worldY)
    {
        targetY = worldY;
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateTo(targetY));
    }

    private IEnumerator AnimateTo(float target)
    {
        enabled = true;
        while (!Mathf.Approximately(currentY, target))
        {
            currentY = Mathf.MoveTowards(currentY, target, animationSpeed * Time.deltaTime);
            Vector3 p = transform.localPosition;
            transform.localPosition = new Vector3(p.x, currentY, p.z);
            yield return null;
        }
        currentY = target;
        Vector3 fp = transform.localPosition;
        transform.localPosition = new Vector3(fp.x, currentY, fp.z);
        enabled = false;
    }
}