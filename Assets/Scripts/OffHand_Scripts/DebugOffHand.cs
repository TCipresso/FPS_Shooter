using UnityEngine;

public class DebugOffHand : OffHandBase
{
    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log($"[DebugOffHand] Base present on {gameObject.name}.");
    }
}
