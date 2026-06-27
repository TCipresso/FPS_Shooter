using UnityEngine;
using System.Collections.Generic;

public class WeaponDropManager : MonoBehaviour
{
    public static WeaponDropManager Instance { get; private set; }

    [Header("Drop Pool")]
    public List<WeaponDefinitionSO> weaponPool = new List<WeaponDefinitionSO>();

    [Header("Drop Settings")]
    [Range(0f, 1f)] public float dropChance = 0.2f;

    [Header("Rarity Weights")]
    public float commonWeight = 60f;
    public float rareWeight = 25f;
    public float epicWeight = 10f;
    public float legendaryWeight = 4f;
    public float contrabandWeight = 1f;

    [Header("Pickup Prefab")]
    public GameObject weaponPickupPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TryDrop(Vector3 position)
    {
        if (weaponPool.Count == 0) return;
        if (Random.value > dropChance) return;

        WeaponDefinitionSO definition = weaponPool[Random.Range(0, weaponPool.Count)];
        WeaponRarity rarity = RollRarity();
        WeaponInstance instance = new WeaponInstance(definition, rarity);

        SpawnPickup(position, instance);
    }

    WeaponRarity RollRarity()
    {
        float total = commonWeight + rareWeight + epicWeight + legendaryWeight + contrabandWeight;
        float roll = Random.Range(0f, total);

        if (roll < commonWeight) return WeaponRarity.Common;
        if (roll < commonWeight + rareWeight) return WeaponRarity.Rare;
        if (roll < commonWeight + rareWeight + epicWeight) return WeaponRarity.Epic;
        if (roll < commonWeight + rareWeight + epicWeight + legendaryWeight) return WeaponRarity.Legendary;
        return WeaponRarity.Contraband;
    }

    void SpawnPickup(Vector3 position, WeaponInstance instance)
    {
        if (weaponPickupPrefab == null)
        {
            Debug.LogWarning("[WeaponDropManager] No pickup prefab assigned.");
            return;
        }

        GameObject obj = Instantiate(weaponPickupPrefab, position, Quaternion.identity);
        WeaponPickup pickup = obj.GetComponent<WeaponPickup>();
        if (pickup != null)
            pickup.Initialize(instance);
    }
}