using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Visual chest reveal. Present(attachment) fills the icon / name / description, then the
// panel fades the artwork in, then the Left/Right buttons. No grant logic yet - the buttons
// fire an event and close. Runs on unscaled time because the game is paused.
public class ChestUIDraft : MonoBehaviour
{
    [Header("Groups")]
    [Tooltip("CanvasGroup wrapping the icon + name + description. Fades in first.")]
    public CanvasGroup artworkGroup;
    [Tooltip("CanvasGroup wrapping the Left/Right buttons. Hidden until the artwork is fully in.")]
    public CanvasGroup buttonsGroup;

    [Header("Item Display")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    [Tooltip("Optional. Shows the rarity word, tinted.")]
    public TMP_Text rarityText;
    [Tooltip("Tint the name text with the rarity color.")]
    public bool tintNameByRarity = true;

    [Header("Timing (seconds, unscaled)")]
    public float artworkFadeDuration = 0.5f;
    public float holdAfterArtwork = 0.15f;
    public float buttonsFadeDuration = 0.25f;

    [Header("Grant")]
    [Tooltip("Auto-found if left empty.")]
    public WeaponInventory inventory;

    [Header("Events (optional)")]
    public UnityEvent onEquipLeft;
    public UnityEvent onEquipRight;

    public WeaponAttachmentSO Current { get; private set; }

    Coroutine routine;

    // Called by MenuUIHelper.OpenLootChest with the rolled attachment.
    public void Present(WeaponAttachmentSO attachment)
    {
        Current = attachment;
        if (attachment == null)
            Debug.LogWarning("[ChestUIDraft] Present() called with a null attachment - check LootChestBuy.table and that its pool is populated.");
        else
            Debug.Log($"[ChestUIDraft] Presenting: {attachment.displayName} ({attachment.rarity})");
        ApplyItemData();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true); // -> OnEnable -> RevealSequence
        }
        else
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(RevealSequence());
        }
    }

    void ApplyItemData()
    {
        if (Current == null) return;

        Color rarityColor = UpgradeRarityHelper.GetColor(Current.rarity);

        if (iconImage != null)
        {
            iconImage.sprite = Current.icon;
            if (Current.icon == null)
                Debug.LogWarning($"[ChestUIDraft] '{Current.displayName}' has no icon sprite assigned.");
        }
        if (nameText != null)
        {
            nameText.text = Current.displayName;
            if (tintNameByRarity) nameText.color = rarityColor;
        }
        if (descriptionText != null)
            descriptionText.text = Current.description;
        if (rarityText != null)
        {
            rarityText.text = Current.rarity.ToString();
            rarityText.color = rarityColor;
        }
    }

    void OnEnable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(RevealSequence());
    }

    void OnDisable()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
    }

    IEnumerator RevealSequence()
    {
        if (artworkGroup == null)
            Debug.LogWarning("[ChestUIDraft] artworkGroup is not assigned - the artwork can't fade in.");

        if (artworkGroup != null) artworkGroup.alpha = 0f;
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        yield return Fade(artworkGroup, 1f, artworkFadeDuration);

        if (holdAfterArtwork > 0f)
            yield return WaitUnscaled(holdAfterArtwork);

        if (buttonsGroup != null)
        {
            buttonsGroup.blocksRaycasts = true;
            yield return Fade(buttonsGroup, 1f, buttonsFadeDuration);
            buttonsGroup.interactable = true;
        }

        routine = null;
    }

    IEnumerator Fade(CanvasGroup g, float to, float duration)
    {
        if (g == null) yield break;
        if (duration <= 0f) { g.alpha = to; yield break; }

        float from = g.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        g.alpha = to;
    }

    IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }

    // Wire to the Left / Right buttons' OnClick.
    public void EquipLeft()
    {
        onEquipLeft?.Invoke();
        Grant(WeaponInventory.Hand.Left);
    }

    public void EquipRight()
    {
        onEquipRight?.Invoke();
        Grant(WeaponInventory.Hand.Right);
    }

    void Grant(WeaponInventory.Hand hand)
    {
        if (Current != null)
        {
            if (inventory == null) inventory = FindFirstObjectByType<WeaponInventory>();

            WeaponEntry entry = inventory != null ? inventory.GetActiveEntry(hand) : null;
            if (entry != null)
                WeaponAttachmentService.GrantOne(entry, Current);
            else
                Debug.LogWarning($"[ChestUIDraft] No active weapon in the {hand} hand - attachment not applied.");
        }

        if (MenuUIHelper.Instance != null) MenuUIHelper.Instance.CloseLootChest();
    }
}
