using UnityEngine;

[CreateAssetMenu(menuName = "Zarcade/Powers/DummyPower")]
public class DummyPower : PowerEffectSO
{
    public override void OnApply(PlayerStats stats)
    {
        Debug.Log("Dummy power activated");
    }

    public override void OnRemove(PlayerStats stats)
    {
    }
}