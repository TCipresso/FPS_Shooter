using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Slot 0 = Left Hand, Slot 1 = Right Hand
public class WeaponInventory : MonoBehaviour
{
    [Header("References")]
    public Transform leftWeaponHolder;
    public Transform rightWeaponHolder;
    public PlayerStats playerStats;

    [Header("IK")]
    public IKWeaponHandler ikHandler;

    [Header("Debug Start Weapon")]
    public bool giveRightWeaponOnStart = false;
    public WeaponDefinitionSO rightStartWeapon;

    [Header("Input")]
    public InputActionReference fireAction;
    public InputActionReference reloadAction;
    public InputActionReference scrollAction;
    public InputActionReference slot1Action;
    public InputActionReference slot2Action;

    public List<GameObject> equippedWeapons = new List<GameObject>(2) { null, null };
    public List<WeaponData> equippedData = new List<WeaponData>(2) { null, null };
    public List<WeaponUpgradeData> equippedUpgradeData = new List<WeaponUpgradeData>(2) { null, null };
    public List<int> weaponLevels = new List<int>(2) { 0, 0 };

    private int activeSlot = 0;
    private WeaponInstance activeInstance;
    private PlayerFpsController fpsController;

    // Alternating fire
    private int alternateIndex = 0;
    private float alternateTimer = 0f;

    void Awake()
    {
        fpsController = GetComponentInParent<PlayerFpsController>();
        if (fpsController == null)
            fpsController = FindFirstObjectByType<PlayerFpsController>();

        while (equippedWeapons.Count < 2) equippedWeapons.Add(null);
        while (equippedData.Count < 2) equippedData.Add(null);
        while (equippedUpgradeData.Count < 2) equippedUpgradeData.Add(null);
        while (weaponLevels.Count < 2) weaponLevels.Add(0);
    }

    void Start()
    {
        if (giveRightWeaponOnStart && rightStartWeapon != null)
        {
            WeaponInstance instance = new WeaponInstance(rightStartWeapon, WeaponRarity.Common);
            TryAddWeaponInstanceToSlot(instance, 1);
        }
    }

    void OnEnable()
    {
        if (fireAction != null) fireAction.action.Enable();
        if (reloadAction != null) reloadAction.action.Enable();
        if (scrollAction != null) scrollAction.action.Enable();
        if (slot1Action != null) slot1Action.action.Enable();
        if (slot2Action != null) slot2Action.action.Enable();
    }

    void OnDisable()
    {
        if (fireAction != null) fireAction.action.Disable();
        if (reloadAction != null) reloadAction.action.Disable();
        if (scrollAction != null) scrollAction.action.Disable();
        if (slot1Action != null) slot1Action.action.Disable();
        if (slot2Action != null) slot2Action.action.Disable();
    }

    void Update()
    {
        if (fireAction != null)
        {
            bool pressed = fireAction.action.WasPressedThisFrame();
            bool held = fireAction.action.IsPressed();
            bool released = fireAction.action.WasReleasedThisFrame();

            if (IsAlternateMode())
            {
                alternateTimer -= Time.deltaTime;

                if (released)
                {
                    alternateTimer = 0f;
                    foreach (var w in equippedWeapons)
                    {
                        if (w == null) continue;
                        w.GetComponentInChildren<WeaponBase>()?.StopRecoil();
                    }
                }
                else if ((held || pressed) && alternateTimer <= 0f)
                {
                    WeaponDefinitionSO def = GetAlternateDefinition();
                    float interval = 60f / def.alternateRPM;
                    alternateTimer = interval;

                    GameObject current = equippedWeapons[alternateIndex];
                    if (current != null)
                        current.GetComponentInChildren<WeaponBase>()?.Shoot();

                    alternateIndex = (alternateIndex + 1) % 2;
                }
            }
            else
            {
                for (int i = 0; i < equippedWeapons.Count; i++)
                {
                    if (equippedWeapons[i] == null) continue;
                    WeaponBase wb = equippedWeapons[i].GetComponentInChildren<WeaponBase>();
                    if (wb == null) continue;

                    bool shouldFire = wb.isAutomatic ? held : pressed;
                    if (shouldFire) wb.Shoot();
                    else if (released) wb.StopRecoil();
                }
            }
        }

        if (reloadAction != null && reloadAction.action.WasPressedThisFrame())
            ReloadActiveWeapon();

        if (scrollAction != null)
        {
            float scroll = scrollAction.action.ReadValue<float>();
            if (scroll > 0f) CycleSlot(-1);
            else if (scroll < 0f) CycleSlot(1);
        }

        if (slot1Action != null && slot1Action.action.WasPressedThisFrame()) SetActiveSlot(0);
        if (slot2Action != null && slot2Action.action.WasPressedThisFrame()) SetActiveSlot(1);
    }

