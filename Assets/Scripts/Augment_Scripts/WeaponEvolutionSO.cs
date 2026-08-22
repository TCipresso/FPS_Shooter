using UnityEngine;
public abstract class WeaponEvolutionSO : ScriptableObject
{
    public string displayName = "Evolution";
    [TextArea] public string description;
    public Sprite icon;
    public abstract void Apply(WeaponBase weapon);
}
