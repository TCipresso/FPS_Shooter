using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public FPSController controller;
    public FPSLook look;

    [Header("Movement Stats")]
    public float moveSpeed = 6f;
    public float jumpForce = 550f;

    [Header("Combat Stats")]
    public float reloadSpeed = 1f;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Gold")]
    public int gold = 500;
    public int goldOnHit = 0;
    public int goldOnKill = 100;

    void Awake()
    {
        currentHealth = maxHealth;
        ApplyStats();
        Application.targetFrameRate = 240;
    }

    void OnValidate()
    {
        ApplyStats();
    }

    public void ApplyStats()
    {
        if (controller != null)
        {
            controller.walkSpeed = moveSpeed;
            controller.jumpForce = jumpForce;
        }
    }

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"[PlayerStats] +{amount} gold | Total: {gold}");
    }

    public bool SpendGold(int amount)
    {
        if (gold < amount)
        {
            Debug.Log($"[PlayerStats] Not enough gold. Have: {gold} | Need: {amount}");
            return false;
        }

        gold -= amount;
        Debug.Log($"[PlayerStats] -{amount} gold | Total: {gold}");
        return true;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"[PlayerStats] Took {amount} damage | Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            OnDeath();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"[PlayerStats] Healed {amount} | Health: {currentHealth}/{maxHealth}");
    }

    void OnDeath()
    {
        Debug.Log("[PlayerStats] Player has died.");
    }
}