using UnityEngine;
public class PowerUpPickup : MonoBehaviour
{
    /*[Tooltip("Index of the weapon in WeaponInventory's Weapons list this pickup activates.")]
    public int weaponIndex;
    [Tooltip("Check this for a weapon that has its own permanent baseLevel (e.g. Z16). Starts from baseLevel+1 and decays back to baseLevel instead of resetting to level 1.")]
    public bool isBaseTypeWeapon;

    [Header("Lifetime")]
    public float lifetime = 20f;
    public float flashWarningTime = 10f;
    public float flashInterval = 0.15f;
    public GameObject model;

    float timer;
    float flashTimer;
    bool modelVisible = true;

    void OnEnable()
    {
        timer = lifetime;
        flashTimer = 0f;
        modelVisible = true;
        if (model != null) model.SetActive(true);
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= flashWarningTime)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                flashTimer = flashInterval;
                modelVisible = !modelVisible;
                if (model != null) model.SetActive(modelVisible);
            }
        }

        if (timer <= 0f)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        WeaponInventory inventory = other.GetComponentInParent<WeaponInventory>();
        if (inventory == null) return;
        if (isBaseTypeWeapon)
            inventory.PickupBaseLevelPowerUpByIndex(weaponIndex);
        else
            inventory.PickupPowerUpByIndex(weaponIndex);
        Destroy(gameObject);
    }*/
}