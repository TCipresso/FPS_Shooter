using UnityEngine;

public class MorePointsPowerUp : PowerUpBase
{
    int originalPointsOnHit;
    int originalPointsOnKill;

    protected override void ApplyEffect()
    {
        if (playerStats == null) return;

        originalPointsOnHit = playerStats.pointsOnHit;
        originalPointsOnKill = playerStats.pointsOnKill;

        playerStats.pointsOnHit *= 2;
        playerStats.pointsOnKill *= 2;

        Debug.Log($"[MorePointsPowerUp] Points doubled. Hit: {playerStats.pointsOnHit} Kill: {playerStats.pointsOnKill}");
    }

    protected override void RemoveEffect()
    {
        if (playerStats == null) return;

        playerStats.pointsOnHit = originalPointsOnHit;
        playerStats.pointsOnKill = originalPointsOnKill;

        Debug.Log("[MorePointsPowerUp] Points restored.");
    }
}