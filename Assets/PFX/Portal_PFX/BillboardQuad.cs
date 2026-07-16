using UnityEngine;

public class BillboardQuad : MonoBehaviour
{
    public bool lockYAxis = true;

    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        if (lockYAxis)
        {
            Vector3 dir = cam.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(-dir);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}