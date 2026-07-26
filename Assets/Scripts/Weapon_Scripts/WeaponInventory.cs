using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class WeaponInventory : NetworkBehaviour
{
    public const int SlotCount = 2;

    [Header("References")]
    public Transform weaponHolder;
    public PlayerStats playerStats;
    public IKWeaponHandler ikHandler;

    [Header("Input")]
    public InputActionReference fireAction;
    public InputActionReference swapAction;

    [Header("Networking")]
    public string remoteWeaponLayer = "Default";

    [Header("Starting Loadout (element 0 = slot 1, element 1 = slot 2)")]
    public List<WeaponDefinitionSO> startingWeapons = new List<WeaponDefinitionSO>();

    [Header("Weapons (drag pre-placed, disabled weapon children here)")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();

    private readonly WeaponEntry[] slots = new WeaponEntry[SlotCount];
    private Dictionary<WeaponDefinitionSO, WeaponEntry> weaponLookup = new Dictionary<WeaponDefinitionSO, WeaponEntry>();
    private Dictionary<WeaponBase, Material[]> originalMaterialsCache = new Dictionary<WeaponBase, Material[]>();
    private int activeSlot = 0;

    [SyncVar(hook = nameof(OnActiveWeaponIndexChanged))]
    private int syncedActiveWeaponIndex = -1;

    [Header("Drop / Pickup")]
    [Tooltip("Where dropped weapons spawn, relative to the player. Usually a point slightly in front and below the camera.")]
    public Transform dropOrigin;
    public float dropForwardOffset = 1.2f;
    public float dropUpOffset = 0.2f;

    void Awake()
    {
        weaponLookup.Clear();
        originalMaterialsCache.Clear();

        foreach (WeaponEntry entry in weapons)
        {
            if (entry == null || entry.weaponRoot == null || entry.definition == null
                || entry.weaponBases == null || entry.weaponBases.Count == 0)
            {
                Debug.LogWarning("[WeaponInventory] Skipping invalid WeaponEntry.");
                continue;
            }

            weaponLookup[entry.definition] = entry;

            if (entry.level <= 0)
                entry.level = 1;

            foreach (WeaponBase wb in entry.weaponBases)
            {
                if (wb == null) continue;
                if (wb.skinRenderer != null && !originalMaterialsCache.ContainsKey(wb))
                    originalMaterialsCache[wb] = (Material[])wb.skinRenderer.sharedMaterials.Clone();

                wb.onShotFired += HandleShotFired;
            }
        }
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
            InitializeStartingLoadout();
        else
            ActivateEntryVisualByIndex(syncedActiveWeaponIndex);
    }

    void InitializeStartingLoadout()
    {
        for (int i = 0; i < SlotCount; i++)
            slots[i] = null;

        for (int i = 0; i < SlotCount && i < startingWeapons.Count; i++)
        {
            WeaponDefinitionSO def = startingWeapons[i];
            if (def == null) continue;

            if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
            {
                Debug.LogWarning($"[WeaponInventory] Starting weapon {def.weaponName} has no matching WeaponEntry.");
                continue;
            }

            slots[i] = entry;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == null) continue;
            EquipSlot(i);
            return;
        }

        Debug.LogWarning("[WeaponInventory] No starting weapons assigned.");
    }

    public override void OnStartLocalPlayer()
    {
        if (fireAction != null) fireAction.action.Enable();
        if (swapAction != null) swapAction.action.Enable();
    }

    void OnDisable()
    {
        if (!isLocalPlayer) return;
        if (fireAction != null) fireAction.action.Disable();
        if (swapAction != null) swapAction.action.Disable();
    }

    void Update()
    {
        if (NetworkClient.active && !isLocalPlayer) return;

        if (swapAction != null && swapAction.action.WasPressedThisFrame())
            SwapWeapon();

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

    void HandleShotFired(WeaponBase wb, List<Vector3> endpoints, List<byte> hitTypes)
    {
        if (!NetworkClient.active || !isLocalPlayer) return;

        for (int e = 0; e < weapons.Count; e++)
        {
            WeaponEntry entry = weapons[e];
            if (entry?.weaponBases == null) continue;

            int b = entry.weaponBases.IndexOf(wb);
            if (b >= 0)
            {
                CmdFireEffects(e, b, endpoints.ToArray(), hitTypes.ToArray());
                return;
            }
        }
    }

    [Command]
    void CmdFireEffects(int entryIndex, int baseIndex, Vector3[] endpoints, byte[] hitTypes)
    {
        RpcFireEffects(entryIndex, baseIndex, endpoints, hitTypes);
    }

    [ClientRpc(includeOwner = false)]
    void RpcFireEffects(int entryIndex, int baseIndex, Vector3[] endpoints, byte[] hitTypes)
    {
        if (isLocalPlayer) return;
        if (entryIndex < 0 || entryIndex >= weapons.Count) return;

        WeaponEntry entry = weapons[entryIndex];
        if (entry?.weaponBases == null) return;
        if (baseIndex < 0 || baseIndex >= entry.weaponBases.Count) return;

        WeaponBase wb = entry.weaponBases[baseIndex];
        if (wb == null) return;

        wb.PlayRemoteFireEffects(endpoints, hitTypes);
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

    public void SwapWeapon()
    {
        int next = (activeSlot + 1) % SlotCount;
        if (slots[next] == null) return;
        EquipSlot(next);
    }

    public void EquipSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return;
        if (slots[slot] == null) return;

        activeSlot = slot;
        SetActiveEntry(slots[slot]);
    }

    public int AddWeapon(WeaponDefinitionSO def)
    {
        if (def == null) return -1;

        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot add weapon, no entry for {def.weaponName}.");
            return -1;
        }

        return AddEntry(entry);
    }

    public int AddWeaponByIndex(int index)
    {
        if (index < 0 || index >= weapons.Count)
        {
            Debug.LogWarning($"[WeaponInventory] AddWeaponByIndex: index {index} out of range.");
            return -1;
        }

        return AddEntry(weapons[index]);
    }

    public bool AddWeaponToSlot(WeaponDefinitionSO def, int slot)
    {
        if (def == null) return false;
        if (slot < 0 || slot >= SlotCount) return false;

        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot add weapon, no entry for {def.weaponName}.");
            return false;
        }

        slots[slot] = entry;
        EquipSlot(slot);
        return true;
    }

    int AddEntry(WeaponEntry entry)
    {
        if (entry?.weaponRoot == null || entry.definition == null) return -1;

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != entry) continue;
            EquipSlot(i);
            return i;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null) continue;
            slots[i] = entry;
            EquipSlot(i);
            return i;
        }

        slots[activeSlot] = entry;
        EquipSlot(activeSlot);
        return activeSlot;
    }

    public void RemoveWeapon(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return;
        if (slots[slot] == null) return;

        slots[slot] = null;

        if (slot != activeSlot) return;

        int other = (slot + 1) % SlotCount;
        if (slots[other] != null)
            EquipSlot(other);
        else if (slots[slot] == null && slots[other] == null)
            SetAllWeaponsInactive();
    }

    void SetAllWeaponsInactive()
    {
        foreach (WeaponEntry w in weapons)
        {
            if (w?.weaponRoot != null)
                w.weaponRoot.SetActive(false);
        }
    }

    public void LevelUpWeapon(WeaponDefinitionSO def)
    {
        if (!weaponLookup.TryGetValue(def, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot level up, no entry for {def?.weaponName}.");
            return;
        }

        entry.level = Mathf.Min(entry.level + 1, entry.definition.maxLevel);

        if (slots[activeSlot] == entry)
            ApplyLevel(entry);
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

            wb.ApplyLevel(entry.definition, entry.level);
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

            if (entry.level <= 1 || def.packedMaterial == null)
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

            int tintIndex = entry.level - 2;
            Color tint = (def.levelTintColors != null && tintIndex >= 0 && tintIndex < def.levelTintColors.Length)
                ? def.levelTintColors[tintIndex]
                : Color.white;

            sharedPropertyBlock.SetColor(def.tintPropertyName, tint);
            renderer.SetPropertyBlock(sharedPropertyBlock);
        }
    }

    public int ActiveSlot => activeSlot;

    public WeaponEntry GetSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return null;
        return slots[slot];
    }

    public bool HasWeapon(WeaponDefinitionSO def)
    {
        if (def == null) return false;
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null && slots[i].definition == def)
                return true;
        }
        return false;
    }

    public WeaponBase GetActiveWeaponBase()
    {
        return slots[activeSlot]?.Primary;
    }

    public List<WeaponBase> GetActiveWeaponBases()
    {
        return slots[activeSlot]?.weaponBases;
    }

    public int GetLevel(WeaponDefinitionSO def)
    {
        return weaponLookup.TryGetValue(def, out WeaponEntry entry) ? entry.level : 0;
    }

    public void RequestPickup(WeaponPickup pickup)
    {
        if (pickup == null) { Debug.Log("[PU] pickup is null"); return; }
        if (!isLocalPlayer) { Debug.Log("[PU] not local player"); return; }

        Debug.Log("[PU] sending Cmd for " + (pickup.definition != null ? pickup.definition.weaponName : "NULL DEF"));
        CmdPickupWeapon(pickup.netId);
    }

    [Command]
    void CmdPickupWeapon(uint pickupNetId)
    {
        if (!NetworkServer.spawned.TryGetValue(pickupNetId, out NetworkIdentity pickupIdentity))
        {
            Debug.Log("[PU] server: pickup netId not found in spawned");
            return;
        }

        WeaponPickup pickup = pickupIdentity.GetComponent<WeaponPickup>();
        if (pickup == null) { Debug.Log("[PU] server: no WeaponPickup on spawned object"); return; }

        WeaponDefinitionSO incomingDef = pickup.definition;
        if (incomingDef == null) { Debug.Log("[PU] server: pickup.definition is null"); return; }

        if (!weaponLookup.TryGetValue(incomingDef, out WeaponEntry incomingEntry))
        {
            Debug.Log("[PU] server: no pre-placed WeaponEntry on this player for " + incomingDef.weaponName);
            return;
        }

        int incomingIndex = weapons.IndexOf(incomingEntry);
        if (incomingIndex < 0) { Debug.Log("[PU] server: entry not found in weapons list"); return; }

        int incomingLevel = pickup.level;

        int targetSlot = activeSlot;
        WeaponEntry displaced = slots[targetSlot];
        bool hasDisplaced = displaced != null && displaced.definition != null;

        NetworkServer.Destroy(pickup.gameObject);

        if (hasDisplaced)
            SpawnPickup(displaced.definition, displaced.level);

        Debug.Log("[PU] server: applying " + incomingDef.weaponName + " Lv" + incomingLevel + " to slot " + targetSlot);
        RpcApplyPickup(targetSlot, incomingIndex, incomingLevel);
    }

    [ClientRpc]
    void RpcApplyPickup(int slot, int weaponIndex, int level)
    {
        if (weaponIndex < 0 || weaponIndex >= weapons.Count) return;

        WeaponEntry entry = weapons[weaponIndex];
        if (entry == null || entry.definition == null) return;

        entry.level = Mathf.Clamp(level, 1, entry.definition.maxLevel);
        slots[slot] = entry;
        EquipSlot(slot);
    }

    [Server]
    void SpawnPickup(WeaponDefinitionSO def, int level)
    {
        if (def == null || def.dropPrefab == null)
        {
            Debug.LogWarning("[WeaponInventory] No dropPrefab assigned for this weapon; cannot drop.");
            return;
        }

        Vector3 basePos = dropOrigin != null ? dropOrigin.position : transform.position;
        Vector3 forward = dropOrigin != null ? dropOrigin.forward : transform.forward;
        Vector3 spawnPos = basePos + forward * dropForwardOffset + Vector3.up * dropUpOffset;

        GameObject go = Instantiate(def.dropPrefab, spawnPos, Quaternion.identity);

        WeaponPickup pickup = go.GetComponent<WeaponPickup>();
        if (pickup == null)
        {
            Debug.LogWarning("[WeaponInventory] dropPrefab has no WeaponPickup component.");
            Destroy(go);
            return;
        }

        NetworkServer.Spawn(go);
        pickup.Initialize(level);
    }

    public void RequestDropActive()
    {
        if (!isLocalPlayer) return;
        CmdDropActive();
    }

    [Command]
    void CmdDropActive()
    {
        WeaponEntry entry = slots[activeSlot];
        if (entry == null || entry.definition == null) return;

        SpawnPickup(entry.definition, entry.level);

        RpcClearSlot(activeSlot);
    }

    [ClientRpc]
    void RpcClearSlot(int slot)
    {
        RemoveWeapon(slot);
    }
}