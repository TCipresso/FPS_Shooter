using UnityEngine;

public class Billboard : MonoBehaviour
{
    Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        transform.forward = -mainCamera.transform.forward;
        transform.rotation *= Quaternion.Euler(90f, 0f, 0f);
    }
}