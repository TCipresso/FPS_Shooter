using UnityEngine;

public abstract class OffHandBase : MonoBehaviour
{
    protected PlayerStats playerStats;
    protected PlayerFpsController fpsController;
    public PlayerStats OwnerStats => playerStats;

    protected virtual void Awake()
    {
        ResolveOwningPlayerReferences();
    }

    protected virtual void OnEnable()
    {
        ResolveOwningPlayerReferences();
    }

    void ResolveOwningPlayerReferences()
    {
        if (playerStats == null)
            playerStats = GetComponentInParent<PlayerStats>();
        if (fpsController == null)
            fpsController = GetComponentInParent<PlayerFpsController>();
    }

    public virtual void OnEquip()
    {
        Debug.Log($"[OffHandBase] {gameObject.name} equipped.");
    }

    public virtual void OnUnequip()
    {
        Debug.Log($"[OffHandBase] {gameObject.name} unequipped.");
    }
}
