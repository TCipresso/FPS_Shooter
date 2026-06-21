using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public Color gizmoColor = Color.red;
    public float radius = 0.4f;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawSphere(transform.position + transform.up * radius * 2f, radius * 0.2f);
    }
}