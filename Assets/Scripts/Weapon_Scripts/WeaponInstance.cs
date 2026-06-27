using UnityEngine;

public class WeaponInstance
{
    public WeaponDefinitionSO definition;
    public WeaponRarity rarity;
    public int finalDamage;
    public float finalRpm;
    public float finalRange;

    public WeaponInstance(WeaponDefinitionSO definition, WeaponRarity rarity)
    {
        this.definition = definition;
        this.rarity = rarity;
        ComputeStats();
    }

    void ComputeStats()
    {
        float rarityMult = definition.GetRarityMultiplier(rarity);
        finalDamage = Mathf.Max(1, Mathf.RoundToInt(definition.baseDamage * rarityMult));
        finalRpm = Mathf.Max(1f, definition.baseRpm * rarityMult);
        finalRange = Mathf.Max(1f, definition.baseRange * rarityMult);
    }
}