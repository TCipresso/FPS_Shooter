using UnityEngine;

[CreateAssetMenu(fileName = "NewAugment", menuName = "Augments/Augment Data")]
public class AugmentData : ScriptableObject
{
    [Header("Basic Info")]
    public string augmentName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effects")]
    [Tooltip("List of all effects this augment will apply when chosen.")]
    public AugmentEffect[] effects;  // Drag & drop multiple effect SOs here

    public void Apply(GameObject player)
    {
        if (effects == null || effects.Length == 0)
            return;

        // Loop through each effect and apply it to the player
        foreach (var effect in effects)
        {
            if (effect != null)
                effect.ApplyEffect(player);
        }
    }
}
