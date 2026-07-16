using UnityEngine;
[CreateAssetMenu(menuName = "Zarcade/Tokens/DummyToken")]
public class DummyToken : TokenEffectSO
{
    public override void OnApply(PlayerStats stats)
    {
        Debug.Log("Dummy token activated");
    }
    public override void OnRemove(PlayerStats stats)
    {
    }
}