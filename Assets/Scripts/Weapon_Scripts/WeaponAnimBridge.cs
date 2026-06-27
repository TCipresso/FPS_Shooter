using UnityEngine;

public class WeaponAnimBridge : MonoBehaviour
{
    public WeaponBase weapon;
    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Cock");
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
        }

        if (weapon != null)
            weapon.isCocking = false;
    }

    public void OnCockComplete()
    {
        Debug.Log("[WeaponAnimBridge] OnCockComplete fired on: " + gameObject.name);
        if (weapon != null)
            weapon.OnCockComplete();
    }

    public void EjectCasing()
    {
        if (weapon != null)
            weapon.EjectCasing();
    }

    public void PlayWeaponSound(AnimationEvent evt)
    {
        if (weapon == null) return;
        weapon.PlaySoundByName(evt.stringParameter);
    }
}