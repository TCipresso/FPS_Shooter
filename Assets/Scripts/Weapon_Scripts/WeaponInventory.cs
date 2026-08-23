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

            entry.weaponRoot.SetActive(false);

            Transform weaponTransform = entry.weaponRoot.transform;

            if (rightWeaponHolder != null &&
                (weaponTransform == rightWeaponHolder || weaponTransform.IsChildOf(rightWeaponHolder)))
            {
                if (rightWeaponLookup.ContainsKey(entry.definition))
                {
                    Debug.LogWarning(
                        $"[WeaponInventory] Duplicate RIGHT weapon definition: {entry.definition.weaponName}"
                    );
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
                    Debug.LogWarning(
                        $"[WeaponInventory] Duplicate LEFT weapon definition: {entry.definition.weaponName}"
                    );
                }
                else
                {
                    leftWeaponLookup.Add(entry.definition, entry);
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[WeaponInventory] {entry.weaponRoot.name} is not under the left or right weapon holder."
                );
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
            if (entry == null || entry.definition == null)
                continue;

            if (BulletPool.Instance != null && entry.definition.trailPrefab != null)
            {
                BulletPool.Instance.EnsurePoolSize(
                    entry.definition.trailPoolKey,
                    entry.definition.trailPrefab.gameObject,
                    entry.definition.trailPoolSize
                );
            }

            if (ProjectilePool.Instance != null &&
                entry.definition.bulletType == BulletType.Projectile &&
                entry.definition.projectilePrefab != null)
            {
                ProjectilePool.Instance.EnsurePoolSize(
                    entry.definition.projectilePrefab,
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
                Debug.LogWarning(
                    $"[WeaponInventory] {handType} hand already has {MaxPerHand} weapons."
                );
                break;
            }

            if (!lookup.TryGetValue(definition, out WeaponEntry entry))
            {
                Debug.LogWarning(
                    $"[WeaponInventory] Could not find {definition.weaponName} for {handType} hand."
                );
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
            Debug.LogWarning(
                $"[WeaponInventory] No starting weapons found for {handType} hand."
            );
        }
    }

    void Update()
    {
        if (rightSwapAction != null &&
            rightSwapAction.action.WasPressedThisFrame())
        {
            SwapHand(rightHand);
        }

        if (leftSwapAction != null &&
            leftSwapAction.action.WasPressedThisFrame())
        {
            SwapHand(leftHand);
        }

        HandleFire(rightHand, rightFireAction);
        HandleFire(leftHand, leftFireAction);
    }

    void SwapHand(HandState hand)
    {
        if (hand.equipped.Count <= 1)
            return;

        int next = (hand.activeIndex + 1) % hand.equipped.Count;
        EquipIndexCore(hand, next);
    }

    void HandleFire(
        HandState hand,
        InputActionReference fireAction)
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

            bool shouldFire =
                weaponBase.isAutomatic
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

    void EquipIndexCore(
        HandState hand,
        int index)
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

            weaponBase.weaponDefinition = next.definition;

            if (next.definition != null)
                weaponBase.ApplyLevel(next.definition);

            weaponBase.LoadRecoilValues();
        }

        if (ikHandler != null)
            ikHandler.UpdateIKTargets(next.weaponRoot);
    }

    public void EquipIndex(
        Hand hand,
        int index)
    {
        EquipIndexCore(
            GetHand(hand),
            index
        );
    }

    public void Swap(Hand hand)
    {
        SwapHand(GetHand(hand));
    }

    public int AddWeapon(
        WeaponDefinitionSO definition,
        Hand hand)
    {
        if (definition == null)
            return -1;

        Dictionary<WeaponDefinitionSO, WeaponEntry> lookup =
            GetLookup(hand);

        if (!lookup.TryGetValue(definition, out WeaponEntry entry))
        {
            Debug.LogWarning(
                $"[WeaponInventory] Cannot add {definition.weaponName} to {hand} hand."
            );
            return -1;
        }

        return AddEntry(
            GetHand(hand),
            entry
        );
    }

    public int AddWeaponByIndex(
        int index,
        Hand hand)
    {
        Dictionary<WeaponDefinitionSO, WeaponEntry> lookup =
            GetLookup(hand);

        List<WeaponEntry> handWeapons =
            new List<WeaponEntry>(lookup.Values);

        if (index < 0 || index >= handWeapons.Count)
        {
            Debug.LogWarning(
                $"[WeaponInventory] AddWeaponByIndex index {index} out of range."
            );
            return -1;
        }

        return AddEntry(
            GetHand(hand),
            handWeapons[index]
        );
    }

    int AddEntry(
        HandState hand,
        WeaponEntry entry)
    {
        if (entry == null || entry.weaponRoot == null)
            return -1;

        int existingIndex =
            hand.equipped.IndexOf(entry);

        if (existingIndex >= 0)
        {
            EquipIndexCore(
                hand,
                existingIndex
            );
            return existingIndex;
        }

        if (hand.equipped.Count >= MaxPerHand)
        {
            Debug.LogWarning(
                $"[WeaponInventory] Hand is full."
            );
            return -1;
        }

        hand.equipped.Add(entry);

        int newIndex =
            hand.equipped.Count - 1;

        EquipIndexCore(
            hand,
            newIndex
        );

        return newIndex;
    }

    public void RemoveWeapon(
        WeaponDefinitionSO definition,
        Hand hand)
    {
        if (definition == null)
            return;

        Dictionary<WeaponDefinitionSO, WeaponEntry> lookup =
            GetLookup(hand);

        if (!lookup.TryGetValue(definition, out WeaponEntry entry))
            return;

        RemoveEntry(
            GetHand(hand),
            entry
        );
    }

    public void RemoveWeaponAt(
        int index,
        Hand hand)
    {
        HandState state =
            GetHand(hand);

        if (index < 0 || index >= state.equipped.Count)
            return;

        RemoveEntry(
            state,
            state.equipped[index]
        );
    }

    void RemoveEntry(
        HandState hand,
        WeaponEntry entry)
    {
        int index =
            hand.equipped.IndexOf(entry);

        if (index < 0)
            return;

        bool wasActive =
            index == hand.activeIndex;

        if (entry.weaponRoot != null)
            entry.weaponRoot.SetActive(false);

        hand.equipped.RemoveAt(index);

        if (hand.equipped.Count == 0)
        {
            hand.activeIndex = -1;
            return;
        }

        if (wasActive)
        {
            int newIndex =
                Mathf.Clamp(
                    index,
                    0,
                    hand.equipped.Count - 1
                );

            EquipIndexCore(
                hand,
                newIndex
            );
        }
        else if (index < hand.activeIndex)
        {
            hand.activeIndex--;
        }
    }

    public void LevelUpWeapon(
        WeaponDefinitionSO definition)
    {
        if (definition == null)
            return;

        WeaponEntry entry = null;

        if (!rightWeaponLookup.TryGetValue(definition, out entry))
            leftWeaponLookup.TryGetValue(definition, out entry);

        if (entry == null)
        {
            Debug.LogWarning(
                $"[WeaponInventory] Cannot level up {definition.weaponName}."
            );
            return;
        }

        definition.level =
            Mathf.Min(
                definition.level + 1,
                definition.maxLevel
            );

        foreach (WeaponBase weaponBase in entry.weaponBases)
        {
            if (weaponBase == null)
                continue;

            weaponBase.ApplyLevel(definition);
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

    public WeaponBase GetEquippedAt(
        Hand hand,
        int index)
    {
        HandState state =
            GetHand(hand);

        if (index < 0 || index >= state.equipped.Count)
            return null;

        return state.equipped[index].Primary;
    }

    public bool HasWeapon(
        WeaponDefinitionSO definition,
        Hand hand)
    {
        if (definition == null)
            return false;

        foreach (WeaponEntry entry in GetHand(hand).equipped)
        {
            if (entry != null &&
                entry.definition == definition)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasWeapon(
        WeaponDefinitionSO definition)
    {
        return HasWeapon(definition, Hand.Left) ||
               HasWeapon(definition, Hand.Right);
    }

    public WeaponBase GetActiveWeapon(
        Hand hand)
    {
        return GetHand(hand).ActiveWeaponBase;
    }

    public int GetLevel(
        WeaponDefinitionSO definition)
    {
        if (definition == null)
            return 0;

        return definition.level;
    }
}