    bool IsAlternateMode()
    {
        if (equippedWeapons[0] == null || equippedWeapons[1] == null) return false;
        var defA = GetWeaponDefinition(0);
        var defB = GetWeaponDefinition(1);
        if (defA == null || defB == null || defA != defB) return false;
        return defA.willAlternate;
    }

    WeaponDefinitionSO GetAlternateDefinition() => GetWeaponDefinition(0);

    WeaponDefinitionSO GetWeaponDefinition(int slot)
    {
        if (equippedWeapons[slot] == null) return null;
        WeaponBase wb = equippedWeapons[slot].GetComponentInChildren<WeaponBase>();
        return wb?.currentInstance?.definition;
    }

    Transform GetHolderForSlot(int slot) => slot == 0 ? leftWeaponHolder : rightWeaponHolder;

    void FireActiveWeapon()
    {
        GetActiveWeaponBase()?.Shoot();
    }

    void ReloadActiveWeapon()
    {
        GetActiveWeaponBase()?.Reload();
    }

    public void MaxAmmo()
    {
        foreach (GameObject w in equippedWeapons)
        {
            if (w == null) continue;
            w.GetComponentInChildren<WeaponBase>()?.Refill();
        }
    }

    public void PartialAmmoRefill(float percent)
    {
        foreach (GameObject w in equippedWeapons)
        {
            if (w == null) continue;
            WeaponBase wb = w.GetComponentInChildren<WeaponBase>();
            if (wb == null) continue;
            int amount = Mathf.RoundToInt(wb.maxReserve * percent);
            wb.reserveAmmo = Mathf.Min(wb.reserveAmmo + amount, wb.maxReserve);
        }
    }

    // --- Legacy WeaponData path ---

    public bool TryAddWeapon(WeaponData data, WeaponUpgradeData upgradeData = null)
    {
        if (data == null || data.prefab == null) return false;

        int emptySlot = GetEmptySlot();
        int targetSlot = emptySlot >= 0 ? emptySlot : activeSlot;

        if (equippedWeapons[targetSlot] != null) Destroy(equippedWeapons[targetSlot]);

        GameObject instance = InstantiateWeapon(data, targetSlot);
        equippedWeapons[targetSlot] = instance;
        equippedData[targetSlot] = data;
        equippedUpgradeData[targetSlot] = upgradeData;
        weaponLevels[targetSlot] = 1;
        SetActiveSlot(targetSlot);
        return true;
    }

    public void UpgradeWeaponInSlot(int slot, WeaponData newWeaponData)
    {
        if (slot < 0 || slot >= equippedWeapons.Count) return;
        if (weaponLevels[slot] >= 10) return;

        weaponLevels[slot]++;
        if (equippedWeapons[slot] != null) Destroy(equippedWeapons[slot]);

        GameObject instance = InstantiateWeapon(newWeaponData, slot);
        equippedWeapons[slot] = instance;
        equippedData[slot] = newWeaponData;
        SetActiveSlot(slot);
    }

    GameObject InstantiateWeapon(WeaponData data, int slot)
    {
        Transform holder = GetHolderForSlot(slot);
        GameObject instance = Instantiate(data.prefab, holder);
        instance.transform.localPosition = data.positionOffset;
        instance.transform.localRotation = Quaternion.Euler(data.rotationOffset);
        instance.SetActive(false);

        WeaponBase wb = instance.GetComponentInChildren<WeaponBase>();
        if (wb != null)
        {
            if (BulletPool.Instance != null && wb.bulletData != null && wb.bulletData.trailPrefab != null)
                BulletPool.Instance.EnsurePoolSize(wb.bulletData.trailPoolKey, wb.bulletData.trailPrefab.gameObject, wb.bulletData.trailPoolSize);
            if (playerStats != null)
            {
                wb.ApplyExtraMagazine(playerStats.extraMagazine);
                wb.currentMag = wb.maxMag;
            }
        }
        return instance;
    }

    // --- WeaponInstance path ---

    public void TryAddWeaponInstanceToSlot(WeaponInstance instance, int slot)
    {
        if (instance == null || instance.definition == null || (instance.definition.leftPrefab == null && instance.definition.rightPrefab == null))
        {
            Debug.LogWarning("[WeaponInventory] Invalid WeaponInstance.");
            return;
        }

        if (equippedWeapons[slot] != null) Destroy(equippedWeapons[slot]);

        GameObject go = InstantiateWeaponInstance(instance, slot);
        equippedWeapons[slot] = go;
        equippedData[slot] = null;
        equippedUpgradeData[slot] = null;
        weaponLevels[slot] = 1;
        SetActiveSlot(slot);

        Debug.Log($"[WeaponInventory] Equipped {instance.definition.weaponName} ({instance.rarity}) to {(slot == 0 ? "Left" : "Right")} hand.");
    }

