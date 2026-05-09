using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Settings")]
    public float animationSpeed = 30f;
    public float tileSize = 5f;

    private List<GameObject> stack = new List<GameObject>();
    private int currentHeight = 1;
    private Coroutine currentAnimation;

    public void SetStack(List<GameObject> cubes)
    {
        stack = cubes;
    }

    public void ApplyHeight(int height)
    {
        currentHeight = height;

        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateTo(height * tileSize));
    }

    private IEnumerator AnimateTo(float targetY)
    {
        while (!Mathf.Approximately(transform.localPosition.y, targetY))
        {
            float newY = Mathf.MoveTowards(transform.localPosition.y, targetY, animationSpeed * Time.deltaTime);
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
            yield return null;
        }
        transform.localPosition = new Vector3(transform.localPosition.x, targetY, transform.localPosition.z);
    }
}