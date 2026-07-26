using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Mirror;

public class PlayerInteract : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public PlayerStats stats;
    public TextMeshProUGUI promptText;
    public WeaponInventory weaponInventory;

    [Header("Interact Settings")]
    public float interactRange = 3f;
    public InputActionReference interactAction;

    [Header("Interactable Tags")]
    public List<string> interactableTags = new List<string> { "Buyable" };

    [Header("Weapon Pickup")]
    [Tooltip("Weapon pickups are detected by component, not tag - but they still need to be on a layer the interact ray can hit.")]
    public bool pickupsUseSeparateTag = false;
    public string weaponPickupTag = "WeaponPickup";

    public override void OnStartLocalPlayer()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }

    void OnDisable()
    {
        if (!isLocalPlayer) return;
        if (interactAction != null)
            interactAction.action.Disable();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        CheckForInteractable();

        if (interactAction != null && interactAction.action.WasPressedThisFrame())
            TryInteract();
    }

    void CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            // Weapon pickups are detected by component, independent of the interactable tag list.
            WeaponPickup pickup = hit.collider.GetComponentInChildren<WeaponPickup>();
            if (pickup != null)
            {
                ShowPrompt(pickup.BuildPrompt());
                return;
            }

            if (!IsInteractableTag(hit.collider.tag))
            {
                ClearPrompt();
                return;
            }

            Buyable buyable = hit.collider.GetComponent<Buyable>();
            if (buyable != null)
            {
                ShowPrompt(buyable.interactPrompt);
                return;
            }

            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                ShowPrompt(interactable.interactPrompt);
                return;
            }

            Debug.Log("Tag matched but no Buyable or Interactable component found on: " + hit.collider.gameObject.name);
        }

        ClearPrompt();
    }

    void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange))
            return;

        // Weapon pickup takes priority and bypasses the tag gate.
        WeaponPickup pickup = hit.collider.GetComponentInChildren<WeaponPickup>();
        if (pickup != null)
        {
            if (weaponInventory != null)
                weaponInventory.RequestPickup(pickup);
            return;
        }

        if (!IsInteractableTag(hit.collider.tag))
            return;

        Buyable buyable = hit.collider.GetComponent<Buyable>();
        if (buyable != null)
        {
            buyable.TryPurchase(stats);
            return;
        }

        Interactable interactable = hit.collider.GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.Interact(stats);
            return;
        }
    }

    bool IsInteractableTag(string tag)
    {
        return interactableTags.Contains(tag);
    }

    void ShowPrompt(string prompt)
    {
        if (promptText == null) return;
        promptText.gameObject.SetActive(true);
        promptText.text = prompt;
    }

    void ClearPrompt()
    {
        if (promptText == null) return;
        promptText.gameObject.SetActive(false);
        promptText.text = "";
    }

    void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactRange);
    }
}