    public void TryAddWeaponInstance(WeaponInstance instance)
    {
        int emptySlot = GetEmptySlot();
        TryAddWeaponInstanceToSlot(instance, emptySlot >= 0 ? emptySlot : activeSlot);
    }

    GameObject InstantiateWeaponInstance(WeaponInstance instance, int slot)
    {
        WeaponDefinitionSO def = instance.definition;
        Transform holder = GetHolderForSlot(slot);

        GameObject go = Instantiate(def.GetPrefabForSlot(slot), holder);
        go.transform.localPosition = def.GetPositionOffsetForSlot(slot);
        go.transform.localRotation = Quaternion.Euler(def.GetRotationOffsetForSlot(slot));
        go.SetActive(false);

        WeaponBase wb = go.GetComponentInChildren<WeaponBase>();
        if (wb != null)
        {
            wb.Equip(instance);

            if (BulletPool.Instance != null && def.bulletData != null && def.bulletData.trailPrefab != null)
                BulletPool.Instance.EnsurePoolSize(def.bulletData.trailPoolKey, def.bulletData.trailPrefab.gameObject, def.bulletData.trailPoolSize);

            if (playerStats != null)
            {
                wb.ApplyExtraMagazine(playerStats.extraMagazine);
                wb.currentMag = wb.maxMag;
            }
        }
        return go;
    }

    // --- Slot management ---

    int GetEmptySlot()
    {
        for (int i = 0; i < equippedWeapons.Count; i++)
            if (equippedWeapons[i] == null) return i;
        return -1;
    }

    void ResetControllerToBase()
    {
        if (fpsController == null) return;
        fpsController.DashCharges = fpsController.BaseDashCharges;
        fpsController.WallJumpCount = fpsController.BaseWallJumpCount;
        fpsController.JumpCount = fpsController.BaseJumpCount;
    }

    public void SetActiveSlot(int slot)
    {
        if (slot < 0 || slot >= equippedWeapons.Count) return;
        if (equippedWeapons[slot] == null) return;

        ResetControllerToBase();

        for (int i = 0; i < equippedWeapons.Count; i++)
            if (equippedWeapons[i] != null) equippedWeapons[i].SetActive(true);

        activeSlot = slot;

        WeaponBase wb = equippedWeapons[slot].GetComponentInChildren<WeaponBase>();
        if (wb != null)
        {
            wb.LoadRecoilValues();
            if (playerStats != null)
            {
                wb.ApplyAttackSpeed(playerStats.attackSpeed);
                wb.ApplyExtraMagazine(playerStats.extraMagazine);
                wb.critChance = playerStats.critChance;
                wb.critMultiplier = playerStats.critMultiplier;
            }

            activeInstance = wb.currentInstance;
            wb.ApplyPerks(activeInstance?.rolledPerks);
        }
        else
        {
            activeInstance = null;
        }

        if (ikHandler != null)
            ikHandler.UpdateIKTargets(equippedWeapons[slot]);

        if (equippedData[slot] != null)
            Debug.Log($"[WeaponInventory] Active slot {slot}: {equippedData[slot].weaponName}.");
    }

    public void SwitchToSlot(int slot) => SetActiveSlot(slot);

    void CycleSlot(int direction)
    {
        int next = (activeSlot + direction + 2) % 2;
        if (equippedWeapons[next] != null) SetActiveSlot(next);
    }

    // Returns the WeaponBase for a specific slot (0 = left, 1 = right)
    public WeaponBase GetWeaponBase(int slot)
    {
        if (slot < 0 || slot >= equippedWeapons.Count) return null;
        if (equippedWeapons[slot] == null) return null;
        return equippedWeapons[slot].GetComponentInChildren<WeaponBase>();
    }

    public WeaponBase GetActiveWeaponBase() => GetWeaponBase(activeSlot);

    public WeaponData GetActiveWeaponData() => equippedData[activeSlot];

    public int GetWeaponLevel(int slot)
    {
        if (slot < 0 || slot >= weaponLevels.Count) return 0;
        return weaponLevels[slot];
    }

    public WeaponUpgradeData GetWeaponUpgradeData(int slot)
    {
        if (slot < 0 || slot >= equippedUpgradeData.Count) return null;
        return equippedUpgradeData[slot];
    }
}