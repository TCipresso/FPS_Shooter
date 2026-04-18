using UnityEngine;

public class PickupZone : MonoBehaviour
{
    public Transform player;

    SphereCollider col;
    float baseRadius;

    void Awake()
    {
        col = GetComponent<SphereCollider>();
        baseRadius = col.radius;
    }

    public void ApplyRange(float multiplier)
    {
        if (col != null)
            col.radius = baseRadius * multiplier;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("XP_Shard")) return;
        XPShard shard = other.GetComponent<XPShard>();
        if (shard != null) shard.StartPull(player);
    }
}