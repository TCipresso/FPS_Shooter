using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AttachmentVisual
{
    public string modelName;
    public GameObject model;
}

[System.Serializable]
public class AttachmentMount
{
    public string slotName;
    public List<AttachmentVisual> visuals = new List<AttachmentVisual>();
}

public class WeaponAttachmentVisuals : MonoBehaviour
{
    public List<AttachmentMount> mounts = new List<AttachmentMount>();

    public void ApplyAttachments(List<AttachmentSO> attachments, Animator animator)
    {
        // Disable all visuals first
        foreach (AttachmentMount mount in mounts)
            foreach (AttachmentVisual visual in mount.visuals)
                if (visual.model != null)
                    visual.model.SetActive(false);

        if (attachments == null) return;

        // Enable matched visuals
        foreach (AttachmentSO attachment in attachments)
        {
            if (attachment == null) continue;

            AttachmentMount mount = mounts.Find(m => m.slotName == attachment.slotType);
            if (mount == null) continue;

            AttachmentVisual visual = mount.visuals.Find(v => v.modelName == attachment.modelName);
            if (visual != null && visual.model != null)
                visual.model.SetActive(true);
        }

        // Apply animation overrides
        if (animator == null) return;

        // Collect all overrides from rolled attachments
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (AttachmentSO attachment in attachments)
        {
            if (attachment == null) continue;
            if (attachment.overrideClip == null || attachment.clipToReplace == null) continue;
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(attachment.clipToReplace, attachment.overrideClip));
        }

        if (overrides.Count == 0) return;

        AnimatorOverrideController overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);

        List<KeyValuePair<AnimationClip, AnimationClip>> allOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(allOverrides);

        for (int i = 0; i < allOverrides.Count; i++)
        {
            KeyValuePair<AnimationClip, AnimationClip> entry = allOverrides[i];
            KeyValuePair<AnimationClip, AnimationClip> match = overrides.Find(o => o.Key == entry.Key);
            if (match.Key != null)
                allOverrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(entry.Key, match.Value);
        }

        overrideController.ApplyOverrides(allOverrides);
        animator.runtimeAnimatorController = overrideController;
    }
}