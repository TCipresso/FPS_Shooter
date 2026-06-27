using UnityEngine;
using System.Collections.Generic;

public class WeaponDropManager : MonoBehaviour
{
    public static WeaponDropManager Instance { get; private set; }

    [Header("Weapon Drop Pool")]
    public List<WeaponDefinitionSO> weaponPool = new List<WeaponDefinitionSO>();

    [Header("Gadget Drop Pool")]
    public List<GadgetDefinitionSO> gadgetPool = new List<GadgetDefinitionSO>();

    [Header("Drop Settings")]
    [Range(0f, 1f)] public float dropChance = 0.2f;

    [Header("Drop Type Weights")]
    public float weaponWeight = 1f;
    public float gadgetWeight = 1f;

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
        if (Random.value > dropChance) return;

        bool hasWeapons = weaponPool.Count > 0;
        bool hasGadgets = gadgetPool.Count > 0;

        if (!hasWeapons && !hasGadgets) return;

        float effectiveWeaponWeight = hasWeapons ? weaponWeight : 0f;
        float effectiveGadgetWeight = hasGadgets ? gadgetWeight : 0f;
        float total = effectiveWeaponWeight + effectiveGadgetWeight;

        if (total <= 0f) return;

        WeaponRarity rarity = RollRarity();

        if (Random.Range(0f, total) < effectiveWeaponWeight)
        {
            WeaponDefinitionSO definition = weaponPool[Random.Range(0, weaponPool.Count)];
            SpawnPickup(position, new WeaponInstance(definition, rarity));
        }
        else
        {
            GadgetDefinitionSO definition = gadgetPool[Random.Range(0, gadgetPool.Count)];
            SpawnPickup(position, new GadgetInstance(definition, rarity));
        }
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
        if (weaponPickupPrefab == null) { Debug.LogWarning("[WeaponDropManager] No pickup prefab assigned."); return; }
        GameObject obj = Instantiate(weaponPickupPrefab, position, Quaternion.identity);
        obj.GetComponent<WeaponPickup>()?.Initialize(instance);
    }

    void SpawnPickup(Vector3 position, GadgetInstance instance)
    {
        if (weaponPickupPrefab == null) { Debug.LogWarning("[WeaponDropManager] No pickup prefab assigned."); return; }
        GameObject obj = Instantiate(weaponPickupPrefab, position, Quaternion.identity);
        obj.GetComponent<WeaponPickup>()?.Initialize(instance);
    }
}