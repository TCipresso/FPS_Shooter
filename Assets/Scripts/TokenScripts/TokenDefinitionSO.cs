using UnityEngine;
[CreateAssetMenu(fileName = "NewToken", menuName = "Zarcade/Token")]
public class TokenDefinitionSO : ScriptableObject
{
    public string tokenName = "Token";
    public Sprite icon;
    public TokenEffectSO effect;
}