using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class WeaponChest : Buyable
{
    [Header("Pickup Prefabs (random pick)")]
    public List<GameObject> pickupPrefabs = new List<GameObject>();

    [Header("Level Roll")]
    [Min(1)] public int minLevel = 1;
    [Min(1)] public int maxLevel = 1;

    [Header("Spawn")]
    [Tooltip("Where the weapon pops out. Defaults to this object's transform.")]
    public Transform spawnPoint;
    public float upOffset = 0.5f;
    public float forwardOffset = 0.5f;

    protected override void OnPurchase(PlayerStats stats)
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[WeaponChest] Not the server/host - can't spawn. Run as host to test.");
            return;
        }

        if (pickupPrefabs == null || pickupPrefabs.Count == 0)
        {
            Debug.LogWarning("[WeaponChest] No pickup prefabs assigned.");
            return;
        }

        GameObject prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Count)];
        if (prefab == null)
        {
            Debug.LogWarning("[WeaponChest] Rolled a null prefab entry.");
            return;
        }

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        Vector3 pos = origin.position + Vector3.up * upOffset + origin.forward * forwardOffset;

        GameObject go = Instantiate(prefab, pos, origin.rotation);

        WeaponPickup pickup = go.GetComponent<WeaponPickup>();
        if (pickup == null)
        {
            Debug.LogWarning("[WeaponChest] Prefab has no WeaponPickup component.");
            Destroy(go);
            return;
        }

        int level = Random.Range(minLevel, maxLevel + 1);

        NetworkServer.Spawn(go);
        pickup.Initialize(level);
    }
}