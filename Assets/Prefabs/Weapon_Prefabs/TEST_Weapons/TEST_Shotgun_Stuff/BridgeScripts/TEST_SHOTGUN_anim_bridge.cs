using UnityEngine;

public class TEST_SHOTGUN_anim_bridge : MonoBehaviour
{
    public WeaponBase weapon;

    public void OnCockComplete()
    {
        if (weapon != null)
            weapon.OnCockComplete();
    }
}
