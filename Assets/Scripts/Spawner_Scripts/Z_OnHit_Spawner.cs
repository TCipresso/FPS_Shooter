using UnityEngine;

public class Z_OnHit_Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject zombiePrefab;
    public int spawnCount = 3;
    public float spawnRadius = 2f;

    public void TriggerSpawn()
    {
        if (zombiePrefab == null)
        {
            Debug.LogWarning("[Z_OnHit_Spawner] No zombie prefab assigned!");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            // Pick a random point around the spawner
            Vector2 random2D = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(random2D.x, 0f, random2D.y);

            Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"[Z_OnHit_Spawner] Spawned {spawnCount} zombies.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}