using UnityEngine;
using System.Collections.Generic;

public class WeaponInstance
{
    public WeaponDefinitionSO definition;
    public WeaponRarity rarity;
    public List<WeaponPerkSO> rolledPerks = new List<WeaponPerkSO>();
    public int finalDamage;
    public float finalRpm;
    public int finalMagSize;
    public int finalReserveAmmo;
    public float finalRange;
    public float finalReloadTime;

    public WeaponInstance(WeaponDefinitionSO definition, WeaponRarity rarity)
    {
        this.definition = definition;
        this.rarity = rarity;
        ComputeStats();
        RollPerks();
    }

    void ComputeStats()
    {
        float rarityMult = definition.GetRarityMultiplier(rarity);

        finalDamage = Mathf.Max(1, Mathf.RoundToInt(definition.baseDamage * rarityMult));
        finalRpm = Mathf.Max(1f, definition.baseRpm * rarityMult);
        finalMagSize = Mathf.Max(1, Mathf.RoundToInt(definition.baseMagSize * rarityMult));
        finalReserveAmmo = Mathf.Max(0, Mathf.RoundToInt(definition.baseReserveAmmo * rarityMult));
        finalRange = Mathf.Max(1f, definition.baseRange * rarityMult);
        finalReloadTime = Mathf.Max(0.1f, definition.baseReloadTime);
    }

    void RollPerks()
    {
        int perkCount = rarity switch
        {
            WeaponRarity.Rare => 1,
            WeaponRarity.Epic => 2,
            WeaponRarity.Legendary => 3,
            WeaponRarity.Contraband => 3,
            _ => 0
        };

        if (perkCount == 0 || definition.perkPool.Count == 0) return;

        List<WeaponPerkSO> available = new List<WeaponPerkSO>(definition.perkPool);
        perkCount = Mathf.Min(perkCount, available.Count);

        for (int i = 0; i < perkCount; i++)
        {
            int index = Random.Range(0, available.Count);
            rolledPerks.Add(available[index]);
            available.RemoveAt(index);
        }
    }
}