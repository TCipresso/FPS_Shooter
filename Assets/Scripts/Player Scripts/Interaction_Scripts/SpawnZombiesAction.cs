using UnityEngine;

[CreateAssetMenu(fileName = "NewSpawnZombiesAction", menuName = "Interactable/Spawn Zombies Action")]
public class SpawnZombiesAction : InteractableAction
{
    [Header("Spawn Settings")]
    public GameObject zombiePrefab;
    public int spawnCount = 5;
    public float spawnRadius = 3f;

    public override void Execute(PlayerStats stats)
    {
        if (zombiePrefab == null)
        {
            Debug.LogWarning("[SpawnZombiesAction] No zombie prefab assigned!");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = stats.transform.position + new Vector3(random2D.x, 0f, random2D.y);
            GameObject.Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"[SpawnZombiesAction] Spawned {spawnCount} zombies.");
    }
}