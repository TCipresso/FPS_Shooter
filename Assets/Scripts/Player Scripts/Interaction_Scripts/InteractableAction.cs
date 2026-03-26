using UnityEngine;

[CreateAssetMenu(fileName = "NewAction", menuName = "Interactable/Action")]
public abstract class InteractableAction : ScriptableObject
{
    public abstract void Execute(PlayerStats stats);
}