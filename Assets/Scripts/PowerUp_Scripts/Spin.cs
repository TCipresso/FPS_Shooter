using UnityEngine;

public class Spin : MonoBehaviour
{
    [Header("Spin Settings")]
    public float spinSpeed = 90f;

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
    }
}