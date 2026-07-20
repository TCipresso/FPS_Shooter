using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class WeaponInventory : NetworkBehaviour
{
    [Header("References")]
    public Transform weaponHolder;
    public PlayerStats playerStats;
    public IKWeaponHandler ikHandler;

    [Header("Input")]
    public InputActionReference fireAction;

    [Header("Networking")]
    public string remoteWeaponLayer = "Default";

    [Header("Weapons (drag pre-placed, disabled weapon children here)")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();

    private Dictionary<WeaponDefinitionSO, WeaponEntry> weaponLookup = new Dictionary<WeaponDefinitionSO, WeaponEntry>();
    private Dictionary<WeaponBase, Material[]> originalMaterialsCache = new Dictionary<WeaponBase, Material[]>();
    private WeaponEntry currentBaseEntry;
    private WeaponEntry activePowerUpEntry;
    private Coroutine powerUpRoutine;

    [SyncVar(hook = nameof(OnActiveWeaponIndexChanged))]
    private int syncedActiveWeaponIndex = -1;

    void Awake()
    {
        weaponLookup.Clear();
        originalMaterialsCache.Clear();
        WeaponEntry defaultEntry = null;

        foreach (WeaponEntry entry in weapons)
        {
            if (entry == null || entry.weaponRoot == null || entry.definition == null
                || entry.weaponBases == null || entry.weaponBases.Count == 0)
            {
                Debug.LogWarning("[WeaponInventory] Skipping invalid WeaponEntry.");
                continue;
            }

            weaponLookup[entry.definition] = entry;

            if (entry.baseLevel <= 0)
                entry.baseLevel = 1;
            if (entry.currentLevel <= 0)
                entry.currentLevel = entry.baseLevel;

            foreach (WeaponBase wb in entry.weaponBases)
            {
                if (wb == null) continue;
                if (wb.skinRenderer != null && !originalMaterialsCache.ContainsKey(wb))
                    originalMaterialsCache[wb] = (Material[])wb.skinRenderer.sharedMaterials.Clone();

                wb.onShotFired += HandleShotFired;
            }

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

    void OnDestroy()
    {
        foreach (WeaponEntry entry in weapons)
        {
            if (entry?.weaponBases == null) continue;

            foreach (WeaponBase wb in entry.weaponBases)
            {
                if (wb != null)
                    wb.onShotFired -= HandleShotFired;
            }
        }
    }

    public override void OnStartClient()
    {
        if (isLocalPlayer) return;
        if (weaponHolder == null) return;

        int layer = LayerMask.NameToLayer(remoteWeaponLayer);
        if (layer >= 0)
            SetLayerRecursively(weaponHolder, layer);
    }

    void Start()
    {
        foreach (WeaponEntry entry in weapons)
        {
            if (entry?.weaponRoot == null) continue;

            entry.weaponRoot.SetActive(false);

            foreach (WeaponBase wb in entry.weaponBases)
            {
                if (wb == null) continue;

                if (BulletPool.Instance != null && wb.bulletData != null && wb.bulletData.trailPrefab != null)
                    BulletPool.Instance.EnsurePoolSize(wb.bulletData.trailPoolKey, wb.bulletData.trailPrefab.gameObject, wb.bulletData.trailPoolSize);
            }
        }

        bool isLocalOrOffline = !NetworkClient.active || isLocalPlayer;

        if (isLocalOrOffline)
        {
            if (currentBaseEntry != null)
            {
                currentBaseEntry.currentLevel = currentBaseEntry.baseLevel;
                SetActiveEntry(currentBaseEntry);
            }
        }
        else
        {
            int index = syncedActiveWeaponIndex >= 0
                ? syncedActiveWeaponIndex
                : weapons.IndexOf(currentBaseEntry);
            ActivateEntryVisualByIndex(index);
        }
    }

    public override void OnStartLocalPlayer()
    {
        if (fireAction != null) fireAction.action.Enable();
    }

    void OnDisable()
    {
        if (!isLocalPlayer) return;
        if (fireAction != null) fireAction.action.Disable();
    }

    void Update()
    {
        if (NetworkClient.active && !isLocalPlayer) return;
        if (fireAction == null) return;

        List<WeaponBase> activeBases = GetActiveWeaponBases();
        if (activeBases == null || activeBases.Count == 0) return;

        WeaponBase primary = activeBases[0];
        bool shouldFire = primary != null && primary.isAutomatic
            ? fireAction.action.IsPressed()
            : fireAction.action.WasPressedThisFrame();

        if (shouldFire)
        {
            foreach (WeaponBase wb in activeBases)
                wb?.Shoot();
        }
        else if (fireAction.action.WasReleasedThisFrame())
        {
            foreach (WeaponBase wb in activeBases)
                wb?.StopRecoil();
        }
    }

    void HandleShotFired(WeaponBase wb)
    {
        if (!NetworkClient.active || !isLocalPlayer) return;

        for (int e = 0; e < weapons.Count; e++)
        {
            WeaponEntry entry = weapons[e];
            if (entry?.weaponBases == null) continue;

            int b = entry.weaponBases.IndexOf(wb);
            if (b >= 0)
            {
                CmdFireEffects(e, b);
                return;
            }
        }
    }

    [Command]
    void CmdFireEffects(int entryIndex, int baseIndex)
    {
        RpcFireEffects(entryIndex, baseIndex);
    }

    [ClientRpc(includeOwner = false)]
    void RpcFireEffects(int entryIndex, int baseIndex)
    {
        if (isLocalPlayer) return;
        if (entryIndex < 0 || entryIndex >= weapons.Count) return;

        WeaponEntry entry = weapons[entryIndex];
        if (entry?.weaponBases == null) return;
        if (baseIndex < 0 || baseIndex >= entry.weaponBases.Count) return;

        WeaponBase wb = entry.weaponBases[baseIndex];
        if (wb == null) return;

        wb.PlayRemoteFireEffects();
    }

    [Command]
    void CmdSetActiveWeapon(int index)
    {
        syncedActiveWeaponIndex = index;
    }

    void OnActiveWeaponIndexChanged(int oldIndex, int newIndex)
    {
        if (isLocalPlayer) return;
        ActivateEntryVisualByIndex(newIndex);
    }

    void ActivateEntryVisualByIndex(int index)
    {
        if (index < 0 || index >= weapons.Count) return;

        WeaponEntry entry = weapons[index];
        if (entry?.weaponRoot == null) return;

        foreach (WeaponEntry w in weapons)
        {
            if (w?.weaponRoot != null)
                w.weaponRoot.SetActive(false);
        }

        entry.weaponRoot.SetActive(true);

        if (ikHandler != null)
            ikHandler.UpdateIKTargets(entry.weaponRoot);
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }

    public void SetBaseWeapon(WeaponDefinitionSO def)
    {
        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot set base weapon, no entry for {def?.weaponName}.");
            return;
        }

        currentBaseEntry = entry;
        entry.currentLevel = entry.baseLevel;

        if (activePowerUpEntry == null)
            SetActiveEntry(entry);
    }

    public void PickupBaseWeaponBoost()
    {
        if (currentBaseEntry == null) return;
        ActivateBaseLevelPowerUp(currentBaseEntry);
    }

    public void PickupBaseLevelPowerUp(WeaponDefinitionSO def)
    {
        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot pick up base-level power-up, no entry for {def?.weaponName}.");
            return;
        }

        ActivateBaseLevelPowerUp(entry);
    }

    // Zero-lookup path for base-type power-ups (e.g. Z16), same pattern as PickupPowerUpByIndex.
    public void PickupBaseLevelPowerUpByIndex(int index)
    {
        if (index < 0 || index >= weapons.Count)
        {
            Debug.LogWarning($"[WeaponInventory] PickupBaseLevelPowerUpByIndex: index {index} out of range.");
            return;
        }

        ActivateBaseLevelPowerUp(weapons[index]);
    }

    void ActivateBaseLevelPowerUp(WeaponEntry entry)
    {
        if (entry?.weaponRoot == null || entry.definition == null) return;

        if (activePowerUpEntry == entry)
            entry.currentLevel = Mathf.Min(entry.currentLevel + 1, entry.definition.maxLevel);
        else
            entry.currentLevel = Mathf.Min(entry.baseLevel + 1, entry.definition.maxLevel);

        activePowerUpEntry = entry;
        SetActiveEntry(entry);

        if (powerUpRoutine != null)
            StopCoroutine(powerUpRoutine);
        powerUpRoutine = StartCoroutine(BaseLevelPowerUpCountdown(entry));
    }

    IEnumerator BaseLevelPowerUpCountdown(WeaponEntry entry)
    {
        while (true)
        {
            yield return new WaitForSeconds(entry.definition.powerUpDurationPerLevel);

            if (activePowerUpEntry != entry)
                yield break;

            if (entry.currentLevel > entry.baseLevel)
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
        {
            currentBaseEntry.currentLevel = currentBaseEntry.baseLevel;
            SetActiveEntry(currentBaseEntry);
        }
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

        WeaponBase primary = entry.Primary;
        if (primary != null)
            primary.LoadRecoilValues();

        if (ikHandler != null)
            ikHandler.UpdateIKTargets(entry.weaponRoot);

        if (NetworkClient.active && isLocalPlayer)
        {
            int index = weapons.IndexOf(entry);
            if (index >= 0)
                CmdSetActiveWeapon(index);
        }
    }

    void ApplyLevel(WeaponEntry entry)
    {
        if (entry?.weaponBases == null || entry.definition == null) return;

        foreach (WeaponBase wb in entry.weaponBases)
        {
            if (wb == null) continue;

            wb.ApplyLevel(entry.definition, entry.currentLevel);
        }

        ApplyWeaponSkin(entry);
    }

    static MaterialPropertyBlock sharedPropertyBlock;

    void ApplyWeaponSkin(WeaponEntry entry)
    {
        WeaponDefinitionSO def = entry.definition;

        foreach (WeaponBase wb in entry.weaponBases)
        {
            if (wb == null) continue;

            Renderer renderer = wb.skinRenderer;
            if (renderer == null) continue;

            if (entry.currentLevel <= 1 || def.packedMaterial == null)
            {
                if (originalMaterialsCache.TryGetValue(wb, out Material[] original))
                    renderer.sharedMaterials = original;
                renderer.SetPropertyBlock(null);
                continue;
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
    }

    public WeaponBase GetActiveWeaponBase()
    {
        WeaponEntry active = activePowerUpEntry ?? currentBaseEntry;
        return active?.Primary;
    }

    public List<WeaponBase> GetActiveWeaponBases()
    {
        WeaponEntry active = activePowerUpEntry ?? currentBaseEntry;
        return active?.weaponBases;
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