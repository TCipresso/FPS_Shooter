using UnityEngine;

public class ScopeCameraSync : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera scopeCamera;

    void LateUpdate()
    {
        scopeCamera.transform.position = mainCamera.transform.position;
        scopeCamera.transform.rotation = mainCamera.transform.rotation;
        scopeCamera.fieldOfView = mainCamera.fieldOfView;
    }
}