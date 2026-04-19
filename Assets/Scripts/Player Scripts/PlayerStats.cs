using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public FPSController controller;
    public FPSLook look;
    public WeaponInventory weaponInventory;
    public PickupZone pickupZone;
    public AugmentDraftUI augmentDraftUI;

    [Header("Movement Stats")]
    public float baseSprintSpeed = 10f;
    public float jumpForce = 550f;

    [Header("Mobility")]
    public float mobilityMultiplier = 1f;

    [Header("Combat Stats")]
    public float reloadSpeed = 1f;
    [Range(0f, 5f)] public float attackSpeed = 1f;
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critMultiplier = 1.5f;
    public float luck = 0f;
    public int extraMagazine = 0;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Gold")]
    public int gold = 500;
    public int baseGoldOnHit = 0;
    public int baseGoldOnKill = 100;
    public float goldGainMultiplier = 1f;
    public int goldOnHit => Mathf.RoundToInt(baseGoldOnHit * goldGainMultiplier);
    public int goldOnKill => Mathf.RoundToInt(baseGoldOnKill * goldGainMultiplier);

    void Awake()
    {
        currentHealth = maxHealth;
        ApplyStats();
        Application.targetFrameRate = 260;
    }

    void Start()
    {
        if (pickupZone != null) pickupZone.ApplyRange(pickupRange);
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

    public void AddCritChance(float amount)
    {
        critChance += amount;
        critChance = Mathf.Clamp01(critChance);
        ApplyCombatStats();
        Debug.Log($"[PlayerStats] Crit Chance: {critChance * 100:F0}%");
    }

    public void AddCritMultiplier(float amount)
    {
        critMultiplier += amount;
        ApplyCombatStats();
        Debug.Log($"[PlayerStats] Crit Multiplier: {critMultiplier:F2}x");
    }

    void ApplyCombatStats()
    {
        WeaponBase wb = weaponInventory?.GetActiveWeaponBase();
        if (wb != null)
        {
            wb.critChance = critChance;
            wb.critMultiplier = critMultiplier;
        }
    }

    public void AddReloadSpeed(float amount)
    {
        reloadSpeed *= (1f + amount);
        Debug.Log($"[PlayerStats] Reload Speed: {reloadSpeed:F2}x");
    }

    public void AddAttackSpeed(float amount)
    {
        attackSpeed += amount;
        WeaponBase wb = weaponInventory?.GetActiveWeaponBase();
        if (wb != null) wb.ApplyAttackSpeed(attackSpeed);
        Debug.Log($"[PlayerStats] Attack Speed: {attackSpeed * 100:F0}%");
    }

    public void AddExtraMagazine(int amount)
    {
        extraMagazine += amount;
        if (weaponInventory != null)
            foreach (GameObject w in weaponInventory.equippedWeapons)
            {
                WeaponBase wb = w.GetComponentInChildren<WeaponBase>();
                if (wb != null) wb.ApplyExtraMagazine(extraMagazine);
            }
        Debug.Log($"[PlayerStats] Extra Magazine: +{extraMagazine}");
    }

    public void AddGoldGain(float amount)
    {
        goldGainMultiplier += amount;
        Debug.Log($"[PlayerStats] Gold Gain: {goldGainMultiplier:F2}x");
    }

    public void AddLuck(float amount)
    {
        luck += amount;
        Debug.Log($"[PlayerStats] Luck: {luck:F2}");
    }

    [Header("XP & Leveling")]
    public int currentXP = 0;
    public int level = 1;
    public int baseXPToLevel = 100;
    public float xpGainMultiplier = 1f;
    public float pickupRange = 1f;

    public int XPToNextLevel => Mathf.RoundToInt(baseXPToLevel * level * level);

    public void AddXP(int amount)
    {
        int gained = Mathf.RoundToInt(amount * xpGainMultiplier);
        currentXP += gained;
        Debug.Log($"[PlayerStats] +{gained} XP | {currentXP}/{XPToNextLevel} | Level {level}");

        while (currentXP >= XPToNextLevel)
        {
            currentXP -= XPToNextLevel;
            level++;
            OnLevelUp();
        }
    }

    public void AddXPGain(float amount)
    {
        xpGainMultiplier += amount;
        Debug.Log($"[PlayerStats] XP Gain: {xpGainMultiplier:F2}x");
    }

    public void AddPickupRange(float amount)
    {
        pickupRange += amount;
        if (pickupZone != null) pickupZone.ApplyRange(pickupRange);
        Debug.Log($"[PlayerStats] Pickup Range: {pickupRange * 100:F0}%");
    }

    void OnLevelUp()
    {
        Debug.Log($"[PlayerStats] LEVEL UP! Now level {level} | Next level needs {XPToNextLevel} XP");
        if (augmentDraftUI != null) augmentDraftUI.OpenAugmentDraft();
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