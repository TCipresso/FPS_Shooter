using UnityEngine;

[CreateAssetMenu(fileName = "MoneyPickupEffect", menuName = "Zarcade/Pickups/Money")]
public class MoneyPickupEffectSO : PickupEffectSO
{
    [Header("Money Amount")]
    [SerializeField] private int amount = 100;

    public override void Apply(GameObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (stats == null)
            stats = player.GetComponentInParent<PlayerStats>();

        if (stats == null)
        {
            Debug.LogWarning("MoneyPickupEffectSO: no PlayerStats found on player.", player);
            return;
        }

        stats.AddGold(amount);
    }
}