using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WeaponPickup : MonoBehaviour
{
    public WeaponDefinitionSO definition;
    public int startingAmmo = -1;
    public float pickupDelay = 0.35f;

    private WeaponDefinitionSO runtimeDefinition;
    private int[] ammo;
    private float availableAt;
    private bool consumed;
    private Rigidbody body;
    private Collider[] pickupColliders;
    private Collider[] ownerColliders;

    public bool CanPickup => !consumed && isActiveAndEnabled && Time.time >= availableAt;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        pickupColliders = GetComponentsInChildren<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        TryPickup(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryPickup(other);
    }

    void TryPickup(Collider other)
    {
        if (!CanPickup)
            return;

        WeaponInventory inventory = other.GetComponentInParent<WeaponInventory>();
        if (inventory != null)
            inventory.TryPickup(this);
    }

    public void Capture(WeaponEntry entry)
    {
        definition = entry.definition;
        if (runtimeDefinition != null)
            Destroy(runtimeDefinition);
        runtimeDefinition = Instantiate(entry.RuntimeDefinition);
        runtimeDefinition.usedEvolutions = new List<WeaponEvolutionSO>(entry.RuntimeDefinition.usedEvolutions);
        ammo = new int[entry.weaponBases.Count];
        for (int i = 0; i < ammo.Length; i++)
            ammo[i] = entry.weaponBases[i] != null ? entry.weaponBases[i].currentAmmo : 0;
    }

    public WeaponDefinitionSO CreateRuntimeDefinition()
    {
        WeaponDefinitionSO source = runtimeDefinition != null ? runtimeDefinition : definition;
        WeaponDefinitionSO copy = Instantiate(source);
        copy.usedEvolutions = new List<WeaponEvolutionSO>(source.usedEvolutions);
        if (runtimeDefinition == null)
        {
            copy.level = 1;
            copy.currentXP = 0f;
            copy.usedEvolutions.Clear();
        }
        return copy;
    }

    public void RestoreAmmo(WeaponEntry entry)
    {
        for (int i = 0; i < entry.weaponBases.Count; i++)
        {
            WeaponBase weaponBase = entry.weaponBases[i];
            if (weaponBase == null)
                continue;

            int count = ammo != null && i < ammo.Length ? ammo[i] : startingAmmo;
            weaponBase.currentAmmo = count < 0 ? weaponBase.MaxAmmo : Mathf.Clamp(count, 0, weaponBase.MaxAmmo);
        }
    }

    public void Launch(Vector3 velocity, Vector3 spin, Transform owner)
    {
        availableAt = Time.time + Mathf.Max(0f, pickupDelay);
        ownerColliders = owner.GetComponentsInChildren<Collider>();
        SetOwnerCollision(true);
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.AddForce(velocity, ForceMode.VelocityChange);
        body.AddTorque(spin, ForceMode.VelocityChange);
    }

    void FixedUpdate()
    {
        if (ownerColliders == null || Time.time < availableAt)
            return;

        foreach (Collider pickupCollider in pickupColliders)
        {
            foreach (Collider ownerCollider in ownerColliders)
            {
                if (pickupCollider != null && ownerCollider != null &&
                    pickupCollider.bounds.Intersects(ownerCollider.bounds))
                    return;
            }
        }

        SetOwnerCollision(false);
        ownerColliders = null;
    }

    void SetOwnerCollision(bool ignore)
    {
        if (ownerColliders == null)
            return;

        foreach (Collider pickupCollider in pickupColliders)
        {
            foreach (Collider ownerCollider in ownerColliders)
            {
                if (pickupCollider != null && ownerCollider != null)
                    Physics.IgnoreCollision(pickupCollider, ownerCollider, ignore);
            }
        }
    }

    public void Consume()
    {
        consumed = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void OnDisable()
    {
        SetOwnerCollision(false);
        ownerColliders = null;
    }

    void OnDestroy()
    {
        if (runtimeDefinition != null)
            Destroy(runtimeDefinition);
    }
}
