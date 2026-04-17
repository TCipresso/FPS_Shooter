using UnityEngine;

public class XPShardDrop : MonoBehaviour
{
    [Header("Pop Settings")]
    public float minForce = 1.5f;
    public float maxForce = 3f;
    public float upwardForce = 2f;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 force = new Vector3(randomDir.x, 0f, randomDir.y) * Random.Range(minForce, maxForce);
        force.y = upwardForce;

        rb.AddForce(force, ForceMode.Impulse);
    }
}