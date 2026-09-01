using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : MonoBehaviour
{
    public enum Hand { Left, Right }

    private const int MaxPerHand = 2;

    [Header("References")]
    public Transform rightWeaponHolder;
    public Transform leftWeaponHolder;
    public PlayerStats playerStats;
    public IKWeaponHandler ikHandler;
    public FPSInput input;

    [Header("Input")]
    public InputActionReference rightFireAction;
    public InputActionReference leftFireAction;
    public InputActionReference rightSwapAction;
    public InputActionReference leftSwapAction;

    [Header("Starting Loadout")]
    public List<WeaponDefinitionSO> startingRightHandWeapons = new List<WeaponDefinitionSO>();
    public List<WeaponDefinitionSO> startingLeftHandWeapons = new List<WeaponDefinitionSO>();

    [Header("Weapons")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();

    private class HandState
    {
        public readonly List<WeaponEntry> equipped = new List<WeaponEntry>();
        public int activeIndex = -1;

        public WeaponEntry ActiveEntry =>
            activeIndex >= 0 && activeIndex < equipped.Count
                ? equipped[activeIndex]
                : null;

        public WeaponBase ActiveWeaponBase => ActiveEntry?.Primary;
    }

    private readonly HandState rightHand = new HandState();
    private readonly HandState leftHand = new HandState();

    private readonly Dictionary<WeaponDefinitionSO, WeaponEntry> rightWeaponLookup =
        new Dictionary<WeaponDefinitionSO, WeaponEntry>();

    private readonly Dictionary<WeaponDefinitionSO, WeaponEntry> leftWeaponLookup =
        new Dictionary<WeaponDefinitionSO, WeaponEntry>();

    private HandState GetHand(Hand hand)
    {
        return hand == Hand.Left ? leftHand : rightHand;
    }

    private Dictionary<WeaponDefinitionSO, WeaponEntry> GetLookup(Hand hand)
    {
        return hand == Hand.Left ? leftWeaponLookup : rightWeaponLookup;
    }

    void Awake()
    {
        rightWeaponLookup.Clear();
        leftWeaponLookup.Clear();

        foreach (WeaponEntry entry in weapons)
        {
            if (entry == null || entry.definition == null || entry.weaponRoot == null)
            {
                Debug.LogWarning("[WeaponInventory] Invalid WeaponEntry.");
                continue;
            }

            CreateRuntimeDefinition(entry);

            entry.weaponRoot.SetActive(false);

            Transform weaponTransform = entry.weaponRoot.transform;

            if (rightWeaponHolder != null &&
                (weaponTransform == rightWeaponHolder || weaponTransform.IsChildOf(rightWeaponHolder)))
            {
                if (rightWeaponLookup.ContainsKey(entry.definition))
                {
                    Debug.LogWarning($"[WeaponInventory] Duplicate RIGHT weapon definition: {entry.definition.weaponName}");
                }
                else
                {
                    rightWeaponLookup.Add(entry.definition, entry);
                }
            }
            else if (leftWeaponHolder != null &&
                     (weaponTransform == leftWeaponHolder || weaponTransform.IsChildOf(leftWeaponHolder)))
            {
                if (leftWeaponLookup.ContainsKey(entry.definition))
                {
                    Debug.LogWarning($"[WeaponInventory] Duplicate LEFT weapon definition: {entry.definition.weaponName}");
                }
                else
                {
                    leftWeaponLookup.Add(entry.definition, entry);
                }
            }
            else
            {
                Debug.LogWarning($"[WeaponInventory] {entry.weaponRoot.name} is not under the left or right weapon holder.");
            }
        }

        if (rightFireAction != null)
            rightFireAction.action.Enable();

        if (leftFireAction != null)
            leftFireAction.action.Enable();

        if (rightSwapAction != null)
            rightSwapAction.action.Enable();

        if (leftSwapAction != null)
            leftSwapAction.action.Enable();
    }

    void Start()
    {
        foreach (WeaponEntry entry in weapons)
        {
            if (entry == null || entry.runtimeDefinition == null)
                continue;

            WeaponDefinitionSO definition = entry.runtimeDefinition;

            if (BulletPool.Instance != null && definition.trailPrefab != null)
            {
                BulletPool.Instance.EnsurePoolSize(
                    definition.trailPoolKey,
                    definition.trailPrefab.gameObject,
                    definition.trailPoolSize
                );
            }

            if (ProjectilePool.Instance != null &&
                definition.bulletType == BulletType.Projectile &&
                definition.projectilePrefab != null)
            {
                ProjectilePool.Instance.EnsurePoolSize(
                    definition.projectilePrefab,
                    8
                );
            }
        }

        SetupHand(
            rightHand,
            startingRightHandWeapons,
            rightWeaponLookup,
            Hand.Right
        );

        SetupHand(
            leftHand,
            startingLeftHandWeapons,
            leftWeaponLookup,
            Hand.Left
        );
    }

    void OnDisable()
    {
        if (rightFireAction != null)
            rightFireAction.action.Disable();

        if (leftFireAction != null)
            leftFireAction.action.Disable();

        if (rightSwapAction != null)
            rightSwapAction.action.Disable();

        if (leftSwapAction != null)
            leftSwapAction.action.Disable();
    }

    void CreateRuntimeDefinition(WeaponEntry entry)
    {
        entry.runtimeDefinition = Instantiate(entry.definition);

        entry.runtimeDefinition.level = 1;
        entry.runtimeDefinition.currentXP = 0f;
        entry.runtimeDefinition.usedEvolutions.Clear();

        foreach (WeaponBase weaponBase in entry.weaponBases)
        {
            if (weaponBase == null)
                continue;

            weaponBase.weaponDefinition = entry.runtimeDefinition;
            weaponBase.ApplyLevel(entry.runtimeDefinition);
            weaponBase.RefreshWeaponSkin();
        }
    }

    void ResetWeaponProgress(WeaponEntry entry)
    {
        if (entry == null || entry.runtimeDefinition == null)
            return;

        entry.runtimeDefinition.level = 1;
        entry.runtimeDefinition.currentXP = 0f;
        entry.runtimeDefinition.usedEvolutions.Clear();

        foreach (WeaponBase weaponBase in entry.weaponBases)
        {
            if (weaponBase == null)
                continue;

            weaponBase.weaponDefinition = entry.runtimeDefinition;
            weaponBase.ApplyLevel(entry.runtimeDefinition);
            weaponBase.RefreshWeaponSkin();
        }
    }

    void SetupHand(
        HandState hand,
        List<WeaponDefinitionSO> definitions,
        Dictionary<WeaponDefinitionSO, WeaponEntry> lookup,
        Hand handType)
    {
        hand.equipped.Clear();
        hand.activeIndex = -1;

        if (definitions == null)
            return;

        foreach (WeaponDefinitionSO definition in definitions)
        {
            if (definition == null)
                continue;

            if (hand.equipped.Count >= MaxPerHand)
            {
                Debug.LogWarning($"[WeaponInventory] {handType} hand already has {MaxPerHand} weapons.");
                break;
            }

            if (!lookup.TryGetValue(definition, out WeaponEntry entry))
            {
                Debug.LogWarning($"[WeaponInventory] Could not find {definition.weaponName} for {handType} hand.");
                continue;
            }

            if (!hand.equipped.Contains(entry))
                hand.equipped.Add(entry);
        }

        if (hand.equipped.Count > 0)
        {
            EquipIndexCore(hand, 0);
        }
        else
        {
            Debug.LogWarning($"[WeaponInventory] No starting weapons found for {handType} hand.");
        }
    }

    void Update()
    {
        if (rightSwapAction != null && rightSwapAction.action.WasPressedThisFrame())
            SwapHand(rightHand);

        if (leftSwapAction != null && leftSwapAction.action.WasPressedThisFrame())
            SwapHand(leftHand);

        HandleFire(rightHand, rightFireAction);
        HandleFire(leftHand, leftFireAction);

        // Handle reload input
        HandleReload();
    }

    void SwapHand(HandState hand)
    {
        if (hand.equipped.Count <= 1)
            return;

        int next = (hand.activeIndex + 1) % hand.equipped.Count;
        EquipIndexCore(hand, next);
    }

    void HandleFire(HandState hand, InputActionReference fireAction)
    {
        if (fireAction == null)
            return;

        WeaponEntry entry = hand.ActiveEntry;

        if (entry == null)
            return;

        foreach (WeaponBase weaponBase in entry.weaponBases)
        {
            if (weaponBase == null)
                continue;

            bool shouldFire = weaponBase.isAutomatic
                ? fireAction.action.IsPressed()
                : fireAction.action.WasPressedThisFrame();

            if (shouldFire)
            {
                weaponBase.Shoot();
            }
            else if (fireAction.action.WasReleasedThisFrame())
            {
                weaponBase.StopRecoil();
            }
        }
    }

    void HandleReload()
    {
        // Check if reload was pressed
        if (input == null || input.reloadAction == null) return;
        if (!input.ReloadPressed) return;

        bool anyReloaded = false;

        // Try to reload right hand weapon
        WeaponBase rightWeapon = rightHand.ActiveWeaponBase;
        if (rightWeapon != null)
        {
            // Only reload if not full and not already reloading
            if (rightWeapon.currentAmmo < rightWeapon.MaxAmmo && !rightWeapon.IsReloading)
            {
                rightWeapon.Reload();
                anyReloaded = true;
            }
        }

        // Try to reload left hand weapon
        WeaponBase leftWeapon = leftHand.ActiveWeaponBase;
        if (leftWeapon != null)
        {
            // Only reload if not full and not already reloading
            if (leftWeapon.currentAmmo < leftWeapon.MaxAmmo && !leftWeapon.IsReloading)
            {
                leftWeapon.Reload();
                anyReloaded = true;
            }
        }

        // Optional: Play a sound or show feedback if no weapon could reload
        if (!anyReloaded)
        {
            // Both weapons are either full or already reloading
            // You could play a "click" sound here if desired
        }
    }

    void EquipIndexCore(HandState hand, int index)
    {
        if (index < 0 || index >= hand.equipped.Count)
            return;

        WeaponEntry previous = hand.ActiveEntry;

        if (previous != null && previous.weaponRoot != null)
            previous.weaponRoot.SetActive(false);

        hand.activeIndex = index;

        WeaponEntry next = hand.ActiveEntry;

        if (next == null || next.weaponRoot == null)
            return;

        next.weaponRoot.SetActive(true);

        foreach (WeaponBase weaponBase in next.weaponBases)
        {
            if (weaponBase == null)
                continue;

            weaponBase.weaponDefinition = next.runtimeDefinition;
            weaponBase.LoadRecoilValues();
            weaponBase.RefreshWeaponSkin();
        }

        if (ikHandler != null)
            ikHandler.UpdateIKTargets(next.weaponRoot);
    }

    public void EquipIndex(Hand hand, int index)
    {
        EquipIndexCore(GetHand(hand), index);
    }

    public void Swap(Hand hand)
    {
        SwapHand(GetHand(hand));
    }

    public int AddWeapon(WeaponDefinitionSO definition, Hand hand)
    {
        if (definition == null)
            return -1;

        Dictionary<WeaponDefinitionSO, WeaponEntry> lookup = GetLookup(hand);

        if (!lookup.TryGetValue(definition, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot add {definition.weaponName} to {hand} hand.");
            return -1;
        }

        return AddEntry(GetHand(hand), entry);
    }

    public int AddWeaponByIndex(int index, Hand hand)
    {
        Dictionary<WeaponDefinitionSO, WeaponEntry> lookup = GetLookup(hand);
        List<WeaponEntry> handWeapons = new List<WeaponEntry>(lookup.Values);

        if (index < 0 || index >= handWeapons.Count)
        {
            Debug.LogWarning($"[WeaponInventory] AddWeaponByIndex index {index} out of range.");
            return -1;
        }

        return AddEntry(GetHand(hand), handWeapons[index]);
    }

    int AddEntry(HandState hand, WeaponEntry entry)
    {
        if (entry == null || entry.weaponRoot == null)
            return -1;

        int existingIndex = hand.equipped.IndexOf(entry);

        if (existingIndex >= 0)
        {
            EquipIndexCore(hand, existingIndex);
            return existingIndex;
        }

        if (hand.equipped.Count >= MaxPerHand)
        {
            Debug.LogWarning("[WeaponInventory] Hand is full.");
            return -1;
        }

        hand.equipped.Add(entry);

        foreach (WeaponBase weaponBase in entry.weaponBases)
        {
            if (weaponBase == null)
                continue;

            weaponBase.weaponDefinition = entry.runtimeDefinition;
            weaponBase.ApplyLevel(entry.runtimeDefinition);
            weaponBase.RefreshWeaponSkin();
        }

        int newIndex = hand.equipped.Count - 1;

        EquipIndexCore(hand, newIndex);

        return newIndex;
    }

    public void RemoveWeapon(WeaponDefinitionSO definition, Hand hand)
    {
        if (definition == null)
            return;

        Dictionary<WeaponDefinitionSO, WeaponEntry> lookup = GetLookup(hand);

        if (!lookup.TryGetValue(definition, out WeaponEntry entry))
            return;

        RemoveEntry(GetHand(hand), entry);
    }

    public void RemoveWeaponAt(int index, Hand hand)
    {
        HandState state = GetHand(hand);

        if (index < 0 || index >= state.equipped.Count)
            return;

        RemoveEntry(state, state.equipped[index]);
    }

    void RemoveEntry(HandState hand, WeaponEntry entry)
    {
        int index = hand.equipped.IndexOf(entry);

        if (index < 0)
            return;

        bool wasActive = index == hand.activeIndex;

        if (entry.weaponRoot != null)
            entry.weaponRoot.SetActive(false);

        hand.equipped.RemoveAt(index);

        ResetWeaponProgress(entry);

        if (hand.equipped.Count == 0)
        {
            hand.activeIndex = -1;
            return;
        }

        if (wasActive)
        {
            int newIndex = Mathf.Clamp(index, 0, hand.equipped.Count - 1);
            EquipIndexCore(hand, newIndex);
        }
        else if (index < hand.activeIndex)
        {
            hand.activeIndex--;
        }
    }

    public void LevelUpWeapon(WeaponDefinitionSO definition, Hand hand)
    {
        if (definition == null)
            return;

        Dictionary<WeaponDefinitionSO, WeaponEntry> lookup = GetLookup(hand);

        if (!lookup.TryGetValue(definition, out WeaponEntry entry))
        {
            Debug.LogWarning($"[WeaponInventory] Cannot level up {definition.weaponName} in {hand} hand.");
            return;
        }

        if (!GetHand(hand).equipped.Contains(entry))
        {
            Debug.LogWarning($"[WeaponInventory] {definition.weaponName} is not equipped in {hand} hand.");
            return;
        }

        WeaponDefinitionSO runtimeDefinition = entry.runtimeDefinition;

        runtimeDefinition.level = Mathf.Min(
            runtimeDefinition.level + 1,
            runtimeDefinition.maxLevel
        );

        foreach (WeaponBase weaponBase in entry.weaponBases)
        {
            if (weaponBase == null)
                continue;

            weaponBase.ApplyLevel(runtimeDefinition);
            weaponBase.RefreshWeaponSkin();
        }
    }

    public int ActiveIndex(Hand hand)
    {
        return GetHand(hand).activeIndex;
    }

    public int EquippedCount(Hand hand)
    {
        return GetHand(hand).equipped.Count;
    }

    public WeaponBase GetEquippedAt(Hand hand, int index)
    {
        HandState state = GetHand(hand);

        if (index < 0 || index >= state.equipped.Count)
            return null;

        return state.equipped[index].Primary;
    }

    public bool HasWeapon(WeaponDefinitionSO definition, Hand hand)
    {
        if (definition == null)
            return false;

        foreach (WeaponEntry entry in GetHand(hand).equipped)
        {
            if (entry != null && entry.definition == definition)
                return true;
        }

        return false;
    }

    public bool HasWeapon(WeaponDefinitionSO definition)
    {
        return HasWeapon(definition, Hand.Left) ||
               HasWeapon(definition, Hand.Right);
    }

    public WeaponBase GetActiveWeapon(Hand hand)
    {
        return GetHand(hand).ActiveWeaponBase;
    }

    public int GetLevel(WeaponDefinitionSO definition, Hand hand)
    {
        Dictionary<WeaponDefinitionSO, WeaponEntry> lookup = GetLookup(hand);

        if (!lookup.TryGetValue(definition, out WeaponEntry entry))
            return 0;

        return entry.runtimeDefinition != null
            ? entry.runtimeDefinition.level
            : 0;
    }

    public WeaponDefinitionSO GetRuntimeDefinition(Hand hand)
    {
        WeaponEntry entry = GetHand(hand).ActiveEntry;

        if (entry == null)
            return null;

        return entry.runtimeDefinition;
    }
}