using UnityEngine;
using System.Collections.Generic;

public class ZombieSwarmManager : MonoBehaviour
{
    public static ZombieSwarmManager Instance { get; private set; }

    public GameObject visualPrefab;
    public int capacity = 3000;
    public float moveSpeed = 3.5f;
    public float attackRange = 1.6f;
    public int attackDamage = 10;
    public float attackCooldown = 1f;
    public int maxHealth = 30;
    public int goldBounty = 5;
    public float separationRadius = 0.8f;
    public float separationStrength = 4f;
    public float gridCellSize = 2f;
    public float walkFrameRate = 8f;

    [Header("Start Spawn")]
    public int startSpawnCount = 300;
    public float startSpawnRadius = 15f;

    [Header("Ground")]
    public LayerMask groundLayer;
    public float groundRayHeight = 5f;
    public float groundRayDistance = 20f;
    public int groundResampleSpread = 8;

    Transform player;
    PlayerStats playerStats;

    Vector3[] positions;
    int[] health;
    byte[] state;
    float[] attackCooldownTimer;
    float[] animTimer;
    int[] animFrame;
    Transform[] visualTransforms;
    SwarmZombieVisual[] visuals;
    int[] activeListPosition;

    Stack<int> freeIndices;
    List<int> activeIndices;

    Dictionary<long, List<int>> grid;
    Stack<List<int>> gridListPool;
    List<int> neighborBuffer = new List<int>(32);

    void Awake()
    {
        Instance = this;
        positions = new Vector3[capacity];
        health = new int[capacity];
        state = new byte[capacity];
        attackCooldownTimer = new float[capacity];
        animTimer = new float[capacity];
        animFrame = new int[capacity];
        visualTransforms = new Transform[capacity];
        visuals = new SwarmZombieVisual[capacity];
        activeListPosition = new int[capacity];
        freeIndices = new Stack<int>(capacity);
        activeIndices = new List<int>(capacity);
        grid = new Dictionary<long, List<int>>();
        gridListPool = new Stack<List<int>>();

        for (int i = capacity - 1; i >= 0; i--)
        {
            GameObject go = Instantiate(visualPrefab, transform);
            go.SetActive(false);
            visualTransforms[i] = go.transform;
            visuals[i] = go.GetComponent<SwarmZombieVisual>();
            activeListPosition[i] = -1;
            freeIndices.Push(i);
        }
    }

    void Start()
    {
        PlayerStats ps = FindFirstObjectByType<PlayerStats>();
        if (ps != null)
        {
            playerStats = ps;
            player = ps.transform;
        }

        Vector3 center = player != null ? player.position : transform.position;
        for (int n = 0; n < startSpawnCount; n++)
        {
            Vector2 offset = Random.insideUnitCircle * startSpawnRadius;
            Vector3 spawnPos = center + new Vector3(offset.x, 0f, offset.y);
            SpawnZombie(spawnPos);
        }
    }

