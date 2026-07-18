using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ZBase : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public int maxHealth = 100;
    public int currentHealth;

    Rigidbody rb;
    Transform player;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(Transform playerTransform)
    {
        player = playerTransform;
        currentHealth = maxHealth;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 dir = player.position - rb.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
            ZManager.Instance.Kill(this);
    }
}