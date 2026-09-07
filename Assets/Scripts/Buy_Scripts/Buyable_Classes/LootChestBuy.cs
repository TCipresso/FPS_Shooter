using UnityEngine;

// Buyable loot chest. On purchase it pauses the game (Time.timeScale = 0), hands cursor +
// look control to MenuUIHelper, and enables a referenced UI panel. Wire the panel's close
// button to Close().
public class LootChestBuy : Buyable
{
    [Header("Loot Chest")]
    [Tooltip("UI panel (a disabled GameObject) shown while the game is paused after purchase.")]
    public GameObject chestUI;

    [Tooltip("Handles cursor unlock + disabling FPS look. Auto-found if left empty.")]
    public MenuUIHelper menuUI;

    [Tooltip("Disable this chest's collider once bought so it can't be purchased again.")]
    public bool consumeOnPurchase = true;

    float resumeTimeScale = 1f;

    MenuUIHelper Menu => menuUI != null ? menuUI : (menuUI = FindFirstObjectByType<MenuUIHelper>());

    protected override void OnPurchase(PlayerStats stats)
    {
        if (consumeOnPurchase)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        if (Menu != null)
            Menu.EnterDraftState();
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (chestUI != null)
            chestUI.SetActive(true);
    }

    // Hook this to the panel's close / continue button.
    public void Close()
    {
        if (chestUI != null)
            chestUI.SetActive(false);

        Time.timeScale = resumeTimeScale;

        if (Menu != null)
            Menu.ExitDraftState();
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
