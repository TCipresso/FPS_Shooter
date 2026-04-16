using UnityEngine;

public class HitBox : MonoBehaviour
{
    [Header("References")]
    public ZombieBase zombie;

    [Header("Damage")]
    public float damageMultiplier = 1f;
    public bool isHeadshot = false;

    void Awake()
    {
        if (zombie == null)
            zombie = GetComponentInParent<ZombieBase>();
    }

    public void TakeDamage(int amount, PlayerStats dealer, float weaponMultiplier = 1f)
    {
        if (zombie == null) return;
        int finalDamage = Mathf.RoundToInt(amount * damageMultiplier);

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
        if (zombie == null) return;
        int finalDamage = Mathf.RoundToInt(amount * damageMultiplier);

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

        if (HitMarkerPool.Instance != null)
            HitMarkerPool.Instance.Spawn(hitPoint, isCrit);
    }
}