using UnityEngine;
public class HitBox : MonoBehaviour
{
    [Header("References")]
    public ZombieBase zombie;
    [Header("Damage")]
    [Tooltip("Limb damage modifier. Use 1 for head/body, 0.75 for legs etc.")]
    public float limbMultiplier = 1f;
    public bool isHeadshot = false;
    void Awake()
    {
        if (zombie == null)
            zombie = GetComponentInParent<ZombieBase>();
    }
    public void TakeDamage(int amount, PlayerStats dealer, WeaponBase weapon, float weaponMultiplier = 1f, Vector3 hitDirection = default, float ragdollForceMultiplier = 1f)
    {
        if (zombie == null) return;
        int finalDamage = Mathf.RoundToInt(amount * limbMultiplier);
        if (isHeadshot && weapon != null)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * weapon.critMultiplier);
        }
        else if (weapon != null)
        {
            finalDamage = weapon.ApplyCrit(finalDamage);
        }
        zombie.TakeDamage(finalDamage, dealer, weaponMultiplier, hitDirection, ragdollForceMultiplier, gameObject.name, weapon);
    }
    public void TakeDamageWithHitPoint(int amount, PlayerStats dealer, WeaponBase weapon, Vector3 hitPoint, float weaponMultiplier = 1f, Vector3 hitDirection = default, float ragdollForceMultiplier = 1f)
    {
        if (zombie == null) return;
        int finalDamage = Mathf.RoundToInt(amount * limbMultiplier);
        bool isCrit = false;
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
        zombie.TakeDamage(finalDamage, dealer, weaponMultiplier, hitDirection, ragdollForceMultiplier, gameObject.name, weapon);
        zombie.hitFlash?.Flash(isCrit);
        if (HitMarkerPool.Instance != null)
            HitMarkerPool.Instance.Spawn(hitPoint, isCrit);
    }
}