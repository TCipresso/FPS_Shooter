using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKWeaponHandler : MonoBehaviour
{
    [Header("IK Constraints")]
    public TwoBoneIKConstraint rightHandIK;
    public TwoBoneIKConstraint leftHandIK;

    RigBuilder rigBuilder;

    void Awake()
    {
        rigBuilder = GetComponentInParent<RigBuilder>();
    }

    public void UpdateIKTargets(GameObject weapon)
    {
        if (weapon == null) return;

        Transform rightTarget = weapon.transform.Find("RightHandTarget");
        Transform leftTarget = weapon.transform.Find("LeftHandTarget");

        if (rightTarget != null && rightHandIK != null)
            rightHandIK.data.target = rightTarget;

        if (leftTarget != null && leftHandIK != null)
            leftHandIK.data.target = leftTarget;

        // Rebuild rig so new targets take effect
        if (rigBuilder != null)
            rigBuilder.Build();
    }
}