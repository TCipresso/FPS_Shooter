using UnityEngine;

public class HitBox : MonoBehaviour
{
    [Header("References")]
    public ZombieBase zombie;

    [Header("Damage")]
    [Tooltip("Limb damage modifier. Use 1 for head/body, 0.75 for legs etc. Headshot crit bonus comes from the weapon's Crit Multiplier.")]
    public float limbMultiplier = 1f;
    public bool isHeadshot = false;

    void Awake()
    {
        if (zombie == null)
            zombie = GetComponentInParent<ZombieBase>();
    }

    public void TakeDamage(int amount, PlayerStats dealer, float weaponMultiplier = 1f)
    {
        if (zombie == null) return;
        int finalDamage = Mathf.RoundToInt(amount * limbMultiplier);

        bool isCrit = false;
        WeaponBase weapon = dealer?.GetComponentInChildren<WeaponBase>() ?? FindFirstObjectByType<WeaponBase>();
        if (isHeadshot && weapon != null)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * weapon.critMultiplier);
            isCrit = true;
        }
        else if (weapon != null)
        {
            int rolled = weapon.ApplyCrit(finalDamage);
            isCrit = rolled != finalDamage;
            finalDamage = rolled;
        }

        zombie.TakeDamage(finalDamage, dealer, weaponMultiplier);
        return;
    }

    public void TakeDamageWithHitPoint(int amount, PlayerStats dealer, Vector3 hitPoint, float weaponMultiplier = 1f)
    {
        Debug.Log($"[HitBox] TakeDamageWithHitPoint called. zombie={zombie}, isCrit will be calculated");
        if (zombie == null) { Debug.LogError("[HitBox] zombie is null!"); return; }
        int finalDamage = Mathf.RoundToInt(amount * limbMultiplier);

        bool isCrit = false;
        WeaponBase weapon = dealer?.GetComponentInChildren<WeaponBase>() ?? FindFirstObjectByType<WeaponBase>();
        if (isHeadshot && weapon != null)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * weapon.critMultiplier);
            isCrit = true;
        }
        else if (weapon != null)
        {
            int rolled = weapon.ApplyCrit(finalDamage);
            isCrit = rolled != finalDamage;
            finalDamage = rolled;
        }

        zombie.TakeDamage(finalDamage, dealer, weaponMultiplier);

        ZombieHitFlash flash = zombie.GetComponent<ZombieHitFlash>();
        Debug.Log($"[HitBox] flash component={flash}, isCrit={isCrit}");
        if (flash != null) flash.Flash(isCrit);

        if (HitMarkerPool.Instance != null)
            HitMarkerPool.Instance.Spawn(hitPoint, isCrit);
    }
}