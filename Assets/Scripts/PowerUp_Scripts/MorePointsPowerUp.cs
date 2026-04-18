using UnityEngine;

public class MorePointsPowerUp : PowerUpBase
{
    /* [Header("Gold On Hit Settings")]
     public int goldOnHitAmount = 10;

     int originalGoldOnHit;
     int originalGoldOnKill; */

     protected override void ApplyEffect()
     {
         /*if (playerStats == null) return;

         originalGoldOnHit = playerStats.goldOnHit;
         originalGoldOnKill = playerStats.goldOnKill;

         playerStats.goldOnHit = goldOnHitAmount;
         playerStats.goldOnKill *= 2;

         Debug.Log($"[MorePointsPowerUp] Gold on hit: {playerStats.goldOnHit} | Gold on kill doubled: {playerStats.goldOnKill}");*/
     }

     protected override void RemoveEffect()
     {
         /*if (playerStats == null) return;

         playerStats.goldOnHit = originalGoldOnHit;
         playerStats.goldOnKill = originalGoldOnKill;

         Debug.Log("[MorePointsPowerUp] Gold restored.");*/
     }
}