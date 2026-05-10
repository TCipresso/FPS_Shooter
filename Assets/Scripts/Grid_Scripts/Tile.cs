using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public float tileSize = 5f;
    public float animationSpeed = 30f;
    public float columnHeight = 65f;
    public float pitDepth = -30f;

    private float currentY = 0f;
    private float targetY = 0f;
    private Coroutine currentAnimation;

    private float GetTargetY(int height)
    {
        if (height == 0)
            return pitDepth - columnHeight;

        return height * tileSize;
    }

    public void SetHeightImmediate(int height)
    {
        currentY = GetTargetY(height);
        targetY = currentY;

        transform.localPosition = new Vector3(
            transform.localPosition.x,
            currentY - columnHeight,
            transform.localPosition.z
        );
    }

    public void ApplyHeight(int height)
    {
        targetY = GetTargetY(height);

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateTo(targetY));
    }

    private IEnumerator AnimateTo(float target)
    {
        while (!Mathf.Approximately(currentY, target))
        {
            currentY = Mathf.MoveTowards(
                currentY,
                target,
                animationSpeed * Time.deltaTime
            );

            transform.localPosition = new Vector3(
                transform.localPosition.x,
                currentY - columnHeight,
                transform.localPosition.z
            );

            yield return null;
        }

        currentY = target;
    }
}