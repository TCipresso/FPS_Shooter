using UnityEngine;
using UnityEngine.Events;
using System.Collections;
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }
    [Header("References")]
    public FPSLook look;
    public PlayerFpsController fpsController;
    public GameObject gameOverScreen;
    //public AugmentDraftUI augmentDraftUI;
    [Header("Combat Stats")]
    public float damageMultiplier = 1f;
    [Range(0f, 5f)] public float attackSpeed = 1f;
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critMultiplier = 1.5f;
    public float luck = 0f;
    [Header("Ability Stats")]
    public float abilityDamageMultiplier = 1f;
    public float abilityCooldownMultiplier = 1f;
    public float aoeSize = 1f;
    [Header("Movement Stats")]
    public float moveSpeed = 5f;
    public int jumpCount = 1;
    public int dashCount = 1;
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public float healthRegen = 1f;
    public float regenDelay = 5f;
    float timeSinceLastDamage = 0f;
    float regenAccumulator = 0f;
    [Header("Lives")]
    public int lives = 3;
    public float downDuration = 5f;
    public float reviveInvulnerabilityDuration = 5f;
    public GameObject reviveAura;
    public bool IsInvulnerable { get; private set; }
    [Header("Hit Indicator")]
    public CanvasGroup hitIndicator;
    public float hitFlashMax = 0.6f;
    public float hitFlashOutSpeed = 3f;
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip downSound;
    [Header("Events")]
    public UnityEvent onHit;
    public UnityEvent onDown;
    public UnityEvent onRevive;
    public UnityEvent onGameOver;
    public bool IsDown { get; private set; }
    public bool IsGameOver { get; private set; }
    [Header("Gold")]
    public int gold = 500;
    public int baseGoldOnHit = 0;
    public int baseGoldOnKill = 100;
    public float goldGainMultiplier = 1f;
    public int goldOnHit => Mathf.RoundToInt(baseGoldOnHit * goldGainMultiplier);
    public int goldOnKill => Mathf.RoundToInt(baseGoldOnKill * goldGainMultiplier);
    [Header("Power-Ups")]
    [Range(0f, 1f)] public float powerUpDropChance = 0.05f;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Application.targetFrameRate = 300;
        currentHealth = maxHealth;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    void Start()
    {
        if (hitIndicator != null) hitIndicator.alpha = 0f;
    }
    void Update()
    {
        timeSinceLastDamage += Time.deltaTime;
        if (IsDown || IsGameOver) return;
        if (currentHealth >= maxHealth || healthRegen <= 0f) return;
        if (timeSinceLastDamage < regenDelay) return;
        regenAccumulator += healthRegen * Time.deltaTime;
        if (regenAccumulator >= 1f)
        {
            int healAmount = Mathf.FloorToInt(regenAccumulator);
            regenAccumulator -= healAmount;
            Heal(healAmount);
        }
    }
    //
    public void AddCritChance(float amount)
    {
        critChance = Mathf.Clamp01(critChance + amount);
        Debug.Log($"[PlayerStats] Crit Chance: {critChance * 100:F0}%");
    }
    public void AddCritMultiplier(float amount)
    {
        critMultiplier += amount;
        Debug.Log($"[PlayerStats] Crit Multiplier: {critMultiplier:F2}x");
    }
    public void AddAttackSpeed(float amount)
    {
        attackSpeed += amount;
        Debug.Log($"[PlayerStats] Attack Speed: {attackSpeed * 100:F0}%");
    }
    public void AddDamageMultiplier(float amount)
    {
        damageMultiplier += amount;
        Debug.Log($"[PlayerStats] Damage Multiplier: {damageMultiplier:F2}x");
    }
    //
    public void AddAbilityDamageMultiplier(float amount)
    {
        abilityDamageMultiplier += amount;
        Debug.Log($"[PlayerStats] Ability Damage Multiplier: {abilityDamageMultiplier:F2}x");
    }
    public void AddAbilityCooldownReduction(float amount)
    {
        abilityCooldownMultiplier = Mathf.Max(0.1f, abilityCooldownMultiplier - amount);
        Debug.Log($"[PlayerStats] Ability Cooldown Multiplier: {abilityCooldownMultiplier:F2}x");
    }
    public void AddAoeSize(float amount)
    {
        aoeSize += amount;
        Debug.Log($"[PlayerStats] AoE Size: {aoeSize * 100:F0}%");
    }
    //
    public void AddMoveSpeed(float amount)
    {
        moveSpeed += amount;
        Debug.Log($"[PlayerStats] Move Speed: {moveSpeed:F2}");
    }
    public void AddJumpCount(int amount)
    {
        jumpCount += amount;
        Debug.Log($"[PlayerStats] Jump Count: {jumpCount}");
    }
    public void AddDashCount(int amount)
    {
        dashCount += amount;
        Debug.Log($"[PlayerStats] Dash Count: {dashCount}");
    }
    //
    public void AddMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerStats] Max Health: {maxHealth}");
    }
    public void AddHealthRegen(float amount)
    {
        healthRegen += amount;
        Debug.Log($"[PlayerStats] Health Regen: {healthRegen:F2}/s");
    }
    public void ReduceRegenDelay(float amount)
    {
        regenDelay = Mathf.Max(0f, regenDelay - amount);
        Debug.Log($"[PlayerStats] Regen Delay: {regenDelay:F2}s");
    }
    public void TakeDamage(int amount)
    {
        if (IsDown || IsGameOver || IsInvulnerable) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        timeSinceLastDamage = 0f;
        regenAccumulator = 0f;
        FlashHitIndicator();
        PlaySound(hitSound);
        onHit.Invoke();
        Debug.Log($"[PlayerStats] -{amount} HP | {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
            Down();
    }
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log($"[PlayerStats] +{amount} HP | {currentHealth}/{maxHealth}");
    }
    void FlashHitIndicator()
    {
        if (hitIndicator == null) return;
        StopCoroutine(nameof(HitFlashRoutine));
        StartCoroutine(nameof(HitFlashRoutine));
    }
    IEnumerator HitFlashRoutine()
    {
        hitIndicator.alpha = hitFlashMax;
        while (hitIndicator.alpha > 0f)
        {
            hitIndicator.alpha = Mathf.MoveTowards(hitIndicator.alpha, 0f, hitFlashOutSpeed * Time.deltaTime);
            yield return null;
        }
        hitIndicator.alpha = 0f;
    }
    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }
    void Down()
    {
        IsDown = true;
        lives--;
        PlaySound(downSound);
        EnterDownedState();
        onDown.Invoke();
        Debug.Log($"[PlayerStats] Player is down. Lives remaining: {lives}");
        if (lives <= 0)
            GameOver();
        else
            StartCoroutine(DownRoutine());
    }
    IEnumerator DownRoutine()
    {
        yield return new WaitForSeconds(downDuration);
        Revive();
    }
    void GameOver()
    {
        IsGameOver = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        onGameOver.Invoke();
        Debug.Log("[PlayerStats] Game Over.");
    }
    public void Revive()
    {
        if (IsGameOver) return;
        IsDown = false;
        currentHealth = maxHealth;
        timeSinceLastDamage = 0f;
        regenAccumulator = 0f;
        ExitDownedState();
        onRevive.Invoke();
        Debug.Log($"[PlayerStats] Player revived. Lives remaining: {lives}");
        StartCoroutine(ReviveInvulnerabilityRoutine());
    }
    IEnumerator ReviveInvulnerabilityRoutine()
    {
        IsInvulnerable = true;
        if (reviveAura != null) reviveAura.SetActive(true);
        yield return new WaitForSeconds(reviveInvulnerabilityDuration);
        IsInvulnerable = false;
        if (reviveAura != null) reviveAura.SetActive(false);
    }
    void EnterDownedState()
    {
        if (fpsController != null) fpsController.IsDowned = true;
        if (look != null) look.CanLook = false;
    }
    void ExitDownedState()
    {
        if (fpsController != null) fpsController.IsDowned = false;
        if (look != null) look.CanLook = true;
    }
    public void AddLife(int amount = 1)
    {
        lives += amount;
        Debug.Log($"[PlayerStats] Lives: {lives}");
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
    public void AddLuck(float amount)
    {
        luck += amount;
        Debug.Log($"[PlayerStats] Luck: {luck:F2}");
    }
    public void AddPowerUpDropChance(float amount)
    {
        powerUpDropChance = Mathf.Clamp01(powerUpDropChance + amount);
        Debug.Log($"[PlayerStats] Power-Up Drop Chance: {powerUpDropChance * 100:F0}%");
    }
}