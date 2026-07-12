using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public FPSLook look;
    public WeaponInventory weaponInventory;
    public PickupZone pickupZone;
    //public AugmentDraftUI augmentDraftUI;
    public PlayerHealth playerHealth;

    [Header("Combat Stats")]
    [Range(0f, 5f)] public float attackSpeed = 1f;
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critMultiplier = 1.5f;
    public float luck = 0f;

    [Header("Gold")]
    public int gold = 500;
    public int baseGoldOnHit = 0;
    public int baseGoldOnKill = 100;
    public float goldGainMultiplier = 1f;
    public int goldOnHit => Mathf.RoundToInt(baseGoldOnHit * goldGainMultiplier);
    public int goldOnKill => Mathf.RoundToInt(baseGoldOnKill * goldGainMultiplier);

    [Header("XP & Leveling")]
    public int currentXP = 0;
    public int level = 1;
    public int baseXPToLevel = 100;
    public float xpGainMultiplier = 1f;
    public float pickupRange = 1f;
    public int XPToNextLevel => Mathf.RoundToInt(baseXPToLevel * level * level);

    void Awake()
    {
        // Application.targetFrameRate = 300;
    }

    void Start()
    {
        if (pickupZone != null) pickupZone.ApplyRange(pickupRange);
    }

    //

    public void AddCritChance(float amount)
    {
        critChance = Mathf.Clamp01(critChance + amount);
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
        var actives = weaponInventory?.GetActiveWeaponBases();
        if (actives == null) return;

        foreach (WeaponBase wb in actives)
        {
            if (wb == null) continue;
            wb.critChance = critChance;
            wb.critMultiplier = critMultiplier;
        }
    }

    public void AddAttackSpeed(float amount)
    {
        attackSpeed += amount;
        var actives = weaponInventory?.GetActiveWeaponBases();
        if (actives != null)
        {
            foreach (WeaponBase wb in actives)
                wb?.ApplyAttackSpeed(attackSpeed);
        }
        Debug.Log($"[PlayerStats] Attack Speed: {attackSpeed * 100:F0}%");
    }

    // 

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

    public void AddGoldGain(float amount)
    {
        goldGainMultiplier += amount;
        Debug.Log($"[PlayerStats] Gold Gain: {goldGainMultiplier:F2}x");
    }

    // 

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

    public void AddLuck(float amount)
    {
        luck += amount;
        Debug.Log($"[PlayerStats] Luck: {luck:F2}");
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
       // if (augmentDraftUI != null) augmentDraftUI.OpenAugmentDraft();
    }

    //

    public void TakeHit()
    {
        if (playerHealth != null)
            playerHealth.TakeHit();
    }

    public void AddLife(int amount = 1)
    {
        if (playerHealth != null)
            playerHealth.AddLife(amount);
    }

    public void AddHitsToDown(int amount = 1)
    {
        if (playerHealth != null)
            playerHealth.AddHit(amount);
    }
}