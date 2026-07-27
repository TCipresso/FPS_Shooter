using UnityEngine;

public class PlayerZombieBridge : MonoBehaviour
{
    PlayerStats stats;

    public bool IsTargetable
    {
        get
        {
            if (stats == null) return false;
            return !stats.IsDown && !stats.IsGameOver;
        }
    }

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    void OnEnable()
    {
        ZombiePlayerRegistry.Instance.Register(this);
    }

    void OnDisable()
    {
        if (ZombiePlayerRegistry.Instance != null)
            ZombiePlayerRegistry.Instance.Unregister(this);
    }

    public void ApplyContactDamage(int amount)
    {
        if (stats == null) return;
        stats.TakeDamage(amount);
    }

    public void GrantHitGold()
    {
        if (stats == null) return;
        stats.AddGold(stats.goldOnHit);
    }

    public void GrantKillGold()
    {
        if (stats == null) return;
        stats.AddGold(stats.goldOnKill);
    }
}