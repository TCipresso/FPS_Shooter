using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class WeaponInventory : MonoBehaviour
{
    public enum Hand { Left, Right }

    private const int MaxPerHand = 1;

    [Header("References")]
    public Transform rightWeaponHolder;
    public Transform leftWeaponHolder;
    public PlayerStats playerStats;
    public FPSInput input;

    [Header("Input")]
    public InputActionReference rightFireAction;
    public InputActionReference leftFireAction;
    [UnityEngine.Serialization.FormerlySerializedAs("rightSwapAction")]
    public InputActionReference rightThrowAction;
    [UnityEngine.Serialization.FormerlySerializedAs("leftSwapAction")]
    public InputActionReference leftThrowAction;

    [Header("Throw")]
    public Transform aimTransform;
    public Transform throwOrigin;
    public float throwSpeed = 15f;
    public float throwUpwardSpeed = 2f;
    public float throwSpin = 8f;

    [Header("Starting Loadout")]
    public List<WeaponDefinitionSO> startingRightHandWeapons = new List<WeaponDefinitionSO>();
    public List<WeaponDefinitionSO> startingLeftHandWeapons = new List<WeaponDefinitionSO>();

    [Header("Weapons")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();

    [Header("Melee Off-Hand Reaction")]
    public Transform rightHandParent;
    public Transform leftHandParent;
    public float offHandLowerAmount = 0.3f;
    public float offHandLowerSpeed = 10f;

    private Vector3 rightHandRestPosition;
    private Vector3 leftHandRestPosition;
    private FPSLook fpsLook;

    private class HandState
    {
        public readonly List<WeaponEntry> equipped = new List<WeaponEntry>();
        public int activeIndex = -1;
        public bool wasSwinging;

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
        fpsLook = GetComponentInParent<FPSLook>();
        if (input == null)
            input = GetComponentInParent<FPSInput>();
        if (aimTransform == null && Camera.main != null)
            aimTransform = Camera.main.transform;

        if (rightHandParent != null)
            rightHandRestPosition = rightHandParent.localPosition;
        if (leftHandParent != null)
            leftHandRestPosition = leftHandParent.localPosition;

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
            if (entry.weaponBases.Count == 0)
                entry.weaponBases.AddRange(entry.weaponRoot.GetComponentsInChildren<WeaponBase>(true));
            CreateRuntimeDefinition(entry);

            Transform weaponTransform = entry.weaponRoot.transform;

            if (rightWeaponHolder != null &&
                (weaponTransform == rightWeaponHolder || weaponTransform.IsChildOf(rightWeaponHolder)))
            {
                if (entry.definition.isMelee)
                {
                    Debug.LogWarning($"[WeaponInventory] Move {entry.weaponRoot.name} to the left weapon holder.");
                    continue;
                }
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
                if (!entry.definition.isMelee)
                {
                    Debug.LogWarning($"[WeaponInventory] Move {entry.weaponRoot.name} to the right weapon holder.");
                    continue;
                }
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

    }

    void OnEnable()
    {
        if (rightFireAction != null)
            rightFireAction.action.Enable();

        if (leftFireAction != null)
            leftFireAction.action.Enable();

        if (rightThrowAction != null)
            rightThrowAction.action.Enable();

        if (leftThrowAction != null)
            leftThrowAction.action.Enable();
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

        if (rightThrowAction != null)
            rightThrowAction.action.Disable();

        if (leftThrowAction != null)
            leftThrowAction.action.Disable();
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
    }

    void Update()
    {
        HandleFire(rightHand, rightFireAction);
        HandleFire(leftHand, leftFireAction);

        HandleMelee(rightHand);
        HandleMelee(leftHand);

        UpdateHandReaction(rightHand, leftHand, rightHandParent, rightHandRestPosition);
        UpdateHandReaction(leftHand, rightHand, leftHandParent, leftHandRestPosition);

    }

    void LateUpdate()
    {
        if (rightThrowAction != null && rightThrowAction.action.WasPressedThisFrame())
            ThrowWeapon(Hand.Right);

        if (leftThrowAction != null && leftThrowAction.action.WasPressedThisFrame())
            ThrowWeapon(Hand.Left);
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

            // Melee weapons are driven by the dedicated attack/parry inputs instead.
            if (weaponBase.IsMelee)
                continue;

            if (weaponBase.isLowered || !weaponBase.HasAmmo())
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

    void HandleMelee(HandState hand)
    {
        WeaponBase weaponBase = hand.ActiveWeaponBase;
        if (weaponBase == null || !weaponBase.IsMelee)
            return;

        if ((input != null && input.MeleePressed) ||
            (leftFireAction != null && leftFireAction.action.WasPressedThisFrame()))
            weaponBase.PlayMeleeAttack();
    }

    void UpdateHandReaction(HandState hand, HandState otherHand, Transform handParent, Vector3 restPosition)
    {
        bool ownSwinging = IsSwinging(hand);
        bool otherSwinging = IsSwinging(otherHand);

        WeaponBase weaponBase = hand.ActiveWeaponBase;
        if (weaponBase != null)
            weaponBase.isLowered = otherSwinging;

        if (handParent == null)
            return;

        if (otherSwinging)
        {
            // Off-hand: pinned lowered for as long as the other hand is swinging,
            // every frame - can never get caught mid-rise when the next swing starts.
            handParent.localPosition = restPosition + Vector3.down * offHandLowerAmount;
            hand.wasSwinging = ownSwinging;
            return;
        }

        if (ownSwinging && !hand.wasSwinging)
        {
            // A new swing of OUR OWN just started - always play it from the rest
            // position, even if we were mid-lower/mid-rise from the last one.
            handParent.localPosition = restPosition;
        }
        else if (!ownSwinging && hand.wasSwinging)
        {
            // Our own swing just ended - drop to the lowered "waiting" pose.
            handParent.localPosition = restPosition + Vector3.down * offHandLowerAmount;
        }
        hand.wasSwinging = ownSwinging;

        if (ownSwinging)
            return;

        handParent.localPosition = Vector3.MoveTowards(handParent.localPosition, restPosition, offHandLowerSpeed * Time.deltaTime);
    }

    bool IsSwinging(HandState hand)
    {
        WeaponBase weaponBase = hand.ActiveWeaponBase;
        return weaponBase != null && weaponBase.IsMelee && weaponBase.isMeleeComboActive;
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

        foreach (WeaponBase weaponBase in entry.weaponBases)
        {
            if (weaponBase != null)
                weaponBase.CancelReload();
        }

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

    public bool TryPickup(WeaponPickup pickup)
    {
        if (pickup == null || !pickup.CanPickup || pickup.definition == null)
            return false;

        Hand handType = pickup.definition.isMelee ? Hand.Left : Hand.Right;
        if (!GetLookup(handType).TryGetValue(pickup.definition, out WeaponEntry entry) ||
            entry.weaponRoot == null)
            return false;

        HandState hand = GetHand(handType);
        if (hand.ActiveEntry != null)
            return false;

        Destroy(entry.runtimeDefinition);
        entry.runtimeDefinition = pickup.CreateRuntimeDefinition();
        foreach (WeaponBase weaponBase in entry.weaponBases)
        {
            if (weaponBase != null)
                weaponBase.weaponDefinition = entry.runtimeDefinition;
        }

        if (AddEntry(hand, entry) < 0)
            return false;

        pickup.RestoreAmmo(entry);
        pickup.Consume();
        return true;
    }

    public bool ThrowWeapon(Hand hand)
    {
        HandState state = GetHand(hand);
        WeaponEntry entry = state.ActiveEntry;
        if (entry == null)
            return false;

        GameObject prefab = entry.definition.throwablePrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[WeaponInventory] {entry.definition.weaponName}: Throwable Prefab is empty on definition {entry.definition.name}.", entry.definition);
            return false;
        }

        if (prefab.GetComponent<ThrownWeapon>() == null)
        {
            Debug.LogWarning($"[WeaponInventory] {entry.definition.weaponName}: Add ThrownWeapon to the root of throwable prefab {prefab.name}. A component on a child does not count.", prefab);
            return false;
        }

        WeaponPickup pickupComponent = prefab.GetComponentInChildren<WeaponPickup>(true);
        if (pickupComponent != null)
        {
            Debug.LogWarning($"[WeaponInventory] {entry.definition.weaponName}: Remove WeaponPickup from {pickupComponent.gameObject.name} inside throwable prefab {prefab.name}.", pickupComponent);
            return false;
        }

        if (prefab.GetComponent<Rigidbody>() == null)
        {
            Debug.LogWarning($"[WeaponInventory] {entry.definition.weaponName}: Add a Rigidbody to the root of throwable prefab {prefab.name}. A Rigidbody on a child does not count.", prefab);
            return false;
        }

        if (!System.Array.Exists(prefab.GetComponentsInChildren<Collider>(true),
                collider => collider.enabled && !collider.isTrigger))
        {
            Debug.LogWarning($"[WeaponInventory] {entry.definition.weaponName}: Throwable prefab {prefab.name} needs an enabled 3D collider with Is Trigger turned off.", prefab);
            return false;
        }

        Transform aim = fpsLook != null && fpsLook.playerCamera != null
            ? fpsLook.playerCamera.transform
            : aimTransform != null ? aimTransform : transform;
        Vector3 position = throwOrigin != null ? throwOrigin.position : aim.position;
        Quaternion rotation = aim.rotation * Quaternion.Euler(prefab.GetComponent<ThrownWeapon>().rotationOffset);
        GameObject thrown = Instantiate(prefab, position, rotation);
        thrown.SetActive(true);
        thrown.GetComponent<ThrownWeapon>().Launch(
            aim.forward * throwSpeed + Vector3.up * throwUpwardSpeed,
            throwSpin, transform, aim.rotation);
        RemoveEntry(state, entry);
        return true;
    }

    public bool DropWeapon(Hand handType, bool throwWeapon = false)
    {
        if (throwWeapon)
            return ThrowWeapon(handType);

        HandState hand = GetHand(handType);
        WeaponEntry entry = hand.ActiveEntry;
        if (entry == null)
            return false;

        GameObject prefab = entry.definition.dropPrefab;
        if (prefab == null || prefab.GetComponent<WeaponPickup>() == null ||
            prefab.GetComponent<Rigidbody>() == null ||
            !System.Array.Exists(prefab.GetComponentsInChildren<Collider>(true),
                collider => collider.enabled && !collider.isTrigger))
        {
            Debug.LogWarning($"[WeaponInventory] Assign a drop prefab with WeaponPickup, Rigidbody and a collider for {entry.definition.weaponName}.");
            return false;
        }

        Transform aim = aimTransform != null ? aimTransform : transform;
        Vector3 position = throwOrigin != null ? throwOrigin.position : aim.position;
        GameObject dropped = Instantiate(prefab, position, aim.rotation);
        dropped.SetActive(true);
        WeaponPickup pickup = dropped.GetComponent<WeaponPickup>();
        pickup.Capture(entry);
        RemoveEntry(hand, entry);
        return true;
    }

    void OnDestroy()
    {
        foreach (WeaponEntry entry in weapons)
        {
            if (entry != null && entry.runtimeDefinition != null)
                Destroy(entry.runtimeDefinition);
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
