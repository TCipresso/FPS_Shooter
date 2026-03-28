using UnityEngine;

public class TEST_SHOTGUN_anim_bridge : MonoBehaviour
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
            animator.enabled = false;
            animator.enabled = true;
            animator.SetBool("IsReloading", false);
            animator.ResetTrigger("Cock");
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
        }

        if (weapon != null)
        {
            weapon.isReloading = false;
            weapon.isCocking = false;
        }
    }

    public void OnCockComplete()
    {
        if (weapon != null)
            weapon.OnCockComplete();
    }

    public void OnReloadComplete()
    {
        if (weapon != null)
            weapon.OnReloadComplete();
    }

    public void PlayWeaponSound(AnimationEvent evt)
    {
        if (weapon == null) return;
        weapon.PlaySoundByName(evt.stringParameter);
    }
}