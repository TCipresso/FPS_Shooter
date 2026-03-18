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

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange) && IsInteractableTag(hit.collider.tag))
        {
            Buyable buyable = hit.collider.GetComponent<Buyable>();

            if (buyable != null)
            {
                ShowPrompt(buyable.interactPrompt);
                return;
            }
        }

        ClearPrompt();
    }

    void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange))
            return;

        if (!IsInteractableTag(hit.collider.tag))
            return;

        Buyable buyable = hit.collider.GetComponent<Buyable>();

        if (buyable == null)
            return;

        buyable.TryPurchase(stats);
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