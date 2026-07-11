using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : MonoBehaviour
{
    [Header("References")]
    public Transform weaponHolder;
    public PlayerStats playerStats;
    public IKWeaponHandler ikHandler;

    [Header("Input")]
    public InputActionReference fireAction;

    [Header("Weapons (drag pre-placed, disabled weapon children here)")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();

    private Dictionary<WeaponDefinitionSO, WeaponEntry> weaponLookup = new Dictionary<WeaponDefinitionSO, WeaponEntry>();
    private WeaponEntry currentBaseEntry;
    private WeaponEntry activePowerUpEntry;
    private Coroutine powerUpRoutine;

    void Awake()
    {
        weaponLookup.Clear();
        WeaponEntry defaultEntry = null;

        foreach (WeaponEntry entry in weapons)
        {
            if (entry == null || entry.weaponBase == null || entry.weaponRoot == null || entry.definition == null)
            {
                Debug.LogWarning("[WeaponInventory] Skipping invalid WeaponEntry.");
                continue;
            }

            weaponLookup[entry.definition] = entry;

            if (entry.weaponBase.skinRenderer != null)
                entry.originalMaterials = (Material[])entry.weaponBase.skinRenderer.sharedMaterials.Clone();

            if (entry.isDefaultBase)
            {
                if (defaultEntry != null)
                    Debug.LogWarning($"[WeaponInventory] Multiple entries marked as default base. Using {defaultEntry.definition.weaponName}, ignoring {entry.definition.weaponName}.");
                else
                    defaultEntry = entry;
            }
        }

        if (defaultEntry == null)
            Debug.LogWarning("[WeaponInventory] No entry marked isDefaultBase - check one entry's box in the Inspector.");

        currentBaseEntry = defaultEntry;
    }

    void Start()
    {
        foreach (WeaponEntry entry in weapons)
        {
            if (entry?.weaponRoot == null) continue;

            entry.weaponRoot.SetActive(false);

            if (entry.weaponBase == null) continue;

            if (BulletPool.Instance != null && entry.weaponBase.bulletData != null && entry.weaponBase.bulletData.trailPrefab != null)
                BulletPool.Instance.EnsurePoolSize(entry.weaponBase.bulletData.trailPoolKey, entry.weaponBase.bulletData.trailPrefab.gameObject, entry.weaponBase.bulletData.trailPoolSize);
        }

        if (currentBaseEntry != null)
        {
            currentBaseEntry.currentLevel = currentBaseEntry.baseLevel;
            SetActiveEntry(currentBaseEntry);
        }
    }

    void OnEnable()
    {
        if (fireAction != null) fireAction.action.Enable();
    }

    void OnDisable()
    {
        if (fireAction != null) fireAction.action.Disable();
    }

    void Update()
    {
        if (fireAction != null)
        {
            WeaponBase active = GetActiveWeaponBase();
            bool shouldFire = active != null && active.isAutomatic
                ? fireAction.action.IsPressed()
                : fireAction.action.WasPressedThisFrame();

            if (shouldFire)
                active?.Shoot();
            else if (fireAction.action.WasReleasedThisFrame())
                active?.StopRecoil();
        }
    }

    public void SetBaseWeapon(WeaponDefinitionSO def)
    {
        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot set base weapon, no entry for {def?.weaponName}.");
            return;
        }

        currentBaseEntry = entry;

        if (activePowerUpEntry == null)
            SetActiveEntry(entry);
    }

    public void PickupPowerUp(WeaponDefinitionSO def)
    {
        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot pick up power-up, no entry for {def?.weaponName}.");
            return;
        }

        ActivatePowerUp(entry);
    }

    // Zero-lookup path: index maps directly into the weapons list, no dictionary hit.
    public void PickupPowerUpByIndex(int index)
    {
        if (index < 0 || index >= weapons.Count)
        {
            Debug.LogWarning($"[WeaponInventory] PickupPowerUpByIndex: index {index} out of range.");
            return;
        }

        ActivatePowerUp(weapons[index]);
    }

    void ActivatePowerUp(WeaponEntry entry)
    {
        if (entry?.weaponRoot == null || entry.definition == null) return;

        if (activePowerUpEntry == entry)
        {
            // Already active - stack another level on top, temporary only.
            entry.currentLevel = Mathf.Min(entry.currentLevel + 1, entry.definition.maxLevel);
        }
        else
        {
            // Fresh pickup - starts at level 1 regardless of baseLevel. Purely temporary.
            entry.currentLevel = 1;
        }

        activePowerUpEntry = entry;
        SetActiveEntry(entry);

        if (powerUpRoutine != null)
            StopCoroutine(powerUpRoutine);
        powerUpRoutine = StartCoroutine(PowerUpCountdown(entry));
    }

    IEnumerator PowerUpCountdown(WeaponEntry entry)
    {
        while (true)
        {
            yield return new WaitForSeconds(entry.definition.powerUpDurationPerLevel);

            if (activePowerUpEntry != entry)
                yield break;

            if (entry.currentLevel > 1)
            {
                entry.currentLevel--;
                ApplyLevel(entry);
            }
            else
            {
                RevertToBaseWeapon();
                yield break;
            }
        }
    }

    void RevertToBaseWeapon()
    {
        activePowerUpEntry = null;
        powerUpRoutine = null;

        if (currentBaseEntry != null)
            SetActiveEntry(currentBaseEntry);
    }

    public void LevelUpWeapon(WeaponDefinitionSO def)
    {
        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot level up, no entry for {def?.weaponName}.");
            return;
        }

        entry.baseLevel = Mathf.Min(entry.baseLevel + 1, entry.definition.maxLevel);

        WeaponEntry active = activePowerUpEntry ?? currentBaseEntry;
        if (active == entry)
        {
            entry.currentLevel = entry.baseLevel;
            ApplyLevel(entry);
        }
    }

    void SetActiveEntry(WeaponEntry entry)
    {
        if (entry?.weaponRoot == null) return;

        foreach (WeaponEntry w in weapons)
        {
            if (w?.weaponRoot != null)
                w.weaponRoot.SetActive(false);
        }

        entry.weaponRoot.SetActive(true);
        ApplyLevel(entry);

        entry.weaponBase.LoadRecoilValues();
        if (playerStats != null)
        {
            entry.weaponBase.ApplyAttackSpeed(playerStats.attackSpeed);
            entry.weaponBase.critChance = playerStats.critChance;
            entry.weaponBase.critMultiplier = playerStats.critMultiplier;
        }

        if (ikHandler != null)
            ikHandler.UpdateIKTargets(entry.weaponRoot);
    }

    void ApplyLevel(WeaponEntry entry)
    {
        if (entry?.weaponBase == null || entry.definition == null) return;

        entry.weaponBase.ApplyLevel(entry.definition, entry.currentLevel);

        if (playerStats != null)
            entry.weaponBase.ApplyAttackSpeed(playerStats.attackSpeed);

        ApplyWeaponSkin(entry);
    }

    static MaterialPropertyBlock sharedPropertyBlock;

    void ApplyWeaponSkin(WeaponEntry entry)
    {
        Renderer renderer = entry.weaponBase.skinRenderer;
        WeaponDefinitionSO def = entry.definition;

        if (renderer == null) return;

        if (entry.currentLevel <= 1 || def.packedMaterial == null)
        {
            if (entry.originalMaterials != null)
                renderer.sharedMaterials = entry.originalMaterials;
            renderer.SetPropertyBlock(null);
            return;
        }

        int slotCount = renderer.sharedMaterials.Length;
        Material[] packedSet = new Material[slotCount];
        for (int i = 0; i < slotCount; i++)
            packedSet[i] = def.packedMaterial;
        renderer.sharedMaterials = packedSet;

        if (sharedPropertyBlock == null)
            sharedPropertyBlock = new MaterialPropertyBlock();

        renderer.GetPropertyBlock(sharedPropertyBlock);

        int tintIndex = entry.currentLevel - 2;
        Color tint = (def.levelTintColors != null && tintIndex >= 0 && tintIndex < def.levelTintColors.Length)
            ? def.levelTintColors[tintIndex]
            : Color.white;

        sharedPropertyBlock.SetColor(def.tintPropertyName, tint);
        renderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public WeaponBase GetActiveWeaponBase()
    {
        if (activePowerUpEntry != null)
            return activePowerUpEntry.weaponBase;

        return currentBaseEntry != null ? currentBaseEntry.weaponBase : null;
    }

    public int GetBaseLevel(WeaponDefinitionSO def)
    {
        return weaponLookup.TryGetValue(def, out WeaponEntry entry) ? entry.baseLevel : 0;
    }

    public int GetCurrentLevel(WeaponDefinitionSO def)
    {
        return weaponLookup.TryGetValue(def, out WeaponEntry entry) ? entry.currentLevel : 0;
    }
}