using UnityEngine;
using System.Collections.Generic;

public class GadgetInstance
{
    public GadgetDefinitionSO definition;
    public WeaponRarity rarity;
    public List<WeaponPerkSO> rolledPerks = new List<WeaponPerkSO>();

    public float finalCooldown;
    public float finalDuration;
    public float finalPotency;

    public GadgetInstance(GadgetDefinitionSO definition, WeaponRarity rarity)
    {
        this.definition = definition;
        this.rarity = rarity;
        ComputeStats();
        RollPerks();
    }

    void ComputeStats()
    {
        float rarityMult = definition.GetRarityMultiplier(rarity);
        finalCooldown = Mathf.Max(0.1f, definition.baseCooldown / rarityMult);
        finalDuration = Mathf.Max(0.1f, definition.baseDuration * rarityMult);
        finalPotency = Mathf.Max(0f, definition.basePotency * rarityMult);
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