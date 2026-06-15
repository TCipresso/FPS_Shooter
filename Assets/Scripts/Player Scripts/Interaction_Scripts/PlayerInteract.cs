using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteract : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public PlayerStats stats;
    public TextMeshProUGUI promptText;

    [Header("Interact Settings")]
    public float interactRange = 3f;
    public InputActionReference interactAction;

    [Header("Interactable Tags")]
    public List<string> interactableTags = new List<string> { "Buyable" };

    void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }

    void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.Disable();
    }

    void Update()
    {
        CheckForInteractable();
        if (interactAction != null && interactAction.action.WasPressedThisFrame())
            TryInteract();
    }

    void CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            ZombieBase zombie = hit.collider.GetComponentInParent<ZombieBase>();
            if (zombie != null)
                EnemyHealthBarManager.Instance?.SetTarget(zombie);

            if (hit.collider.CompareTag("ItemPickup"))
            {
                ShowPrompt("[F] Pick up weapon");
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
        }
        else
        {
            EnemyHealthBarManager.Instance?.ClearTarget();
        }

        ClearPrompt();
    }

    void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange))
            return;

        if (hit.collider.CompareTag("ItemPickup"))
        {
            WeaponPickup pickup = hit.collider.GetComponentInParent<WeaponPickup>();
            if (pickup != null)
                UIManager.Instance?.OpenItemPickupUI(pickup);
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

    bool IsInteractableTag(string tag) => interactableTags.Contains(tag);

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