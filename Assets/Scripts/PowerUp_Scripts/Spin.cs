using UnityEngine;
public class Spin : MonoBehaviour
{
    public enum SpinAxis { X, Y, Z }

    [Header("Spin Settings")]
    public float spinSpeed = 90f;
    public SpinAxis spinAxis = SpinAxis.Y;

    [Header("Bob Settings")]
    public float bobHeight = 0.25f;
    public float bobSpeed = 2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        Vector3 axis = spinAxis == SpinAxis.X ? Vector3.right
                     : spinAxis == SpinAxis.Y ? Vector3.up
                     : Vector3.forward;

        transform.Rotate(axis, spinSpeed * Time.deltaTime, Space.Self);

        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}