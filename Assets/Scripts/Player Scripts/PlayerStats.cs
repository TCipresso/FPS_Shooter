using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public FPSController controller;
    public FPSLook look;

    [Header("Movement Stats")]
    public float baseSprintSpeed = 10f;
    public float jumpForce = 550f;

    [Header("Mobility")]
    public float mobilityMultiplier = 1f;

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
        Application.targetFrameRate = 1000;
    }

    void OnValidate()
    {
        ApplyStats();
    }

    public void ApplyStats()
    {
        if (controller != null)
        {
            controller.sprintSpeed = baseSprintSpeed * mobilityMultiplier;
            controller.walkSpeed = controller.sprintSpeed * 0.5f;
            controller.jumpForce = jumpForce;
            controller.slideJumpForce = controller.sprintSpeed * (170f / 3f);
            controller.slideBoostSpeed = controller.sprintSpeed * (4f / 3f);
            controller.wallJumpAwayForce = controller.sprintSpeed;
        }
    }

    public void AddMobility(float amount)
    {
        mobilityMultiplier *= (1f + amount);
        ApplyStats();
        Debug.Log($"[PlayerStats] Mobility: {mobilityMultiplier:F2}x | Sprint: {controller.sprintSpeed:F1} | Walk: {controller.walkSpeed:F1}");
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