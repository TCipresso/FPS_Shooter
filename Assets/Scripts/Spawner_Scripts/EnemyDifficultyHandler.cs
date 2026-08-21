using UnityEngine;

public class EnemyDifficultyHandler : MonoBehaviour
{
    public static EnemyDifficultyHandler Instance { get; private set; }

    [Header("Difficulty Interval (shared by spawn rate + stats)")]
    public float difficultyInterval = 15f;

    [Header("Spawn Rate")]
    public float baseSpawnRate = 0.5f;
    public float maxSpawnRate = 3f;
    public float spawnRateGrowthAmount = 0.1f;

    [Header("Stat Scaling")]
    public float healthMultiplierPerInterval = 1.05f;
    public float damageMultiplierPerInterval = 1.04f;
    public float speedMultiplierPerInterval = 1.02f;
    public float maxStatMultiplier = 5f;

    float startTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        startTime = Time.time;
    }

    public float GetSpawnInterval()
    {
        float elapsed = Time.time - startTime;
        int growthSteps = Mathf.FloorToInt(elapsed / Mathf.Max(difficultyInterval, 0.01f));
        float currentRate = Mathf.Min(baseSpawnRate + growthSteps * spawnRateGrowthAmount, maxSpawnRate);
        return 1f / Mathf.Max(currentRate, 0.01f);
    }

    public void ApplyScaling(GameObject enemy)
    {
        ZombieBase zombie = enemy.GetComponent<ZombieBase>();
        if (zombie == null)
            return;

        float steps = (Time.time - startTime) / Mathf.Max(difficultyInterval, 0.01f);

        float healthMult = Mathf.Min(Mathf.Pow(healthMultiplierPerInterval, steps), maxStatMultiplier);
        float damageMult = Mathf.Min(Mathf.Pow(damageMultiplierPerInterval, steps), maxStatMultiplier);
        float speedMult = Mathf.Min(Mathf.Pow(speedMultiplierPerInterval, steps), maxStatMultiplier);

        zombie.maxHealth = Mathf.RoundToInt(zombie.maxHealth * healthMult);
        zombie.currentHealth = zombie.maxHealth;
        zombie.attackDamage = Mathf.RoundToInt(zombie.attackDamage * damageMult);
        zombie.moveSpeed *= speedMult;
    }
}