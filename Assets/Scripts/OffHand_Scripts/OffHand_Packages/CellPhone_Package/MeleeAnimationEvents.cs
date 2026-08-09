using UnityEngine;

public class MeleeAnimationEvents : MonoBehaviour
{
    OffHandBase offHandBase;

    void Awake()
    {
        offHandBase = GetComponentInChildren<OffHandBase>();
    }

    public void ApplyMeleeScreenShake()
    {
        if (offHandBase == null)
            offHandBase = GetComponentInChildren<OffHandBase>();

        offHandBase?.ApplyMeleeScreenShake();
    }

    public void OnMeleeAnimationComplete()
    {
        if (offHandBase == null)
            offHandBase = GetComponentInChildren<OffHandBase>();

        offHandBase?.OnMeleeAnimationComplete();
    }
}