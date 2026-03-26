using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Prompt")]
    public string interactPrompt = "Press E to interact";

    [Header("Action")]
    public InteractableAction action;

    public void Interact(PlayerStats stats)
    {
        if (action == null)
        {
            Debug.LogWarning($"[Interactable] No action assigned on {gameObject.name}!");
            return;
        }

        action.Execute(stats);
    }
}