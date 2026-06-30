using UnityEngine;

[CreateAssetMenu(fileName = "NewPower", menuName = "Zarcade/Power")]
public class PowerDefinitionSO : ScriptableObject
{
    public string powerName = "Power";
    public Sprite icon;
    public PowerEffectSO effect;
}