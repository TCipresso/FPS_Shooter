using UnityEngine;

public class RandWeaponMachine : Buyable
{
    [Header("Settings")]
    public Vector3 ejectOffset = new Vector3(0f, 1f, 1f);

    int purchasesRemaining;

    void Start()
    {
        purchasesRemaining = Random.Range(3, 7);
    }

    public new string interactPrompt => $"Buy {itemName} - {cost} Points ({purchasesRemaining} uses left)";

    protected override void OnPurchase(PlayerStats stats)
    {
        Vector3 spawnPos = transform.position + transform.TransformDirection(ejectOffset);

        if (WeaponDropManager.Instance != null)
            WeaponDropManager.Instance.ForceDropRandom(spawnPos);

        purchasesRemaining--;

        if (purchasesRemaining <= 0)
        {
            Debug.Log("[RandWeaponMachine] Uses exhausted, disappearing.");
            gameObject.SetActive(false);
        }
    }
}