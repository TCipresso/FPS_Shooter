using UnityEngine;

[CreateAssetMenu(fileName = "NewDoubleJumpAugment", menuName = "Augments/Effects/Double Jump")]
public class DoubleJumpAugment : AugmentEffect
{
    public override void ApplyEffect(GameObject player)
    {
        FPSController controller = player.GetComponent<FPSController>();
        if (controller != null)
            controller.canDoubleJump = true;
    }
}