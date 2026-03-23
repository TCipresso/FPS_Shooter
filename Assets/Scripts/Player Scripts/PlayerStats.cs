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

    [Header("Points")]
    public int points = 500;
    public int pointsOnHit = 10;

    void Awake()
    {
        currentHealth = maxHealth;
        ApplyStats();
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

    public void AddPoints(int amount)
    {
        points += amount;
        Debug.Log($"[PlayerStats] +{amount} points | Total: {points}");
    }

    public bool SpendPoints(int amount)
    {
        if (points < amount)
        {
            Debug.Log($"[PlayerStats] Not enough points. Have: {points} | Need: {amount}");
            return false;
        }

        points -= amount;
        Debug.Log($"[PlayerStats] -{amount} points | Total: {points}");
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