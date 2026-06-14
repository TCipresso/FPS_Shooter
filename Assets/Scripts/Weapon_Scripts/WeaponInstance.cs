using UnityEngine;

public class WeaponInstance
{
    public WeaponDefinitionSO definition;
    public WeaponRarity rarity;
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
}