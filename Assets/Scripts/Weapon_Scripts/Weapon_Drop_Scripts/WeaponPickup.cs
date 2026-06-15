using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    WeaponInstance weaponInstance;
    public WeaponInstance WeaponInstance => weaponInstance;

    [Header("Rarity Colors")]
    public Color commonColor = Color.white;
    public Color rareColor = Color.blue;
    public Color epicColor = new Color(0.5f, 0f, 0.5f);
    public Color legendaryColor = Color.yellow;
    public Color contrabandColor = new Color(1f, 0.4f, 0f);

    Light rarityLight;
    Renderer pickupRenderer;

    [Header("Launch")]
    public float launchForce = 5f;

    void Awake()
    {
        rarityLight = GetComponentInChildren<Light>();
        pickupRenderer = GetComponentInChildren<Renderer>();
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
        Color rarityColor = GetRarityColor(instance.rarity);

        if (rarityLight != null)
            rarityLight.color = rarityColor;

        if (pickupRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            pickupRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", rarityColor);
            mpb.SetColor("_EmissionColor", rarityColor * 0.3f);
            pickupRenderer.SetPropertyBlock(mpb);
        }
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