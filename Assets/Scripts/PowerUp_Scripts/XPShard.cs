using UnityEngine;

public class XPShard : MonoBehaviour
{
    [Header("XP")]
    public int xpValue = 10;

    [Header("Movement")]
    public float pullSpeed = 8f;
    public float acceleration = 20f;
    public float collectDistance = 0.5f;

    bool pulling = false;
    float currentSpeed = 0f;
    Transform target;
    PlayerStats playerStats;

    public void StartPull(Transform playerTransform)
    {
        if (pulling) return;
        pulling = true;
        target = playerTransform;
        playerStats = playerTransform.GetComponent<PlayerStats>();
        currentSpeed = pullSpeed;
    }

    void Update()
    {
        if (!pulling || target == null) return;

        currentSpeed += acceleration * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) <= collectDistance)
        {
            if (playerStats != null) playerStats.AddXP(xpValue);
            Destroy(gameObject);
        }
    }
}