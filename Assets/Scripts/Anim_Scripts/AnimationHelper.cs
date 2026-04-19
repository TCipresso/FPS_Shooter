using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AnimationHelper : MonoBehaviour
{
    [Header("IK Constraints")]
    public TwoBoneIKConstraint rightHandIK;
    public TwoBoneIKConstraint leftHandIK;

    [Header("Mag Eject")]
    public ParticleSystem magEjectEffect;

    // IK Control
    public void DisableLeftIK()
    {
        if (leftHandIK != null) leftHandIK.weight = 0f;
    }

    public void EnableLeftIK()
    {
        if (leftHandIK != null) leftHandIK.weight = 1f;
    }

    public void DisableRightIK()
    {
        if (rightHandIK != null) rightHandIK.weight = 0f;
    }

    public void EnableRightIK()
    {
        if (rightHandIK != null) rightHandIK.weight = 1f;
    }

    public void DisableBothIK()
    {
        DisableLeftIK();
        DisableRightIK();
    }

    public void EnableBothIK()
    {
        EnableLeftIK();
        EnableRightIK();
    }

    // Mag Eject
    public void EjectMagazine()
    {
        if (magEjectEffect == null) return;
        magEjectEffect.gameObject.SetActive(true);
        magEjectEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        magEjectEffect.Play();
    }
}