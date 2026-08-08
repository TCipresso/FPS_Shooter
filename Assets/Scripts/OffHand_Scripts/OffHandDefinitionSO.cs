using UnityEngine;

public enum OffHandType
{
    Utility,
    Melee,
    Throwable,
    Shield
}

[CreateAssetMenu(fileName = "NewOffHandDefinition", menuName = "Zarcade/Off-Hand Definition")]
public class OffHandDefinitionSO : ScriptableObject
{
    [Header("Info (label only, not used as an ID)")]
    public string offHandName = "Off-Hand Item";
    public OffHandType category;
}
