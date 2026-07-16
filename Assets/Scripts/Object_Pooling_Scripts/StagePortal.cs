using UnityEngine;

public class StagePortal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player == null) return;

        RoundManager.Instance.OnStagePortalEntered();
    }
}