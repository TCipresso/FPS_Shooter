using UnityEngine;

public class RangedGrunt : ZombieBase
{
    [Header("Ranged Attack")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float preferredRange = 10f;
    public float retreatRange = 4f;
    public float minFireRange = 3f;
    public float maxFireRange = 18f;

    [Header("Aim")]
    public float aimHeightOffset = 0.5f;

    RangedProjectile[] pool;
    int poolSize = 10;
    int poolIndex = 0;

    protected override void Awake()
    {
        isGrunt = false;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        BuildPool();
    }

    void BuildPool()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[RangedGrunt] No projectile prefab assigned.");
            return;
        }

        pool = new RangedProjectile[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(projectilePrefab);
            go.SetActive(false);
            pool[i] = go.GetComponent<RangedProjectile>();
        }
    }

    protected override void UpdateBehaviour()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        FacePlayer();
        UpdateMovement(dist);
        TryFire(dist);
    }

    void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            8f * Time.deltaTime
        );
    }

    void UpdateMovement(float dist)
    {
        if (!agent.isOnNavMesh) return;

        if (dist < retreatRange)
        {
            Vector3 retreatPos = transform.position + (transform.position - player.position).normalized * preferredRange;
            agent.SetDestination(retreatPos);
            agent.isStopped = false;
        }
        else if (dist > preferredRange)
        {
            agent.SetDestination(player.position);
            agent.isStopped = false;
        }
        else
        {
            agent.isStopped = true;
        }
    }

    void TryFire(float dist)
    {
        if (pool == null) return;
        if (dist < minFireRange || dist > maxFireRange) return;
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (!HasLineOfSight()) return;

        lastAttackTime = Time.time;
        Fire();
    }

    bool HasLineOfSight()
    {
        if (firePoint == null || player == null) return false;
        Vector3 origin = firePoint.position;
        Vector3 target = player.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (target - origin).normalized;
        float dist = Vector3.Distance(origin, target);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist))
            return hit.collider.GetComponent<PlayerStats>() != null;

        return false;
    }

    void Fire()
    {
        if (pool == null || firePoint == null) return;

        RangedProjectile proj = pool[poolIndex % poolSize];
        poolIndex++;

        Vector3 target = player.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (target - firePoint.position).normalized;

        proj.damage = attackDamage;
        proj.owner = playerStats;
        proj.transform.position = firePoint.position;
        proj.transform.rotation = Quaternion.LookRotation(dir);
        proj.gameObject.SetActive(true);
    }
}