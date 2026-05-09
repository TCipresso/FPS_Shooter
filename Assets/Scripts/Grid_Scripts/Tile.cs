using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public float tileSize = 5f;
    public float animationSpeed = 30f;
    public float columnHeight = 65f; // 13 cubes * 5 units

    private float currentY = 0f;
    private float targetY = 0f;
    private Coroutine currentAnimation;

    public void SetHeightImmediate(int height)
    {
        targetY = height * tileSize;
        currentY = targetY;
        transform.localPosition = new Vector3(transform.localPosition.x, currentY - columnHeight, transform.localPosition.z);
    }

    public void ApplyHeight(int height)
    {
        targetY = height * tileSize;
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateTo(targetY));
    }

    private IEnumerator AnimateTo(float target)
    {
        while (!Mathf.Approximately(currentY, target))
        {
            currentY = Mathf.MoveTowards(currentY, target, animationSpeed * Time.deltaTime);
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