    float SampleGroundHeight(Vector3 pos, float fallback)
    {
        if (groundLayer == 0) return fallback;
        Vector3 rayOrigin = new Vector3(pos.x, pos.y + groundRayHeight, pos.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayer))
            return hit.point.y;
        return fallback;
    }

    public int SpawnZombie(Vector3 position)
    {
        if (freeIndices.Count == 0) return -1;
        int i = freeIndices.Pop();

        position.y = SampleGroundHeight(position, position.y);

        positions[i] = position;
        health[i] = maxHealth;
        state[i] = 1;
        attackCooldownTimer[i] = 0f;
        animTimer[i] = Random.value;
        animFrame[i] = 0;
        visualTransforms[i].position = position;
        visualTransforms[i].gameObject.SetActive(true);

        activeIndices.Add(i);
        activeListPosition[i] = activeIndices.Count - 1;

        return i;
    }

    void Despawn(int i)
    {
        state[i] = 0;
        visualTransforms[i].gameObject.SetActive(false);

        int pos = activeListPosition[i];
        int lastIndex = activeIndices.Count - 1;
        int lastValue = activeIndices[lastIndex];

        activeIndices[pos] = lastValue;
        activeListPosition[lastValue] = pos;
        activeIndices.RemoveAt(lastIndex);
        activeListPosition[i] = -1;

        freeIndices.Push(i);
    }

    public void TakeDamage(int i, int amount)
    {
        if (i < 0 || i >= capacity || state[i] == 0) return;
        health[i] -= amount;
        if (health[i] <= 0)
        {
            if (KillMarkerPool.Instance != null)
                KillMarkerPool.Instance.Spawn(positions[i], goldBounty);
            if (playerStats != null)
                playerStats.AddGold(goldBounty);
            Despawn(i);
        }
    }

    long GridKey(Vector3 pos)
    {
        int gx = Mathf.FloorToInt(pos.x / gridCellSize);
        int gz = Mathf.FloorToInt(pos.z / gridCellSize);
        return ((long)gx << 32) ^ (uint)gz;
    }

    void RebuildGrid()
    {
        foreach (var kvp in grid)
        {
            kvp.Value.Clear();
            gridListPool.Push(kvp.Value);
        }
        grid.Clear();

        for (int idx = 0; idx < activeIndices.Count; idx++)
        {
            int i = activeIndices[idx];
            long key = GridKey(positions[i]);
            if (!grid.TryGetValue(key, out List<int> list))
            {
                list = gridListPool.Count > 0 ? gridListPool.Pop() : new List<int>(8);
                grid[key] = list;
            }
            list.Add(i);
        }
    }

    void GetNeighbors(Vector3 pos)
    {
        neighborBuffer.Clear();
        int gx = Mathf.FloorToInt(pos.x / gridCellSize);
        int gz = Mathf.FloorToInt(pos.z / gridCellSize);
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                long key = ((long)(gx + dx) << 32) ^ (uint)(gz + dz);
                if (grid.TryGetValue(key, out List<int> list))
                    neighborBuffer.AddRange(list);
            }
        }
    }

    void Update()
    {
        if (player == null)
        {
            PlayerStats ps = FindFirstObjectByType<PlayerStats>();
            if (ps == null) return;
            playerStats = ps;
            player = ps.transform;
        }

        RebuildGrid();

        float dt = Time.deltaTime;
        Vector3 playerPos = player.position;
        int frameSlice = groundResampleSpread > 0 ? Time.frameCount % groundResampleSpread : -1;

        for (int idx = activeIndices.Count - 1; idx >= 0; idx--)
        {
            int i = activeIndices[idx];

            Vector3 toPlayer = playerPos - positions[i];
            toPlayer.y = 0f;
            float distSqr = toPlayer.sqrMagnitude;

            if (attackCooldownTimer[i] > 0f)
                attackCooldownTimer[i] -= dt;

            GetNeighbors(positions[i]);
            Vector3 separation = Vector3.zero;
            for (int n = 0; n < neighborBuffer.Count; n++)
            {
                int other = neighborBuffer[n];
                if (other == i) continue;
                Vector3 away = positions[i] - positions[other];
                away.y = 0f;
                float d = away.magnitude;
                if (d < separationRadius)
                {
                    Vector3 pushDir = d > 0.0001f ? away.normalized : Random.insideUnitCircle.normalized;
                    separation += pushDir * (separationRadius - d);
                }
            }
            Vector3 separationMove = separation * separationStrength * dt;

            if (distSqr <= attackRange * attackRange)
            {
                state[i] = 2;
                if (attackCooldownTimer[i] <= 0f)
                {
                    attackCooldownTimer[i] = attackCooldown;
                    if (playerStats != null)
                        playerStats.TakeDamage(attackDamage);
                }
                positions[i] += separationMove;
            }
            else
            {
                state[i] = 1;
                Vector3 dir = toPlayer.normalized;
                Vector3 seekMove = dir * moveSpeed * dt;
                positions[i] += seekMove + separationMove;
            }

            if (groundLayer != 0 && frameSlice >= 0 && (i % groundResampleSpread) == frameSlice)
            {
                float targetY = SampleGroundHeight(positions[i], positions[i].y);
                positions[i] = new Vector3(positions[i].x, targetY, positions[i].z);
            }

            visualTransforms[i].position = positions[i];
            if (distSqr > 0.0001f)
            {
                Vector3 lookDir = toPlayer;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.0001f)
                    visualTransforms[i].rotation = Quaternion.LookRotation(lookDir);
            }

            if (visuals[i] != null)
            {
                if (state[i] == 2)
                {
                    visuals[i].SetAttackFrame(animFrame[i]);
                }
                else
                {
                    animTimer[i] += dt * walkFrameRate;
                    if (animTimer[i] >= 1f)
                    {
                        animTimer[i] -= 1f;
                        animFrame[i]++;
                    }
                    visuals[i].SetWalkFrame(animFrame[i]);
                }
            }
        }
    }

    public int FindNearestInRadius(Vector3 origin, Vector3 direction, float maxDistance, float hitRadius)
    {
        int best = -1;
        float bestDist = maxDistance;
        for (int idx = 0; idx < activeIndices.Count; idx++)
        {
            int i = activeIndices[idx];
            Vector3 toZombie = positions[i] - origin;
            float along = Vector3.Dot(toZombie, direction);
            if (along < 0f || along > bestDist) continue;
            Vector3 closest = origin + direction * along;
            float perpDist = Vector3.Distance(closest, positions[i]);
            if (perpDist <= hitRadius)
            {
                bestDist = along;
                best = i;
            }
        }
        return best;
    }

    public void FindAllInSphere(Vector3 center, float radius, List<int> results)
    {
        results.Clear();
        float radiusSqr = radius * radius;
        for (int idx = 0; idx < activeIndices.Count; idx++)
        {
            int i = activeIndices[idx];
            if ((positions[i] - center).sqrMagnitude <= radiusSqr)
                results.Add(i);
        }
    }

    public Vector3 GetPosition(int i) => positions[i];
    public bool IsAlive(int i) => i >= 0 && i < capacity && state[i] != 0;
    public int ActiveCount => activeIndices.Count;
}