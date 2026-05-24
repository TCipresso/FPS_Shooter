using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    WeaponInstance weaponInstance;

    [Header("Rarity Colors")]
    public Color commonColor = Color.white;
    public Color rareColor = Color.blue;
    public Color epicColor = new Color(0.5f, 0f, 0.5f);
    public Color legendaryColor = Color.yellow;
    public Color contrabandColor = new Color(1f, 0.4f, 0f);

    Light rarityLight;

    void Awake()
    {
        rarityLight = GetComponentInChildren<Light>();
    }

    public void Initialize(WeaponInstance instance)
    {
        weaponInstance = instance;
        if (rarityLight != null)
            rarityLight.color = GetRarityColor(instance.rarity);
    }

    void OnTriggerEnter(Collider other)
    {
        if (weaponInstance == null) return;

        WeaponInventory inventory = other.GetComponent<WeaponInventory>();
        if (inventory == null) return;

        inventory.TryAddWeaponInstance(weaponInstance);
        Destroy(gameObject);
    }

    Color GetRarityColor(WeaponRarity rarity)
    {
        return rarity switch
        {
            WeaponRarity.Common => commonColor,
            WeaponRarity.Rare => rareColor,
            WeaponRarity.Epic => epicColor,
            WeaponRarity.Legendary => legendaryColor,
            WeaponRarity.Contraband => contrabandColor,
            _ => commonColor
        };
    }
}