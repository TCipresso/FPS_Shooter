using UnityEngine;
using System.Collections.Generic;

public class EnemyPopulationManager : MonoBehaviour
{
    public static EnemyPopulationManager Instance { get; private set; }

    class TrackedEnemy
    {
        public string enemyId;
        public GameObject enemy;
    }

    [Header("Population Cap")]
    public int maxActiveEnemies = 300;

    [Header("Recycle")]
    public float recycleDistance = 70f;
    public float recycleRadiusMin = 15f;
    public float recycleRadiusMax = 30f;
    public LayerMask groundLayerMask;
    public float raycastHeight = 200f;
    public int enemiesCheckedPerFrame = 15;

    List<TrackedEnemy> activeEnemies = new List<TrackedEnemy>();
    Transform player;
    int checkIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (player == null)
        {
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null) player = stats.transform;
            return;
        }

        int checksThisFrame = Mathf.Min(enemiesCheckedPerFrame, activeEnemies.Count);
        float recycleSqr = recycleDistance * recycleDistance;

        for (int n = 0; n < checksThisFrame; n++)
        {
            if (activeEnemies.Count == 0)
                break;

            checkIndex %= activeEnemies.Count;
            TrackedEnemy tracked = activeEnemies[checkIndex];

            if (tracked.enemy == null || !tracked.enemy.activeInHierarchy)
            {
                activeEnemies.RemoveAt(checkIndex);
                continue;
            }

            float sqrDist = (tracked.enemy.transform.position - player.position).sqrMagnitude;
            if (sqrDist > recycleSqr)
            {
                RecycleEnemy(tracked);
                continue;
            }

            checkIndex++;
        }
    }

    public bool HasRoomForMoreEnemies()
    {
        return activeEnemies.Count < maxActiveEnemies;
    }

    public void RegisterSpawn(string enemyId, GameObject enemy)
    {
        TrackedEnemy tracked = new TrackedEnemy { enemyId = enemyId, enemy = enemy };
        activeEnemies.Add(tracked);

        ZombieBase zombie = enemy.GetComponent<ZombieBase>();
        if (zombie != null)
            zombie.OnDeath += () => activeEnemies.Remove(tracked);
    }

    void RecycleEnemy(TrackedEnemy tracked)
    {
        activeEnemies.Remove(tracked);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float dist = Random.Range(recycleRadiusMin, recycleRadiusMax);
            float x = player.position.x + Mathf.Cos(angle) * dist;
            float z = player.position.z + Mathf.Sin(angle) * dist;

            Vector3 rayStart = new Vector3(x, player.position.y + raycastHeight, z);
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayerMask))
                continue;

            EnemySpawnManager.Instance.ReturnEnemy(tracked.enemyId, tracked.enemy);
            GameObject respawned = EnemySpawnManager.Instance.SpawnEnemy(tracked.enemyId, hit.point, Quaternion.identity);
            if (respawned != null)
            {
                RegisterSpawn(tracked.enemyId, respawned);
                if (EnemyDifficultyHandler.Instance != null)
                    EnemyDifficultyHandler.Instance.ApplyScaling(respawned);
            }
            return;
        }
    }
}
