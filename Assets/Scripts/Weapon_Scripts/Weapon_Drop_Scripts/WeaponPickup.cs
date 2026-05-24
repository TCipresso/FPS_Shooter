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

    [Header("Launch")]
    public float launchForce = 5f;

    void Awake()
    {
        rarityLight = GetComponentInChildren<Light>();
    }

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        Vector3 launchDir = (Vector3.up * 2f + randomDir).normalized;
        rb.AddForce(launchDir * launchForce, ForceMode.Impulse);
    }

    public void Initialize(WeaponInstance instance)
    {
        weaponInstance = instance;
        if (rarityLight != null)
            rarityLight.color = GetRarityColor(instance.rarity);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        WeaponInventory inventory = other.GetComponent<WeaponInventory>();
        if (inventory == null)
            inventory = other.GetComponentInParent<WeaponInventory>();
        if (inventory == null) return;

        inventory.TryAddWeaponInstance(weaponInstance);
        Destroy(transform.root.gameObject);
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