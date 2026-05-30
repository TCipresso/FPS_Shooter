using UnityEngine;

[CreateAssetMenu(fileName = "NewSpawnZombiesAction", menuName = "Interactable/Spawn Zombies Action")]
public class SpawnZombiesAction : InteractableAction
{
    [Header("Spawn Settings")]
    public string enemyId;
    public int spawnCount = 5;
    public float spawnRadius = 3f;

    public override void Execute(PlayerStats stats)
    {
        if (EnemySpawnManager.Instance == null)
        {
            Debug.LogWarning("[SpawnZombiesAction] No EnemySpawnManager in scene!");
            return;
        }

        EnemySpawnManager.Instance.DebugSpawnNearPlayer(enemyId, spawnCount, spawnRadius);
    }
}