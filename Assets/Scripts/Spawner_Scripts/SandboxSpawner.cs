using UnityEngine;

public class SandboxSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public string enemyId;
    public Transform spawnPoint;

    [Header("Trigger Cooldown")]
    [Tooltip("Prevents a single shotgun blast or rapid fire from spawning multiple zombies in one hit.")]
    public float triggerCooldown = 0.5f;

    float cooldownTimer;

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public void TriggerSpawn()
    {
        if (cooldownTimer > 0f) return;

        if (spawnPoint == null)
        {
            Debug.LogWarning($"[SandboxSpawner] No spawnPoint assigned on {gameObject.name}.");
            return;
        }

        if (EnemySpawnManager.Instance == null)
        {
            Debug.LogWarning("[SandboxSpawner] No EnemySpawnManager instance found in scene.");
            return;
        }

        cooldownTimer = triggerCooldown;
        EnemySpawnManager.Instance.SpawnEnemy(enemyId, spawnPoint);
    }
}