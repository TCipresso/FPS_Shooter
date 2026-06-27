using UnityEngine;

public class Cigarette : GadgetBase
{
    protected override void Awake()
    {
        base.Awake();
        gadgetName = "Cigarette";
    }

    protected override void OnUse() { }
}