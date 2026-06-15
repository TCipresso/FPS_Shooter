using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject itemPickupUI;

    [Header("Pickup UI Fields")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI perkSlot1Text;
    public TextMeshProUGUI perkSlot2Text;
    public TextMeshProUGUI perkSlot3Text;

    [Header("References")]
    public FPSLook fpsLook;
    public WeaponInventory weaponInventory;

    WeaponPickup currentPickup;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OpenItemPickupUI(WeaponPickup pickup)
    {
        currentPickup = pickup;
        WeaponInstance instance = pickup.WeaponInstance;

        weaponNameText.text = instance.definition.weaponName;

        var perks = instance.rolledPerks;
        perkSlot1Text.text = perks.Count > 0 ? perks[0].perkName : "No Perk";
        perkSlot2Text.text = perks.Count > 1 ? perks[1].perkName : "No Perk";
        perkSlot3Text.text = perks.Count > 2 ? perks[2].perkName : "No Perk";

        itemPickupUI.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (fpsLook != null) fpsLook.enabled = false;
    }

    // Hook to Left button OnClick
    public void OnPickupLeft() => PickupToSlot(0);

    // Hook to Right button OnClick
    public void OnPickupRight() => PickupToSlot(1);

    void PickupToSlot(int slot)
    {
        if (currentPickup == null) return;

        weaponInventory.TryAddWeaponInstanceToSlot(currentPickup.WeaponInstance, slot);
        Destroy(currentPickup.transform.root.gameObject);
        currentPickup = null;

        CloseItemPickupUI();
    }

    public void CloseItemPickupUI()
    {
        itemPickupUI.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (fpsLook != null) fpsLook.enabled = true;
    }
}