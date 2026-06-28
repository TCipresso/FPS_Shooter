using UnityEngine;

public class ZombieAnimBridge : MonoBehaviour
{
    public ZombieBase zombie;

    public void OnHitFrame()
    {
        zombie?.OnHitFrame();
    }

    public void OnAttackComplete()
    {
        zombie?.OnAttackComplete();
    }
}